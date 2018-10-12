using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.LightweightPipeline
{
    internal class LitShaderGUI : BaseShaderGUI
    {
        public enum WorkflowMode
        {
            Specular = 0,
            Metallic
        }

        public enum SmoothnessMapChannel
        {
            SpecularMetallicAlpha,
            AlbedoAlpha,
        }

        protected class StylesLit
        {
            public static GUIContent specularMapText = new GUIContent("Specular Map", "Specular (RGB) and Smoothness (A)");
            public static GUIContent metallicMapText = new GUIContent("Metallic Map", "Metallic (R) and Smoothness (A)");
            public static GUIContent smoothnessText = new GUIContent("Smoothness", "Smoothness value");
            public static GUIContent smoothnessScaleText = new GUIContent("Smoothness", "Smoothness scale factor");
            public static GUIContent smoothnessMapChannelText = new GUIContent("Source", "Smoothness texture and channel");
            public static GUIContent highlightsText = new GUIContent("Specular Highlights", "Specular Highlights");
            public static GUIContent reflectionsText = new GUIContent("Environment Reflections", "Glossy Reflections");
            public static GUIContent normalMapText = new GUIContent("Normal Map", "Normal Map");
            public static GUIContent occlusionText = new GUIContent("Occlusion Map", "Occlusion (G)");
            public static GUIContent bumpScaleNotSupported = new GUIContent("Bump scale is not supported on mobile platforms");
            public static GUIContent fixNow = new GUIContent("Fix now");

            public static string surfaceInputs = "Surface Inputs";
            public static string workflowModeText = "Workflow Mode";
            public static readonly string[] workflowNames = Enum.GetNames(typeof(WorkflowMode));
            public static readonly string[] metallicSmoothnessChannelNames = {"Metallic Alpha", "Albedo Alpha"};
            public static readonly string[] specularSmoothnessChannelNames = {"Specular Alpha", "Albedo Alpha"};
        }

        private MaterialProperty workflowMode;

        private MaterialProperty smoothness;
        private MaterialProperty smoothnessScale;
        private MaterialProperty smoothnessMapChannel;

        private MaterialProperty metallic;
        private MaterialProperty specColor;
        private MaterialProperty metallicGlossMap;
        private MaterialProperty specGlossMap;
        private MaterialProperty highlights;
        private MaterialProperty reflections;

        private MaterialProperty bumpScale;
        private MaterialProperty bumpMap;
        private MaterialProperty occlusionStrength;
        private MaterialProperty occlusionMap;

        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);

            workflowMode = FindProperty("_WorkflowMode", properties);

            smoothness = FindProperty("_Glossiness", properties);
            smoothnessScale = FindProperty("_GlossMapScale", properties, false);
            smoothnessMapChannel = FindProperty("_SmoothnessTextureChannel", properties, false);

            metallic = FindProperty("_Metallic", properties);
            specColor = FindProperty("_SpecColor", properties);
            metallicGlossMap = FindProperty("_MetallicGlossMap", properties);
            specGlossMap = FindProperty("_SpecGlossMap", properties);
            highlights = FindProperty("_SpecularHighlights", properties);
            reflections = FindProperty("_GlossyReflections", properties);

            bumpScale = FindProperty("_BumpScale", properties);
            bumpMap = FindProperty("_BumpMap", properties);
            occlusionStrength = FindProperty("_OcclusionStrength", properties);
            occlusionMap = FindProperty("_OcclusionMap", properties);
        }

        public override void MaterialChanged(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            material.shaderKeywords = null;
            SetupMaterialBlendMode(material);
            SetMaterialKeywords(material);
        }

        public override void DrawSurfaceOptions(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;

            // Detect any changes to the material
            EditorGUI.BeginChangeCheck();
            {
                DoPopup(StylesLit.workflowModeText, workflowMode, StylesLit.workflowNames);
                base.DrawSurfaceOptions(material);
            }
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var obj in blendModeProp.targets)
                    MaterialChanged((Material)obj);
            }
        }

        public override void DrawSurfaceInputs(Material material)
        {
            base.DrawSurfaceInputs(material);
            // Detect any changes to the material
            EditorGUI.BeginChangeCheck();
            {
                //DoAlbedoArea();
                DoMetallicSpecularArea();
                DoNormalArea();

                materialEditor.TexturePropertySingleLine(StylesLit.occlusionText, occlusionMap, occlusionMap.textureValue != null ? occlusionStrength : null);

                DrawEmissionProperties(material, true);

                DrawBaseTileOffset();
                EditorGUI.BeginChangeCheck();
            }
        }

        public override void DrawAdvancedOptions(Material material)
        {
            EditorGUI.BeginChangeCheck();
            {
                materialEditor.ShaderProperty(highlights, StylesLit.highlightsText);
                materialEditor.ShaderProperty(reflections, StylesLit.reflectionsText);
            }
            base.DrawAdvancedOptions(material);
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            // _Emission property is lost after assigning Standard shader to the material
            // thus transfer it before assigning the new shader
            if (material.HasProperty("_Emission"))
            {
                material.SetColor("_EmissionColor", material.GetColor("_Emission"));
            }

            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            if (oldShader == null || !oldShader.name.Contains("Legacy Shaders/"))
            {
                SetupMaterialBlendMode(material);
                return;
            }

            SurfaceType surfaceType = SurfaceType.Opaque;
            BlendMode blendMode = BlendMode.Alpha;
            if (oldShader.name.Contains("/Transparent/Cutout/"))
            {
                surfaceType = SurfaceType.Opaque;
                material.SetFloat("_AlphaClip", 1);
            }
            else if (oldShader.name.Contains("/Transparent/"))
            {
                // NOTE: legacy shaders did not provide physically based transparency
                // therefore Fade mode
                surfaceType = SurfaceType.Transparent;
                blendMode = BlendMode.Alpha;
            }
            material.SetFloat("_Surface", (float)surfaceType);
            material.SetFloat("_Blend", (float)blendMode);

            if (oldShader.name.Equals("Standard (Specular setup)"))
            {
                material.SetFloat("_WorkflowMode", (float)WorkflowMode.Specular);
                Texture texture = material.GetTexture("_SpecGlossMap");
                if (texture != null)
                    material.SetTexture("_MetallicSpecGlossMap", texture);
            }
            else
            {
                material.SetFloat("_WorkflowMode", (float)WorkflowMode.Metallic);
                Texture texture = material.GetTexture("_MetallicGlossMap");
                if (texture != null)
                    material.SetTexture("_MetallicSpecGlossMap", texture);
            }

            MaterialChanged(material);
        }

        void DoNormalArea()
        {
            materialEditor.TexturePropertySingleLine(StylesLit.normalMapText, bumpMap, bumpMap.textureValue != null ? bumpScale : null);
            if (bumpScale.floatValue != 1 && UnityEditorInternal.InternalEditorUtility.IsMobilePlatform(EditorUserBuildSettings.activeBuildTarget))
                if (materialEditor.HelpBoxWithButton(StylesLit.bumpScaleNotSupported, StylesLit.fixNow))
                    bumpScale.floatValue = 1;
        }

        void DoMetallicSpecularArea()
        {
            string[] metallicSpecSmoothnessChannelName;
            bool hasGlossMap = false;
            if ((WorkflowMode)workflowMode.floatValue == WorkflowMode.Metallic)
            {
                hasGlossMap = metallicGlossMap.textureValue != null;
                metallicSpecSmoothnessChannelName = StylesLit.metallicSmoothnessChannelNames;
                materialEditor.TexturePropertySingleLine(StylesLit.metallicMapText, metallicGlossMap,
                    hasGlossMap ? null : metallic);
            }
            else
            {
                hasGlossMap = specGlossMap.textureValue != null;
                metallicSpecSmoothnessChannelName = StylesLit.specularSmoothnessChannelNames;
                materialEditor.TexturePropertySingleLine(StylesLit.specularMapText, specGlossMap,
                    hasGlossMap ? null : specColor);
            }

            bool showSmoothnessScale = hasGlossMap;
            if (smoothnessMapChannel != null)
            {
                int smoothnessChannel = (int)smoothnessMapChannel.floatValue;
                if (smoothnessChannel == (int)SmoothnessMapChannel.AlbedoAlpha)
                    showSmoothnessScale = true;
            }

            int indentation = 2; // align with labels of texture properties
            materialEditor.ShaderProperty(showSmoothnessScale ? smoothnessScale : smoothness, showSmoothnessScale ? StylesLit.smoothnessScaleText : StylesLit.smoothnessText, indentation);

            int prevIndentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 3;
            if (smoothnessMapChannel != null)
                DoPopup(StylesLit.smoothnessMapChannelText.text, smoothnessMapChannel, metallicSpecSmoothnessChannelName);
            EditorGUI.indentLevel = prevIndentLevel;
        }

        static SmoothnessMapChannel GetSmoothnessMapChannel(Material material)
        {
            int ch = (int)material.GetFloat("_SmoothnessTextureChannel");
            if (ch == (int)SmoothnessMapChannel.AlbedoAlpha)
                return SmoothnessMapChannel.AlbedoAlpha;

            return SmoothnessMapChannel.SpecularMetallicAlpha;
        }

        static void SetMaterialKeywords(Material material)
        {
            // Note: keywords must be based on Material value not on MaterialProperty due to multi-edit & material animation
            // (MaterialProperty value might come from renderer material property block)
            bool isSpecularWorkFlow = (WorkflowMode)material.GetFloat("_WorkflowMode") == WorkflowMode.Specular;
            bool hasGlossMap = false;
            if (isSpecularWorkFlow)
                hasGlossMap = material.GetTexture("_SpecGlossMap");
            else
                hasGlossMap = material.GetTexture("_MetallicGlossMap");

            CoreUtils.SetKeyword(material, "_SPECULAR_SETUP", isSpecularWorkFlow);

            CoreUtils.SetKeyword(material, "_METALLICSPECGLOSSMAP", hasGlossMap);
            CoreUtils.SetKeyword(material, "_SPECGLOSSMAP", hasGlossMap && isSpecularWorkFlow);
            CoreUtils.SetKeyword(material, "_METALLICGLOSSMAP", hasGlossMap && !isSpecularWorkFlow);

            CoreUtils.SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap"));

            CoreUtils.SetKeyword(material, "_SPECULARHIGHLIGHTS_OFF", material.GetFloat("_SpecularHighlights") == 0.0f);
            CoreUtils.SetKeyword(material, "_GLOSSYREFLECTIONS_OFF", material.GetFloat("_GlossyReflections") == 0.0f);

            CoreUtils.SetKeyword(material, "_OCCLUSIONMAP", material.GetTexture("_OcclusionMap"));
            CoreUtils.SetKeyword(material, "_PARALLAXMAP", material.GetTexture("_ParallaxMap"));
            CoreUtils.SetKeyword(material, "_DETAIL_MULX2", material.GetTexture("_DetailAlbedoMap") || material.GetTexture("_DetailNormalMap"));

            CoreUtils.SetKeyword(material, "_RECEIVE_SHADOWS_OFF", material.GetFloat("_ReceiveShadows") == 0.0f);

            // A material's GI flag internally keeps track of whether emission is enabled at all, it's enabled but has no effect
            // or is enabled and may be modified at runtime. This state depends on the values of the current flag and emissive color.
            // The fixup routine makes sure that the material is in the correct state if/when changes are made to the mode or color.
            MaterialEditor.FixupEmissiveFlag(material);
            bool shouldEmissionBeEnabled = (material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.EmissiveIsBlack) == 0;
            CoreUtils.SetKeyword(material, "_EMISSION", shouldEmissionBeEnabled);

            if (material.HasProperty("_SmoothnessTextureChannel"))
            {
                CoreUtils.SetKeyword(material, "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A", GetSmoothnessMapChannel(material) == SmoothnessMapChannel.AlbedoAlpha);
            }
        }
    }
}
