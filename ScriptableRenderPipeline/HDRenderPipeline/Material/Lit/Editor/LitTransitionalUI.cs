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
        protected MaterialProperty detailMapLegacy = new MaterialProperty();
        protected const string kDetailMapLegacy = "_DetailMapLegacy";
        protected MaterialProperty detailMaskMapLegacy = new MaterialProperty();
        protected const string kDetailMaskMapLegacy = "_DetailMaskMapLegacy";

        protected static class Styles
        {
            public static GUIContent occlusionMapText = new GUIContent("Occlusion Map", "Occlusion Map");
            public static GUIContent detailMaskMapLegacyText = new GUIContent("Detail Mask Map", "Detail Mask Map");
            public static GUIContent detailMapLegacyText = new GUIContent("Detail Map", "Detail Map");
            public static string LegacyText = "Legacy";
        }

        protected override void MaterialPropertiesGUI(Material material)
        {
            EditorGUILayout.LabelField(Styles.LegacyText, EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            m_MaterialEditor.TexturePropertySingleLine(Styles.occlusionMapText, occlusionMap);
            m_MaterialEditor.TexturePropertySingleLine(Styles.detailMapLegacyText, detailMapLegacy);
            m_MaterialEditor.TexturePropertySingleLine(Styles.detailMaskMapLegacyText, detailMaskMapLegacy);
            EditorGUI.indentLevel--;
            base.MaterialPropertiesGUI(material);

        }

        protected override void FindMaterialProperties(MaterialProperty[] props)
        {
            base.FindMaterialProperties(props);
            occlusionMap = FindProperty(string.Format("{0}{1}", kOcclusionMap, ""), props);
            detailMapLegacy = FindProperty(string.Format("{0}{1}", kDetailMapLegacy, ""), props);
            detailMaskMapLegacy = FindProperty(string.Format("{0}{1}", kDetailMaskMapLegacy, ""), props);
        }

        static public void SetupLitTransitionalKeywords(Material material)
        {
            SetKeyword(material, "_OCCLUSIONMAP", material.GetTexture(kOcclusionMap));
            SetKeyword(material, "_DETAIL_MAP_LEGACY", material.GetTexture(kDetailMapLegacy));
            SetKeyword(material, "_DETAIL_MASK_MAP_LEGACY", material.GetTexture(kDetailMaskMapLegacy));            
        }

        protected override void SetupMaterialKeywordsAndPassInternal(Material material)
        {
            base.SetupMaterialKeywordsAndPassInternal(material);
            SetupLitTransitionalKeywords(material);
        }

        static public void SetupMaterialKeywordsAndPass(Material material)
        {
            LitGUI.SetupMaterialKeywordsAndPass(material);
            SetupLitTransitionalKeywords(material);
        }
    }
}
