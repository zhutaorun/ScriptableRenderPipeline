using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;


namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class LitTransitionalGUI : LitGUI
    {
        protected MaterialProperty occlusionMap = new MaterialProperty();
        protected const string kOcclusionMap = "_OcclusionMap";

        protected static class Styles
        {
            public static GUIContent occlusionMapText = new GUIContent("Occlusion Map", "Occlusion Map");
        }

        protected override void MaterialPropertiesGUI(Material material)
        {
            base.MaterialPropertiesGUI(material);
            m_MaterialEditor.TexturePropertySingleLine(Styles.occlusionMapText, occlusionMap);
        }

        protected override void FindMaterialProperties(MaterialProperty[] props)
        {
            base.FindMaterialProperties(props);
            occlusionMap = FindProperty(string.Format("{0}{1}", kOcclusionMap, ""), props);
        }

        protected override void SetupMaterialKeywordsAndPassInternal(Material material)
        {
            base.SetupMaterialKeywordsAndPassInternal(material);
            SetKeyword(material, "_OCCLUSIONMAP", material.GetTexture(kOcclusionMap));
        }
    }
}
