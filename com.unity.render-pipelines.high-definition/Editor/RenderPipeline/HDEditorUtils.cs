using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class HDEditorUtils
    {
        delegate void MaterialResetter(Material material);
        static Dictionary<string, MaterialResetter> k_MaterialResetters = new Dictionary<string, MaterialResetter>()
        {
            { "HDRenderPipeline/LayeredLit",  LayeredLitGUI.SetupMaterialKeywordsAndPass },
            { "HDRenderPipeline/LayeredLitTessellation", LayeredLitGUI.SetupMaterialKeywordsAndPass },
            { "HDRenderPipeline/Lit", LitGUI.SetupMaterialKeywordsAndPass },
            { "HDRenderPipeline/LitTessellation", LitGUI.SetupMaterialKeywordsAndPass },
            { "HDRenderPipeline/Unlit", UnlitGUI.SetupMaterialKeywordsAndPass },
            { "HDRenderPipeline/Fabric",  FabricGUI.SetupMaterialKeywordsAndPass },
            { "HDRenderPipeline/Decal", DecalUI.SetupMaterialKeywordsAndPass },
            { "HDRenderPipeline/TerrainLit", TerrainLitGUI.SetupMaterialKeywordsAndPass }
        };

        public static T LoadAsset<T>(string relativePath) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(HDUtils.GetHDRenderPipelinePath() + relativePath);
        }

        public static bool ResetMaterialKeywords(Material material)
        {
            MaterialResetter resetter;
            if (k_MaterialResetters.TryGetValue(material.shader.name, out resetter))
            {
                CoreEditorUtils.RemoveMaterialKeywords(material);
                // We need to reapply ToggleOff/Toggle keyword after reset via ApplyMaterialPropertyDrawers
                MaterialEditor.ApplyMaterialPropertyDrawers(material);
                resetter(material);
                EditorUtility.SetDirty(material);
                return true;
            }
            return false;
        }

        public static List<BaseShaderPreprocessor> GetBaseShaderPreprocessorList()
        {
            var baseType = typeof(BaseShaderPreprocessor);
            var assembly = baseType.Assembly;

            var types = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(baseType))
                .Select(Activator.CreateInstance)
                .Cast<BaseShaderPreprocessor>()
                .ToList();

            return types;
        }

        static readonly GUIContent s_OverrideTooltip = CoreEditorUtils.GetContent("|Override this setting in component.");
        public static void FlagToggle<TEnum>(TEnum v, SerializedProperty property)
            where TEnum : struct, IConvertible // restrict to ~enum
        {
            var intV = (int)(object)v;
            var isOn = (property.intValue & intV) != 0;
            isOn = GUILayout.Toggle(isOn, s_OverrideTooltip, CoreEditorStyles.smallTickbox);
            if (isOn)
                property.intValue |= intV;
            else
                property.intValue &= ~intV;
        }

        public static void PropertyFieldWithFlagToggle<TEnum>(
            TEnum v, SerializedProperty property, GUIContent label, SerializedProperty @override
        )
            where TEnum : struct, IConvertible // restrict to ~enum
        {
            EditorGUILayout.BeginHorizontal();
            FlagToggle(v, @override);
            EditorGUILayout.PropertyField(property, label);
            EditorGUILayout.EndHorizontal();
        }

        public static void PropertyFieldWithFlagToggleIfDisplayed<TEnum>(
            TEnum v, SerializedProperty property, GUIContent label, SerializedProperty @override,
            TEnum displayed
        )
            where TEnum : struct, IConvertible // restrict to ~enum
        {
            var intDisplayed = (int)(object)displayed;
            var intV = (int)(object)v;
            if ((intDisplayed & intV) == intV)
                PropertyFieldWithFlagToggle(v, property, label, @override);
        }

        public static bool DrawSectionFoldout(string title, bool isExpanded)
        {
            CoreEditorUtils.DrawSplitter(false);
            return CoreEditorUtils.DrawHeaderFoldout(title, isExpanded, false);
        }
    }
}
