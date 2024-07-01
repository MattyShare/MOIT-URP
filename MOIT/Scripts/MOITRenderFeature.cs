using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using System.Collections.Generic;
using MOIT.Register;

[DisallowMultipleRendererFeature("Moment Based Order Independent Transparency")]
public class MOITRenderFeature : ScriptableRendererFeature
{
    class MOITPass : ScriptableRenderPass
    {
        MOITSettings settings;
        MOITBias biasSettings;

        Vector2 _ViewDepthMinMax;

        Vector4 m_ScaleBias = new Vector4(1f, 1f, 0f, 0f);

        List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();
        List<ShaderTagId> m_ShaderTagIdListResolve = new List<ShaderTagId>();
        FilteringSettings m_FilteringSettings;
        RenderStateBlock m_RenderStateBlock;

        private static readonly int scaleBiasRt = Shader.PropertyToID("_ScaleBiasRt");

        public static RenderQueueRange oit = new RenderQueueRange()
        {
            lowerBound = 2501,
            upperBound = 3000
        }; // was 4501 - 5000 before using separate layers

        // settings will be empty when first created, so allow to create without and update later
        public MOITPass()
        {

        }

        public MOITPass(MOITSettings _settings, MOITBias _bias)
        {
            UpdateSettings(_settings, _bias);
        }

        public void UpdateSettings(MOITSettings _settings, MOITBias _bias)
        {
            settings = _settings;
            biasSettings = _bias;
            oit = new RenderQueueRange()
            {
                lowerBound = _settings.renderQueueMin,
                upperBound = _settings.renderQueueMax
            };
            if (m_ShaderTagIdList.Count > 0)
            {
                Debug.LogWarning("ShaderTagList unexpectedly kept when recreated, clearing");
                m_ShaderTagIdList.Clear();
                m_ShaderTagIdListResolve.Clear();
            }
            m_ShaderTagIdList.Add(new ShaderTagId("GenerateMoments"));
            m_ShaderTagIdListResolve.Add(new ShaderTagId("ResolveMoments"));
            m_FilteringSettings = new FilteringSettings(oit, settings.layerMask);
            m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
        }

        public static bool GetViewDepthMinMaxWithRenderQueue(Camera camera, MOITSettings settings, out Vector2 minMax)
        {
            if(settings.boundsType == BoundsType.NearFarPlanes)
            {
                // default to nearfarplanes
                minMax.x = camera.nearClipPlane;
                minMax.y = camera.farClipPlane;
                return true;
            }
            else
            {
                minMax = Vector2.zero;
                Bounds bounds;

                if (settings.boundsType == BoundsType.Register && Application.isPlaying) // default to FindObjects if in editor (out of active play mode)
                {
                    MOITRendererList rendererList = MOITRendererList.Instance;
                    if (rendererList.GetCount() == 0)
                        return false; // no registered : skip early

                    if (settings.onlyVisibleRenderers)
                    {
                        bool oneVisible = rendererList.GetVisibleBounds(out bounds);
                        if (!oneVisible) // no visible : skip early
                            return false;
                    }
                    else
                    {
                        bounds = rendererList.GetBounds();
                    }
                }
                else // BoundsType.FindObjects
                {
                    bool b = false;
                    bounds = new Bounds();

                    //Renderer[] coms = Renderer.FindObjectsOfType<Renderer>();
                    Renderer[] coms = Renderer.FindObjectsByType<Renderer>(FindObjectsSortMode.None); // a bit faster than above but still a problem

                    if (null == coms || 0 == coms.Length)
                    {
                        return false;
                    }

                    if(settings.onlyVisibleRenderers)
                    {
                        foreach (var p in coms)
                        {
                            Renderer r = p.GetComponent<Renderer>();
                            if (null != r && r.enabled && r.isVisible
                                && (settings.layerMask & (1 << r.gameObject.layer)) > 0)
                            {
                                if (r is SkinnedMeshRenderer)
                                {
                                    (r as SkinnedMeshRenderer).sharedMesh.RecalculateBounds();
                                }
                                Bounds rb = r.bounds;
                                if (b)
                                {
                                    bounds.Encapsulate(rb);
                                }
                                else
                                {
                                    bounds = rb;
                                    b = true;
                                }
                            }
                        }
                        if (!b)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        foreach (var p in coms)
                        {
                            Renderer r = p.GetComponent<Renderer>();
                            if (null != r && r.enabled
                                //&& r.sharedMaterial.renderQueue >= range.min && r.sharedMaterial.renderQueue <= range.max)
                                //&& r.sharedMaterial.renderQueue >= range.lowerBound && r.sharedMaterial.renderQueue <= range.upperBound)
                                && (settings.layerMask & (1 << r.gameObject.layer)) > 0)
                            {
                                if (r is SkinnedMeshRenderer)
                                {
                                    (r as SkinnedMeshRenderer).sharedMesh.RecalculateBounds();
                                }
                                Bounds rb = r.bounds;
                                if (b)
                                {
                                    bounds.Encapsulate(rb);
                                }
                                else
                                {
                                    bounds = rb;
                                    b = true;
                                }
                            }
                        }
                        if (!b)
                        {
                            return false;
                        }
                    }
                }

                // build conservative bounding sphere
                Vector3 fwd = camera.transform.forward;
                Vector3 c2b = bounds.center - camera.transform.position;
                float c2bDis = Vector3.Dot(fwd, c2b);
                float bs = bounds.extents.magnitude;

                //minMax.x = Mathf.Max(0, c2bDis - bs);
                //minMax.y = c2bDis + bs;
                minMax.x = Mathf.Max(camera.nearClipPlane, c2bDis - bs);
                minMax.y = Mathf.Min(camera.farClipPlane, c2bDis + bs);

                return true;
            }
        }

        private const float M_PI = 3.14159265358979323f;
        //private const float M_PI = Mathf.PI;

        private static float CircleToParameter(float angle, out float maxParameter)
        {
            float x = Mathf.Cos(angle);
            float y = Mathf.Sin(angle);
            float result = Mathf.Abs(y) - Mathf.Abs(x);
            result = (x < 0.0f) ? (2.0f - result) : result;
            result = (y < 0.0f) ? (6.0f - result) : result;
            result += (angle >= 2.0f * M_PI) ? 8.0f : 0.0f;
            maxParameter = 7.0f; // why?
            return result;
        }

        public static Vector4 ComputeWrappingZoneParameters(float newWrappingZoneAngle = 0.1f * M_PI)
        {
            Vector4 result = new Vector4();
            result.x = newWrappingZoneAngle;
            result.y = M_PI - 0.5f * newWrappingZoneAngle;
            if(newWrappingZoneAngle <= 0.0f)
            {
                result.z = 0.0f;
                result.w = 0.0f;
            }
            else
            {
                float zoneEndParameter;
                float zoneBeginParameter = CircleToParameter(2.0f * M_PI - newWrappingZoneAngle, out zoneEndParameter);
                result.z = 1.0f / (zoneEndParameter - zoneBeginParameter);
                result.w = 1.0f - zoneEndParameter * result.z;
            }
            return result;
        }

        Vector4 _WrappingZoneParameters = ComputeWrappingZoneParameters();

        // This class stores the data needed by the RenderGraph pass.
        // It is passed as a parameter to the delegate function that executes the RenderGraph pass.
        internal class GeneratePassData
        {
            internal TextureHandle color;
            internal RendererListHandle rendererList;
            internal UniversalCameraData cameraData;
            //internal bool isActiveTargetBackBuffer; // for xr non rg compatibility mode

            internal MomentsCount momentsCount;
            //internal FloatPrecision momentsPrecision;
            internal bool halfPrecision;
            internal bool trigonometric;
            internal Vector2 viewDepthMinMax;
            internal float momentBias;
        }

        internal class ResolvePassData
        {
            //internal TextureHandle color;
            internal RendererListHandle rendererList;
            internal UniversalCameraData cameraData;
            internal bool isActiveTargetBackBuffer;

            internal MomentsCount momentsCount;
            //internal FloatPrecision momentsPrecision;
            //internal bool trigonometric;
        }

        // for the copy color pass (needed for the blit to work)
        private class CopyPassData
        {
            public TextureHandle inputTexture;
        }

        private class CompositePassData
        {
            //public TextureHandle inputTexture; // don't need for framebufferfetch
            public TextureHandle moitTexture;
            public Material material;
            public bool usesMSAA;
        }
        /*
        // This static method is passed as the RenderFunc delegate to the RenderGraph render pass.
        // It is used to execute draw commands.
        static void ExecutePass(GeneratePassData data, RasterGraphContext context)
        {
        }
        */
        private static readonly int b0TextureID = Shader.PropertyToID("_B0");
        private static readonly int b1TextureID = Shader.PropertyToID("_B1");
        private static readonly int b2TextureID = Shader.PropertyToID("_B2");
        private static readonly int logViewMinDeltaID = Shader.PropertyToID("_LogViewDepthMinDelta");
        private static readonly int wrappingZoneParametersID = Shader.PropertyToID("_WrappingZoneParameters");
        private static readonly int biasID = Shader.PropertyToID("_MOIT_MomentBias");
        private static readonly int alphaToMaskAvailableID = Shader.PropertyToID("_AlphaToMaskAvailable");

        // RecordRenderGraph is where the RenderGraph handle can be accessed, through which render passes can be added to the graph.
        // FrameData is a context container through which URP resources can be accessed and managed.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            //const string copyPassName = "MOIT Copy Color Pass";
            const string generatePassName = "MOIT Generate Moments Pass";
            const string resolvePassName = "MOIT Resolve Moments Pass";
            const string compositePassName = "MOIT Composite Pass";

            // Make use of frameData to access resources and camera data through the dedicated containers.
            // Eg:
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();

            // ignore preview camera (material preview in editor inspector)
            if (cameraData.isPreviewCamera)
                return;

            // The following line ensures that the render pass doesn't blit
            // from the back buffer.
            if (resourceData.isActiveTargetBackBuffer)
                return;

            // skip if there is no object in bounds, and receive conservative bounding sphere minmax
            if (!GetViewDepthMinMaxWithRenderQueue(cameraData.camera, settings, out _ViewDepthMinMax))
                return;

            bool isHalfPrecision = settings.momentPrecision == FloatPrecision._Half;
            // prevent the use of half precision power moments as quantization relies on ROVs
            if (isHalfPrecision && !settings.trigonometric)
                isHalfPrecision = false;

            // prepare copy color and moit add texture
            var colorCopyDescriptor = cameraData.cameraTargetDescriptor;
            //colorCopyDescriptor.msaaSamples = 1;
            bool usesMSAA = colorCopyDescriptor.msaaSamples > 1; // check if uses MSAA
            colorCopyDescriptor.depthBufferBits = (int)DepthBits.None;
            RenderTextureDescriptor baseDescriptor = colorCopyDescriptor;
            bool clear = false; //true
            baseDescriptor.colorFormat = RenderTextureFormat.ARGBHalf; // we need an alpha channel, so the usual B10G11R11_UFloatPack32 doesn't cut it
            TextureHandle moitHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, baseDescriptor, "_MOIT_Texture", clear);

            // prepare moment textures
            RenderTextureDescriptor descriptorFloat4;
            RenderTextureDescriptor descriptorFloat2;
            RenderTextureDescriptor descriptorFloat;
            if (isHalfPrecision)
            {
                baseDescriptor.colorFormat = RenderTextureFormat.ARGBHalf;
                descriptorFloat4 = baseDescriptor;
                baseDescriptor.colorFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGHalf) ? RenderTextureFormat.RGHalf : RenderTextureFormat.ARGBHalf;
                descriptorFloat2 = baseDescriptor;
                baseDescriptor.colorFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RHalf) ? RenderTextureFormat.RHalf : RenderTextureFormat.ARGBHalf;
                descriptorFloat = baseDescriptor;
            }
            else // single precision
            {
                baseDescriptor.colorFormat = RenderTextureFormat.ARGBFloat;
                descriptorFloat4 = baseDescriptor;
                baseDescriptor.colorFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGFloat) ? RenderTextureFormat.RGFloat : RenderTextureFormat.ARGBFloat;
                descriptorFloat2 = baseDescriptor;
                baseDescriptor.colorFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RFloat) ? RenderTextureFormat.RFloat : RenderTextureFormat.ARGBFloat;
                descriptorFloat = baseDescriptor;
            }
            FilterMode filterMode = FilterMode.Bilinear; // unusure if needed over point so putting this here to change it quickly
            TextureHandle b0 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptorFloat, "_MOIT_B0", clear, filterMode);
            TextureHandle b1 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptorFloat4, "_MOIT_B1", clear, filterMode);
            TextureHandle b2 = TextureHandle.nullHandle;
            if (settings.momentsCount == MomentsCount._8)
            {
                b2 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptorFloat4, "_MOIT_B2", clear, filterMode);
            }
            else if (settings.momentsCount == MomentsCount._6)
            {
                b2 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptorFloat2, "_MOIT_B2", clear, filterMode);
            }

            SortingCriteria sortingCritera = settings.sortBackToFront ? SortingCriteria.BackToFront | SortingCriteria.OptimizeStateChanges : SortingCriteria.OptimizeStateChanges;

            // Generate moments pass
            using (var builder = renderGraph.AddRasterRenderPass<GeneratePassData>(generatePassName, out var passData))
            {
                passData.cameraData = cameraData;
                //passData.isActiveTargetBackBuffer = resourceData.isActiveTargetBackBuffer;
                passData.color = resourceData.activeColorTexture;

                passData.momentsCount = settings.momentsCount;
                //passData.momentsPrecision = settings.momentPrecision;
                passData.halfPrecision = isHalfPrecision;
                passData.trigonometric = settings.trigonometric;
                passData.viewDepthMinMax = _ViewDepthMinMax;

                //if(settings.momentPrecision == FloatPrecision._Half)
                if(isHalfPrecision)
                {
                    if (settings.momentsCount == MomentsCount._4)
                        passData.momentBias = settings.trigonometric ? biasSettings.Trigonometric2Half : biasSettings.Moments4Half;
                    else if(settings.momentsCount == MomentsCount._6)
                        passData.momentBias = settings.trigonometric ? biasSettings.Trigonometric3Half : biasSettings.Moments6Half;
                    else
                        passData.momentBias = settings.trigonometric ? biasSettings.Trigonometric4Half : biasSettings.Moments8Half;

                }
                else
                {
                    if (settings.momentsCount == MomentsCount._4)
                        passData.momentBias = settings.trigonometric ? biasSettings.Trigonometric2Single : biasSettings.Moments4Single;
                    else if (settings.momentsCount == MomentsCount._6)
                        passData.momentBias = settings.trigonometric ? biasSettings.Trigonometric3Single : biasSettings.Moments6Single;
                    else
                        passData.momentBias = settings.trigonometric ? biasSettings.Trigonometric4Single : biasSettings.Moments8Single;
                }

                // build rendererlist
                DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIdList, renderingData, passData.cameraData, lightData, sortingCritera);
                //RenderingUtils.CreateRendererListWithRenderStateBlock //internal
                var param = new RendererListParams(renderingData.cullResults, drawingSettings, m_FilteringSettings);
                passData.rendererList = renderGraph.CreateRendererList(param);
                builder.UseRendererList(passData.rendererList);

                // forget about it, debughandler is internal to urp assembly
                //var activeDebugHandler = GetActiveDebugHandler(passData.cameraData);
                //var debugHandler = cameraData.renderer.DebugHandler;

                builder.SetRenderAttachment(b0, 0);
                builder.SetRenderAttachment(b1, 1);
                if (settings.momentsCount != MomentsCount._4)
                    builder.SetRenderAttachment(b2, 2);

                // for ztest (and also it seems like setting MRTs without a depth buffer is not supported in some path)
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read); //AccessFlags.ReadWrite

                // necessary for keywords
                builder.AllowGlobalStateModification(true);

                // give access to next pass without using cmd.setglobaltexture (which is not good according to the docs who talk about things that we can't use)
                //const int b0TextureID = Shader.globalTextureID("_B0"); // why mention this in the docs if it doesn't exist?
                builder.SetGlobalTextureAfterPass(b0, b0TextureID);
                builder.SetGlobalTextureAfterPass(b1, b1TextureID);
                if (passData.momentsCount != MomentsCount._4)
                    builder.SetGlobalTextureAfterPass(b2, b2TextureID);

                // don't skip pass even if nothing is visible (for testing)
                builder.AllowPassCulling(false);

                // Assigns the ExecutePass function to the render pass delegate. This will be called by the render graph when executing the pass.
                //builder.SetRenderFunc((GeneratePassData data, RasterGraphContext context) => ExecutePass(data, context));
                builder.SetRenderFunc((GeneratePassData data, RasterGraphContext context) =>
                {
                    //ExecutePass(data, context);
                    RasterCommandBuffer cmd = context.cmd;

                    var isYFlipped = data.cameraData.IsRenderTargetProjectionMatrixFlipped(data.color);
                    float flipSign = isYFlipped ? -1.0f : 1.0f;
                    // scaleBias.x = flipSign
                    // scaleBias.y = scale
                    // scaleBias.z = bias
                    // scaleBias.w = unused
                    Vector4 scaleBias = (flipSign < 0.0f)
                        ? new Vector4(flipSign, 1.0f, -1.0f, 1.0f)
                        : new Vector4(flipSign, 0.0f, 1.0f, 1.0f);
                    cmd.SetGlobalVector(scaleBiasRt, scaleBias);

                    // setup keywords
                    CoreUtils.SetKeyword(cmd, "_MOMENT6", data.momentsCount == MomentsCount._6);
                    CoreUtils.SetKeyword(cmd, "_MOMENT8", data.momentsCount == MomentsCount._8);
                    //CoreUtils.SetKeyword(cmd, "_MOMENT_HALF_PRECISION", data.momentsPrecision == FloatPrecision._Half);
                    CoreUtils.SetKeyword(cmd, "_MOMENT_HALF_PRECISION", data.halfPrecision);
                    CoreUtils.SetKeyword(cmd, "_MOMENT_SINGLE_PRECISION", !data.halfPrecision);
                    CoreUtils.SetKeyword(cmd, "_TRIGONOMETRIC", data.trigonometric);
                    Vector2 logViewDepthMinDelta = new Vector2(Mathf.Log(data.viewDepthMinMax.x), Mathf.Log(data.viewDepthMinMax.y));
                    logViewDepthMinDelta.y = logViewDepthMinDelta.y - logViewDepthMinDelta.x;
                    cmd.SetGlobalVector(logViewMinDeltaID, logViewDepthMinDelta);
                    //Debug.Log(logViewDepthMinDelta.ToString()); // is (-infinity,+infinity) sometimes, dunno why - will maybe be fine with register scripts instead of the findobjects
                    //cmd.SetGlobalVector(logViewMinDeltaID, new Vector4(logViewDepthMinDelta.x, logViewDepthMinDelta.y, 0.0f, 0.0f));
                    if (data.trigonometric)
                    {
                        cmd.SetGlobalVector(wrappingZoneParametersID, _WrappingZoneParameters);
                    }
                    cmd.SetGlobalFloat(biasID, data.momentBias);

                    // Set a value that can be used by shaders to identify when AlphaToMask functionality may be active
                    // The material shader alpha clipping logic requires this value in order to function correctly in all cases.
                    //float alphaToMaskAvailable = ((data.cameraData.cameraTargetDescriptor.msaaSamples > 1) && data.isOpaque) ? 1.0f : 0.0f;
                    //cmd.SetGlobalFloat(ShaderPropertyId.alphaToMaskAvailable, alphaToMaskAvailable);
                    cmd.SetGlobalFloat(alphaToMaskAvailableID, 0.0f); // removed check as this is a transparent pass so always false

                    // clear target
                    cmd.ClearRenderTarget(false, true, Color.clear);

                    // draw renderers
                    cmd.DrawRendererList(data.rendererList);
                    //RenderingUtils.DrawRendererListObjectsWithError(cmd, ref data.errorRenderers); // another internal
                });
            }

            // Resolve moments pass
            using (var builder = renderGraph.AddRasterRenderPass<ResolvePassData>(resolvePassName, out var passData))
            {
                passData.cameraData = cameraData;

                passData.momentsCount = settings.momentsCount;
                //passData.momentsPrecision = settings.momentPrecision;
                //passData.trigonometric = settings.trigonometric;

                // textures used for shading
                //builder.UseAllGlobalTextures(true); //only use this if you don't know which textures you need
                TextureHandle mainShadowsTexture = resourceData.mainShadowsTexture;
                TextureHandle additionalShadowsTexture = resourceData.additionalShadowsTexture;
                if (mainShadowsTexture.IsValid())
                    builder.UseTexture(mainShadowsTexture, AccessFlags.Read);
                if (additionalShadowsTexture.IsValid())
                    builder.UseTexture(additionalShadowsTexture, AccessFlags.Read);
                TextureHandle ssaoTexture = resourceData.ssaoTexture;
                if (ssaoTexture.IsValid())
                    builder.UseTexture(ssaoTexture, AccessFlags.Read);
                TextureHandle opaqueTexture = resourceData.cameraOpaqueTexture; // for refraction
                if (opaqueTexture.IsValid())
                    builder.UseTexture(opaqueTexture, AccessFlags.Read);

                // build rendererlist
                DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIdListResolve, renderingData, passData.cameraData, lightData, sortingCritera); //todo: find a way to reuse the rendererlist from previous pass
                //RenderingUtils.CreateRendererListWithRenderStateBlock //internal
                var param = new RendererListParams(renderingData.cullResults, drawingSettings, m_FilteringSettings);
                passData.rendererList = renderGraph.CreateRendererList(param);
                builder.UseRendererList(passData.rendererList);

                // TODO: check if we can use framebuffer fetch in our shader :
                //builder.SetInputAttachment(b0, 0);
                //builder.SetInputAttachment(b1, 1);
                //if (settings.momentsCount != MomentsCount._4)
                //    builder.SetInputAttachment(b2, 2);
                // else :
                builder.UseTexture(b0, AccessFlags.Read);
                builder.UseTexture(b1, AccessFlags.Read);
                if (settings.momentsCount != MomentsCount._4)
                    builder.UseTexture(b2, AccessFlags.Read);

                builder.SetRenderAttachment(moitHandle, 0);

                // for ztest
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.ReadWrite); // r/w as some shaders may want to write to depth

                // necessary for keywords
                //builder.AllowGlobalStateModification(true);
                if(settings.debugMakeMOITTexGlobal)
                    builder.SetGlobalTextureAfterPass(moitHandle, Shader.PropertyToID("_MOIT")); // for testing

                // don't skip pass even if nothing is visible (for testing)
                builder.AllowPassCulling(false);

                // Assigns the ExecutePass function to the render pass delegate. This will be called by the render graph when executing the pass.
                builder.SetRenderFunc((ResolvePassData data, RasterGraphContext context) =>
                {
                    
                    var cmd = context.cmd;

                    cmd.ClearRenderTarget(false, true, Color.clear);
                    cmd.DrawRendererList(data.rendererList);
                });
            }
            
            // composite pass
            using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>(compositePassName, out var passData))
            {
                passData.material = settings.compositeMaterial;
                passData.moitTexture = moitHandle;
                passData.usesMSAA = usesMSAA;

                // use b0 for alpha
                builder.UseTexture(b0, AccessFlags.Read);
                if(usesMSAA) // don't use framebufferfetch
                    builder.UseTexture(moitHandle, AccessFlags.Read);
                else // can use framebufferfetch
                    builder.SetInputAttachment(moitHandle, 0); // Use the output of the previous pass as the input, and bind it as FrameBufferFetch input

                // write to screen
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);

                //builder.AllowGlobalStateModification(true);

                // don't skip pass even if nothing is visible (for testing)
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((CompositePassData data, RasterGraphContext context) =>
                {
                    if (data.usesMSAA)
                        Blitter.BlitTexture(context.cmd, data.moitTexture, m_ScaleBias, data.material, 0); // use normal pass
                    else
                        Blitter.BlitTexture(context.cmd, m_ScaleBias, data.material, 1); // use framebufferfetch pass
                });
            }
        }

        // functions for non render graph (not implemented)
        /*
        // NOTE: This method is part of the compatibility rendering path, please use the Render Graph API above instead.
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
        }

        // NOTE: This method is part of the compatibility rendering path, please use the Render Graph API above instead.
        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
        }

        // NOTE: This method is part of the compatibility rendering path, please use the Render Graph API above instead.
        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
        */
    }

    MOITPass m_ScriptablePass;

    public RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingTransparents;
    public MOITSettings settings;
    public MOITBias biasSettings;

    private bool initialized;

    /// <inheritdoc/>
    public override void Create()
    {
        if(settings == null || biasSettings == null)
        {
            Debug.LogWarning("Missing settings in MOITPass");
            m_ScriptablePass = new MOITPass();
        }
        else
        {
            m_ScriptablePass = new MOITPass(settings, biasSettings);
            initialized = true;
        }        

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = passEvent;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!initialized)
        {
            if (settings == null || biasSettings == null)
                return;

            m_ScriptablePass.UpdateSettings(settings, biasSettings);
            initialized = true;
        }

        var camData = renderingData.cameraData;

        // skip preview camera
        if (camData.isPreviewCamera)
            return;

        // skip if missing material
        if (settings.compositeMaterial == null)
            return;

        // skip if layermask is nothing
        if (settings.layerMask == 0)
            return;

        renderer.EnqueuePass(m_ScriptablePass);
    }

    public enum MomentsCount
    {
        _4 = 4,
        _6 = 6,
        _8 = 8
    }

    public enum FloatPrecision
    {
        _Half = 16,
        _Single = 32
    }

    public enum BoundsType
    {
        NearFarPlanes,
        FindObjects,
        Register
        // ideally add JustIterateThroughTheRendererListHandle at some point
    }
}
