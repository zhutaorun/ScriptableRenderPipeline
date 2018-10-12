using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.LightweightPipeline
{
    internal class UnlitShaderGUI : BaseShaderGUI
    {
        private MaterialProperty sampleGIProp;
        private MaterialProperty bumpMap;

        private static class Styles
        {
            public static string surfaceProperties = "Surface Properties";
            public static GUIContent normalMapLabel = new GUIContent("Normal Map", "This property takes a Tangent Space Normal Map to add the illusion of more detail");
            public static GUIContent sampleGILabel = new GUIContent("Global Illumination", "If enabled Global Illumination will be sampled from Ambient lighting, Lightprobes or Lightmap.");
        }

        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
            sampleGIProp = FindProperty("_SampleGI", properties, false);
            bumpMap = FindProperty("_BumpMap", properties, false);
        }

        public override void DrawSurfaceOptions(Material material)
        {
            EditorGUI.BeginChangeCheck();
            {
                base.DrawSurfaceOptions(material);
            }
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var target in blendModeProp.targets)
                    MaterialChanged((Material)target);
            }
        }

        public override void DrawSurfaceInputs(Material material)
        {
            base.DrawSurfaceInputs(material);
            
            EditorGUI.BeginChangeCheck();
            {
                EditorGUI.BeginDisabledGroup(sampleGIProp.floatValue < 1.0);
                materialEditor.TexturePropertySingleLine(Styles.normalMapLabel, bumpMap);
                EditorGUI.EndDisabledGroup();
                DrawBaseTileOffset();
            }
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var target in blendModeProp.targets)
                    MaterialChanged((Material)target);
            }
        }

        public override void DrawAdvancedOptions(Material material)
        {
            EditorGUI.BeginChangeCheck();
            materialEditor.ShaderProperty(sampleGIProp, Styles.sampleGILabel);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var target in blendModeProp.targets)
                    MaterialChanged((Material)target);
            }
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

        static void SetMaterialKeywords(Material material)
        {
            bool sampleGI = material.GetFloat("_SampleGI") >= 1.0f;
            CoreUtils.SetKeyword(material, "_SAMPLE_GI", sampleGI);
            CoreUtils.SetKeyword(material, "_NORMAL_MAP", sampleGI && material.GetTexture("_BumpMap"));
        }
    }
}
