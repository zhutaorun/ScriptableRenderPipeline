using System;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class StackLitAssetObserver : AssetPostprocessor
    {
        private static void SaveAssetsAndFreeMemory()
        {
            AssetDatabase.SaveAssets();
            System.GC.Collect();
            EditorUtility.UnloadUnusedAssetsImmediate();
            AssetDatabase.Refresh();
        }

        private static void UpdateStackLitMaterials(bool checkShaderDependencies = false, string reimportedTexturePath = null, bool markDirtyAndSave = false)
        {
            Shader stacklitShader = Shader.Find(StackLitGUI.k_StacklitShaderName);
            if (stacklitShader == null)
            {
                Debug.LogWarning("Cannot find StackLit shader!");
                return;
            }

            int definedSharedSamplerUsedNum = 0;
            if (!TextureSamplerSharingShaderGenerator.GetOriginalShaderDefinedSharedSamplerUseNum(ref definedSharedSamplerUsedNum, stacklitShader))
            {
                Debug.LogWarning("Cannot find " + TextureSamplerSharingShaderGenerator.k_SharedSamplerUsedNumDefine + " in StackLit shader!");
                return;
            }

            int count = 0;
            int numMaterialsUpdated = 0;
            foreach (string s in AssetDatabase.GetAllAssetPaths())
            {
                if (s.EndsWith(".mat", StringComparison.InvariantCultureIgnoreCase))
                {
                    count++;
                }
            }

            int i = 0;
            List<string> alreadyUpdatedMaterials = new List<string>();
            foreach (string path in AssetDatabase.GetAllAssetPaths())
            {
                if (path.EndsWith(".mat", StringComparison.InvariantCultureIgnoreCase))
                {
                    i++;
                    if (EditorUtility.DisplayCancelableProgressBar("Scanning materials for those using StackLit for dependencies and update.",
                        string.Format("({0} of {1}) {2}", i, count, path), (float)i / (float)count))
                    {
                        break;
                    }

                    Material mat = AssetDatabase.LoadMainAssetAtPath(path) as Material;
                    if (mat == null || !mat.name.StartsWith("")) { continue; }
                    bool shaderHasStackLitGeneratedName = mat.shader.name.StartsWith(TextureSamplerSharingShaderGenerator.k_StacklitGeneratedShaderNamePrefix);
                    if (mat.shader != stacklitShader && !shaderHasStackLitGeneratedName) { continue; }
                    if (alreadyUpdatedMaterials.Contains(path)) { continue; }

                    alreadyUpdatedMaterials.Add(path);

                    bool materialDependsOnReimportedTexture = false;
                    if (reimportedTexturePath != null)
                    {
                        string[] matDependencies = AssetDatabase.GetDependencies(path);
                        var res = matDependencies.Where(p => p.Equals(reimportedTexturePath, StringComparison.InvariantCultureIgnoreCase));
                        if (res.Count() > 0)
                        {
                            Debug.LogFormat("{0} depends on just (re)imported {1}, will call SetupMaterialKeywordsAndPass.", path, reimportedTexturePath);
                            materialDependsOnReimportedTexture = true;
                        }
                    }

                    //if (TextureSamplerSharing.CheckUpdateMaterialSharedSamplerUsedNumDefineProperty(m, 0.0f))
                    bool refreshSharedSamplerUsedNumDefineProperty = checkShaderDependencies && TextureSamplerSharing.CheckUpdateMaterialSharedSamplerUsedNumDefineProperty(mat, definedSharedSamplerUsedNum);
                    if (materialDependsOnReimportedTexture || refreshSharedSamplerUsedNumDefineProperty)
                    {
                        // Note: it is the responsibility of the material to enable auto-generation when it was generated
                        // and it wants to support a needed config modification (and thus regeneration) in case a texture
                        // importer has changed
                        StackLitGUI.SetupMaterialKeywordsAndPassWithOptions(mat, false, false, refreshSharedSamplerUsedNumDefineProperty: refreshSharedSamplerUsedNumDefineProperty);
                        numMaterialsUpdated++;
                    }
                }
            }

            if (markDirtyAndSave)
            {
                if (numMaterialsUpdated > 0)
                {
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                }
                // TODO this is too heavy and causes some bugs that require editor restart
                SaveAssetsAndFreeMemory();
            }
            EditorUtility.ClearProgressBar();
        }


        public static readonly string[] TextureImageExtensions = { ".bmp", ".exr", ".gif", ".hdr", ".iff", ".jpeg", ".jpg", ".pict", ".png", ".psd", ".tga", ".tiff" };
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool masterShaderHasChanged = false;


            foreach( var path in importedAssets)
            {
                if (path.Equals(StackLitGUI.k_StackLitPackagedShaderPath, StringComparison.InvariantCultureIgnoreCase))
                {
                    Debug.Log("StackLit shader (re)imported...");
                    masterShaderHasChanged = true;
                }
                bool assetIsTexture = TextureImageExtensions.Any(s => Path.GetExtension(path).ToLowerInvariant().Equals(s, StringComparison.InvariantCultureIgnoreCase));
                if (assetIsTexture)
                {
                    Debug.LogFormat("{0} texture (re)imported...", path);
                }
                if (masterShaderHasChanged || assetIsTexture)
                {
                    UpdateStackLitMaterials(checkShaderDependencies: masterShaderHasChanged, reimportedTexturePath: (assetIsTexture ? path : null));
                }
                if (masterShaderHasChanged)
                {
                    // We only need to signal stacklit materials' dependencies on the master shader once
                    masterShaderHasChanged = false;
                }
            }
        }
    }
}
