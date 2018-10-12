using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.LightweightPipeline
{
    internal class SimpleLitShaderGUI : BaseShaderGUI
    {
        private const float kMinShininessValue = 0.01f;
        private MaterialProperty specHighlights;
        private MaterialProperty smoothnessSourceProp;
        private MaterialProperty specularGlossMapProp;
        private MaterialProperty specularColorProp;
        private MaterialProperty shininessProp;
        private MaterialProperty bumpMapProp;

        private static class StylesSimpleLit
        {
            public static GUIContent specularMapLabel = new GUIContent("Specular Map", "Specular Color (RGB)");
            public static readonly string[] smoothnessSourceNames = {"Specular Alpha", "Albedo Alpha"};
            public static GUIContent smoothnessSource = new GUIContent("Smoothness Source", "Here you can choose where the Map based smoothness comes from.");
            
            public static GUIContent normalMapText = new GUIContent("Normal Map", "Normal Map");
            
            public static GUIContent highlightsText = new GUIContent("Specular Highlights", "Specular Highlights");
        }

        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
            specHighlights = FindProperty("_SpecularHighlights", properties);
            smoothnessSourceProp = FindProperty("_SmoothnessSource", properties);
            specularGlossMapProp = FindProperty("_SpecMap", properties);
            specularColorProp = FindProperty("_SpecColor", properties);
            bumpMapProp = FindProperty("_BumpMap", properties);
            emissionMapProp = FindProperty("_EmissionMap", properties);
            emissionColorProp = FindProperty("_EmissionColor", properties);
        }

        public override void DrawSurfaceOptions(Material material)
        {
            EditorGUI.BeginChangeCheck();
            {
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
            
            EditorGUI.BeginChangeCheck();
            {
                DoSpecular();

                materialEditor.TexturePropertySingleLine(StylesSimpleLit.normalMapText, bumpMapProp);

                DrawEmissionProperties(material, true);

                DrawBaseTileOffset();
                EditorGUI.BeginChangeCheck();
            }
        }

        public override void DrawAdvancedOptions(Material material)
        {
            SpecularSource specularSource = (SpecularSource)specHighlights.floatValue;
            EditorGUI.BeginChangeCheck();
            bool enabled = EditorGUILayout.Toggle(StylesSimpleLit.highlightsText, specularSource == SpecularSource.SpecularTextureAndColor);
            if (EditorGUI.EndChangeCheck())
                specHighlights.floatValue = enabled ? (float)SpecularSource.SpecularTextureAndColor : (float)SpecularSource.NoSpecular;
            base.DrawAdvancedOptions(material);
            
        }
        
        public override void MaterialChanged(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            material.shaderKeywords = null;
            SetupMaterialBlendMode(material);
            SetMaterialKeywords(material);
        }

        private void SetMaterialKeywords(Material material)
        {
            material.shaderKeywords = null;
            SetupMaterialBlendMode(material);
            UpdateMaterialSpecularSource(material);
            CoreUtils.SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap"));

            // A material's GI flag internally keeps track of whether emission is enabled at all, it's enabled but has no effect
            // or is enabled and may be modified at runtime. This state depends on the values of the current flag and emissive color.
            // The fixup routine makes sure that the material is in the correct state if/when changes are made to the mode or color.
            MaterialEditor.FixupEmissiveFlag(material);
            bool shouldEmissionBeEnabled = (material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.EmissiveIsBlack) == 0;
            CoreUtils.SetKeyword(material, "_EMISSION", shouldEmissionBeEnabled);

            CoreUtils.SetKeyword(material, "_RECEIVE_SHADOWS_OFF", material.GetFloat("_ReceiveShadows") == 0.0f);
        }

        private void UpdateMaterialSpecularSource(Material material)
        {
            SpecularSource specSource = (SpecularSource)material.GetFloat("_SpecularHighlights");
            if (specSource == SpecularSource.NoSpecular)
            {
                CoreUtils.SetKeyword(material, "_SPECGLOSSMAP", false);
                CoreUtils.SetKeyword(material, "_SPECULAR_COLOR", false);
                CoreUtils.SetKeyword(material, "_GLOSSINESS_FROM_BASE_ALPHA", false);
            }
            else
            {
                var smoothnessSource = (SmoothnessSource)material.GetFloat("_SmoothnessSource");
                bool hasMap = material.GetTexture("_SpecMap");
                CoreUtils.SetKeyword(material, "_SPECGLOSSMAP", hasMap);
                CoreUtils.SetKeyword(material, "_SPECULAR_COLOR", !hasMap);
                CoreUtils.SetKeyword(material, "_GLOSSINESS_FROM_BASE_ALPHA", smoothnessSource == SmoothnessSource.BaseAlpha);
            }
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            if (oldShader == null)
                throw new ArgumentNullException("oldShader");

            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            // Shininess value cannot be zero since it will produce undefined values for cases where pow(0, 0).
            float shininess = material.GetFloat("_Shininess");
            material.SetFloat("_Shininess", Mathf.Clamp(shininess, kMinShininessValue, 1.0f));

            string oldShaderName = oldShader.name;
            string[] shaderStrings = oldShaderName.Split('/');

            if (shaderStrings[0].Equals("Legacy Shaders") || shaderStrings[0].Equals("Mobile"))
            {
                ConvertFromLegacy(material, oldShaderName);
            }

            StandardSimpleLightingUpgrader.UpdateMaterialKeywords(material);
        }

        private bool RequiresAlpha()
        {
            SurfaceType surfaceType = (SurfaceType)surfaceTypeProp.floatValue;
            return alphaClipProp.floatValue > 0.0f || surfaceType == SurfaceType.Transparent;
        }

        private void DoSpecular()
        {
            SpecularSource specSource = (SpecularSource)specHighlights.floatValue;
            EditorGUI.BeginDisabledGroup(specSource == SpecularSource.NoSpecular);
            
            materialEditor.TexturePropertySingleLine(StylesSimpleLit.specularMapLabel, specularGlossMapProp, specularColorProp);

            EditorGUI.indentLevel += 2;
            //if (RequiresAlpha())
            //{
                //EditorGUI.BeginDisabledGroup(true);
                //smoothnessSourceProp.floatValue = (float)EditorGUILayout.Popup(StylesSimpleLit.smoothnessSource, (int)GlossinessSource.SpecularAlpha, StylesSimpleLit.smoothnessSourceNames);
                //EditorGUI.EndDisabledGroup();
            //}
            //else
            //{
                int glossinessSource = (int)smoothnessSourceProp.floatValue;
                EditorGUI.BeginChangeCheck();
                glossinessSource = EditorGUILayout.Popup(StylesSimpleLit.smoothnessSource, glossinessSource, StylesSimpleLit.smoothnessSourceNames);
                if (EditorGUI.EndChangeCheck())
                    smoothnessSourceProp.floatValue = glossinessSource;
            //}

            EditorGUI.indentLevel -= 2;
            
            EditorGUI.EndDisabledGroup();
        }

        private void ConvertFromLegacy(Material material, string oldShaderName)
        {
            UpgradeParams shaderUpgradeParams = new UpgradeParams();

            if (oldShaderName.Contains("Transp"))
            {
                shaderUpgradeParams.surfaceType = UpgradeSurfaceType.Transparent;
                shaderUpgradeParams.blendMode = UpgradeBlendMode.Alpha;
                shaderUpgradeParams.alphaClip = false;
                shaderUpgradeParams.glosinessSource = GlossinessSource.SpecularAlpha;
            }
            else if (oldShaderName.Contains("Cutout"))
            {
                shaderUpgradeParams.surfaceType = UpgradeSurfaceType.Opaque;
                shaderUpgradeParams.blendMode = UpgradeBlendMode.Alpha;
                shaderUpgradeParams.alphaClip = true;
                shaderUpgradeParams.glosinessSource = GlossinessSource.SpecularAlpha;
            }
            else
            {
                shaderUpgradeParams.surfaceType = UpgradeSurfaceType.Opaque;
                shaderUpgradeParams.blendMode = UpgradeBlendMode.Alpha;
                shaderUpgradeParams.alphaClip = false;
                shaderUpgradeParams.glosinessSource = GlossinessSource.BaseAlpha;
            }

            if (oldShaderName.Contains("Spec"))
                shaderUpgradeParams.specularSource = SpecularSource.SpecularTextureAndColor;
            else
                shaderUpgradeParams.specularSource = SpecularSource.NoSpecular;

            material.SetFloat("_Surface", (float)shaderUpgradeParams.surfaceType);
            material.SetFloat("_Blend", (float)shaderUpgradeParams.blendMode);
            material.SetFloat("_SpecSource", (float)shaderUpgradeParams.specularSource);
            material.SetFloat("_GlossinessSource", (float)shaderUpgradeParams.glosinessSource);

            if (oldShaderName.Contains("Self-Illumin"))
            {
                material.SetTexture("_EmissionMap", material.GetTexture("_MainTex"));
                material.SetTexture("_MainTex", null);
                material.SetColor("_EmissionColor", Color.white);
            }
        }
    }
}
