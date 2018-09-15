using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
            { "HDRenderPipeline/TerrainLit", TerrainLitGUI.SetupMaterialKeywordsAndPass },
            { StackLitGUI.k_StackLitShaderName, StackLitGUI.SetupMaterialKeywordsAndPass },
        };

        const string k_MaterialShaderNameRegexPattern = @"\A"
            + TextureSamplerSharingShaderGenerator.k_StackLitFamilyFindRegexPattern //+ @"(?<shadername>HDRenderPipeline\/StackLit)(\/Generated\/(?<statecode>[0-9a-fA-F]{32}))?"
            //+ @"(?<shadername>HDRenderPipeline\/LayeredLit)"
            //+ @"|(?<shadername>HDRenderPipeline\/LayeredLitTessellation)"
            //+ @"|(?<shadername>HDRenderPipeline\/Unlit)"
            //+ @"|(?<shadername>HDRenderPipeline\/Fabric)"
            //+ @"|(?<shadername>HDRenderPipeline\/Decal)"
            //+ @"|(?<shadername>HDRenderPipeline\/TerrainLit)"
            + @"\z";
        static Regex k_MaterialShaderNameRegex = new Regex(k_MaterialShaderNameRegexPattern, RegexOptions.ExplicitCapture| RegexOptions.Compiled);

        private static bool TryGetMaterialResetter(string shaderName, out MaterialResetter resetter)
        {
            // First try to find without filtering the name
            if (k_MaterialResetters.TryGetValue(shaderName, out resetter))
            {
                return true;
            }
            Match match = k_MaterialShaderNameRegex.Match(shaderName);
            if (match.Success)
            {
                shaderName = match.Groups["shadername"].Value;
                if (k_MaterialResetters.TryGetValue(shaderName, out resetter))
                {
                    return true;
                }
            }
            return false;
        }

        public static T LoadAsset<T>(string relativePath) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(HDUtils.GetHDRenderPipelinePath() + relativePath);
        }

        public static bool ResetMaterialKeywords(Material material)
        {
            MaterialResetter resetter;
            if (TryGetMaterialResetter(material.shader.name, out resetter))
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
    }
}
