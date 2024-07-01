
// MOIT
//#pragma shader_feature _MOMENT4 _MOMENT6 _MOMENT8
#pragma multi_compile _MOMENT4 _MOMENT6 _MOMENT8
#pragma multi_compile _ _TRIGONOMETRIC
#pragma shader_feature _MOMENT_HALF_PRECISION _MOMENT_SINGLE_PRECISION

#if _MOMENT_HALF_PRECISION
#include "Assets/MOIT/ShaderLibrary/Quantization.hlsl"
#endif
#include "Assets/MOIT/ShaderLibrary/MomentMath.hlsl"
#include "Assets/MOIT/ShaderLibrary/TrigonometricMomentMath.hlsl"
#include "Assets/MOIT/ShaderLibrary/WarpDepth.hlsl"
#include "Assets/MOIT/ShaderLibrary/ResolveMoments.hlsl"
//

//#define _ALPHATEST_ON 1

void InitializeInputData(Varyings input, SurfaceDescription surfaceDescription, out InputData inputData)
{
    inputData = (InputData)0;

    inputData.positionWS = input.positionWS;

#ifdef _NORMALMAP
    // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
    float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
    float3 bitangent = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);

    inputData.tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
#if _NORMAL_DROPOFF_TS
    inputData.normalWS = TransformTangentToWorld(surfaceDescription.NormalTS, inputData.tangentToWorld);
#elif _NORMAL_DROPOFF_OS
    inputData.normalWS = TransformObjectToWorldNormal(surfaceDescription.NormalOS);
#elif _NORMAL_DROPOFF_WS
    inputData.normalWS = surfaceDescription.NormalWS;
#endif
#else
    inputData.normalWS = input.normalWS;
#endif
    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif

    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
#if defined(DYNAMICLIGHTMAP_ON)
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV.xy, input.sh, inputData.normalWS);
#elif !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
    inputData.bakedGI = SAMPLE_GI(input.sh,
        GetAbsolutePositionWS(inputData.positionWS),
        inputData.normalWS,
        inputData.viewDirectionWS,
        input.positionCS.xy);
#else
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.sh, inputData.normalWS);
#endif
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);

#if defined(DEBUG_DISPLAY)
#if defined(DYNAMICLIGHTMAP_ON)
    inputData.dynamicLightmapUV = input.dynamicLightmapUV.xy;
#endif
#if defined(LIGHTMAP_ON)
    inputData.staticLightmapUV = input.staticLightmapUV;
#else
    inputData.vertexSH = input.sh;
#endif

    inputData.positionCS = input.positionCS;
#endif
}

PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    output = BuildVaryings(input);
    PackedVaryings packedOutput = (PackedVaryings)0;
    packedOutput = PackVaryings(output);
    return packedOutput;
}

void frag(
    PackedVaryings packedInput
    , out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out float4 outRenderingLayers : SV_Target1
#endif
)
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);
    SurfaceDescription surfaceDescription = BuildSurfaceDescription(unpacked);

#if defined(_SURFACE_TYPE_TRANSPARENT)
    bool isTransparent = true;
#else
    bool isTransparent = false;
#endif

#if defined(_ALPHATEST_ON)
    half alpha = AlphaDiscard(surfaceDescription.Alpha, surfaceDescription.AlphaClipThreshold);
#elif defined(_SURFACE_TYPE_TRANSPARENT)
    half alpha = surfaceDescription.Alpha;
#else
    half alpha = half(1.0);
#endif

#if defined(LOD_FADE_CROSSFADE) && USE_UNITY_CROSSFADE
    LODFadeCrossFade(unpacked.positionCS);
#endif

    InputData inputData;
    InitializeInputData(unpacked, surfaceDescription, inputData);
#ifdef VARYINGS_NEED_TEXCOORD0
    SETUP_DEBUG_TEXTURE_DATA(inputData, unpacked.texCoord0);
#else
    SETUP_DEBUG_TEXTURE_DATA_NO_UV(inputData);
#endif

#ifdef _SPECULAR_SETUP
    float3 specular = surfaceDescription.Specular;
    float metallic = 1;
#else
    float3 specular = 0;
    float metallic = surfaceDescription.Metallic;
#endif

    half3 normalTS = half3(0, 0, 0);
#if defined(_NORMALMAP) && defined(_NORMAL_DROPOFF_TS)
    normalTS = surfaceDescription.NormalTS;
#endif

    SurfaceData surface;
    surface.albedo = surfaceDescription.BaseColor;
    surface.metallic = saturate(metallic);
    surface.specular = specular;
    surface.smoothness = saturate(surfaceDescription.Smoothness),
        surface.occlusion = surfaceDescription.Occlusion,
        surface.emission = surfaceDescription.Emission,
        surface.alpha = saturate(alpha);
    surface.normalTS = normalTS;
    surface.clearCoatMask = 0;
    surface.clearCoatSmoothness = 1;

#ifdef _CLEARCOAT
    surface.clearCoatMask = saturate(surfaceDescription.CoatMask);
    surface.clearCoatSmoothness = saturate(surfaceDescription.CoatSmoothness);
#endif

    surface.albedo = AlphaModulate(surface.albedo, surface.alpha);

#ifdef _DBUFFER
    ApplyDecalToSurfaceData(unpacked.positionCS, surface, inputData);
#endif

    half4 color = UniversalFragmentPBR(inputData, surface);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);

    color.a = OutputAlpha(color.a, isTransparent);

    outColor = color;

#ifdef _WRITE_RENDERING_LAYERS
    uint renderingLayers = GetMeshRenderingLayer();
    outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
#endif

    //clip(color.a - surfaceDescription.AlphaClipThreshold - 0.01);
    //clip(color.a - surfaceDescription.AlphaClipThreshold - 0.1h);
    //clip(color.a - surfaceDescription.AlphaClipThreshold);
    //clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold); // fix for moit tex being blown up in game view
    clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold - 0.01); // fix for moit tex being blown up in game view

    // resolve moments
    //float positionCSZ = LinearEyeDepth(unpacked.positionWS, UNITY_MATRIX_V);
    float positionCSZ = LinearEyeDepth(unpacked.positionCS.z, _ZBufferParams); // does not correctly handle oblique nor ortho
    float td, tt;
    //_Overestimation = 0.25f; // now declared as 0.25 by default
    ResolveMoments(td, tt, positionCSZ, unpacked.positionCS.xy * _B0_TexelSize.xy);
    outColor.rgb *= outColor.a;
    //outColor.rgb *= saturate(outColor.a);
    outColor *= td;

    //outColor.rgb = max(outColor.rgb, 0.0); // fix for the tiny outline that sends negative values into the moit texture (blowing it up) in game view - don't use this, use clip instead
}