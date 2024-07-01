using System;
using UnityEditor.Rendering.Universal;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;
using static Unity.Rendering.Universal.ShaderUtils;

// currently the same as ShaderGraphLitGUI, to stay up to date with later changes to URP we'll just use that unless we have to change something here later

namespace UnityEditor
{
    class ShaderGraphMOITLitGUI : BaseShaderGUI
    //class ShaderGraphMOITLitGUI : ShaderGraphLitGUI
    {
        
        public MaterialProperty workflowMode;
        //public MaterialProperty specularHighlights;

        MaterialProperty[] properties;

        public override void FindProperties(MaterialProperty[] properties)
        {
            // save off the list of all properties for shadergraph
            this.properties = properties;

            var material = materialEditor?.target as Material;
            if (material == null)
                return;

            base.FindProperties(properties);
            workflowMode = BaseShaderGUI.FindProperty(Property.SpecularWorkflowMode, properties, false);
            //specularHighlights = BaseShaderGUI.FindProperty(Rendering.Universal.ShaderGraph.MOITProperty.SpecularHighlights, properties, false);
        }

        public static void UpdateMaterial(Material material, MaterialUpdateType updateType)
        {
            // newly created materials should initialize the globalIlluminationFlags (default is off)
            if (updateType == MaterialUpdateType.CreatedNewMaterial)
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;

            bool automaticRenderQueue = GetAutomaticQueueControlSetting(material);
            BaseShaderGUI.UpdateMaterialSurfaceOptions(material, automaticRenderQueue);
            LitGUI.SetupSpecularWorkflowKeyword(material, out bool isSpecularWorkflow);
            BaseShaderGUI.UpdateMotionVectorKeywordsAndPass(material);
        }

        public override void ValidateMaterial(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            UpdateMaterial(material, MaterialUpdateType.ModifiedMaterial);
        }

        public override void DrawSurfaceOptions(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;

            // Detect any changes to the material
            if (workflowMode != null)
                DoPopup(LitGUI.Styles.workflowModeText, workflowMode, Enum.GetNames(typeof(LitGUI.WorkflowMode)));
            base.DrawSurfaceOptions(material);
        }

        // material main surface inputs
        public override void DrawSurfaceInputs(Material material)
        {
            DrawShaderGraphProperties(material, properties);
        }

        //public static readonly GUIContent specularHighlightsText = EditorGUIUtility.TrTextContent("Specular Highlights", "When enabled, this GameObject will receive Specular Highlights.");

        public override void DrawAdvancedOptions(Material material)
        {
            //if (specularHighlights != null)
            //    DrawFloatToggleProperty(specularHighlightsText, specularHighlights);

            // Always show the queue control field.  Only show the render queue field if queue control is set to user override
            DoPopup(Styles.queueControl, queueControlProp, Styles.queueControlNames);
            if (material.HasProperty(Property.QueueControl) && material.GetFloat(Property.QueueControl) == (float)QueueControl.UserOverride)
                materialEditor.RenderQueueField();
            base.DrawAdvancedOptions(material);

            // ignore emission color for shadergraphs, because shadergraphs don't have a hard-coded emission property, it's up to the user
            materialEditor.DoubleSidedGIField();
            materialEditor.LightmapEmissionFlagsProperty(0, enabled: true, ignoreEmissionColor: true);
        }
        
    }
}