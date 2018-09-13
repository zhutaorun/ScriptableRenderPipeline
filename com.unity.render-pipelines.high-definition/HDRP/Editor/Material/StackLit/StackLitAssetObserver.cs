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
#if UNITY_MATERIAL_AUTO_REFRESH
    public class StackLitAssetObserver : AssetPostprocessor
    {
        private static void SaveAssetsAndFreeMemory()
        {
            AssetDatabase.SaveAssets();
            System.GC.Collect();
            EditorUtility.UnloadUnusedAssetsImmediate();
            AssetDatabase.Refresh();
        }

        private static IEnumerable<T> FindLoadAssets<T>(string filter) where T : UnityEngine.Object
        {
            string[] assetsGUIDs = AssetDatabase.FindAssets(filter);
            return assetsGUIDs.Select(x => AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(x), typeof(T)) as T);
        }

        private static IEnumerable<KeyValuePair<T, string>> FindLoadAssetsAndPath<T>(string filter, out int count) where T : UnityEngine.Object
        {
            string[] assetsGUIDs = AssetDatabase.FindAssets(filter);
            count = assetsGUIDs.Length;
            return assetsGUIDs.Select(x =>
            {
                return new KeyValuePair<T, string>(AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(x), typeof(T)) as T, AssetDatabase.GUIDToAssetPath(x));
            });
        }

        // TODO: check collab works, crawl scenes too (cf ResetAllMaterialKeywordsInProjectAndScenes)
        private static void UpdateStackLitMaterials(string[] reImportedTextures = null, bool markSceneDirtyAndSave = false, bool useGetAllAssetPaths = false)
        {
            Shader stacklitShader = Shader.Find(StackLitGUI.k_StackLitShaderName);
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

            int numMaterialsUpdated = 0;

            try
            {
                IEnumerable<KeyValuePair<Material, string>> materialsWithPath = null;

                int count = 0;

                if (useGetAllAssetPaths)
                {
                    foreach (string s in AssetDatabase.GetAllAssetPaths())
                    {
                        if (s.EndsWith(".mat", StringComparison.InvariantCultureIgnoreCase))
                        {
                            count++;
                        }
                    }
                }
                else
                {
                    materialsWithPath = FindLoadAssetsAndPath<Material>("t:Material", out count);
                }

                int i = 0;
                bool VCSEnabled = (UnityEditor.VersionControl.Provider.enabled && UnityEditor.VersionControl.Provider.isActive);

                List<string> alreadyUpdatedMaterials = useGetAllAssetPaths? new List<string>() : null;

                System.Collections.IEnumerable seq = useGetAllAssetPaths ? AssetDatabase.GetAllAssetPaths() : (System.Collections.IEnumerable) materialsWithPath;
                var seqEnumerator = seq.GetEnumerator();
                while (seqEnumerator.MoveNext())
                {
                    string path = useGetAllAssetPaths ? ((string)seqEnumerator.Current) : ((KeyValuePair<Material, string>)seqEnumerator.Current).Value;

                    Material mat;
                    if (useGetAllAssetPaths)
                    {
                        if (!path.EndsWith(".mat", StringComparison.InvariantCultureIgnoreCase))
                            continue;
                        mat = AssetDatabase.LoadMainAssetAtPath(path) as Material;
                    }
                    else
                    {
                        mat = ((KeyValuePair<Material, string>)seqEnumerator.Current).Key;
                    }

                    i++;
                    if (EditorUtility.DisplayCancelableProgressBar("Scanning materials for those using StackLit for dependencies and update.",
                        string.Format("({0} of {1}) {2}", i, count, path), (float)i / (float)count))
                    {
                        break;
                    }

                    // Check the material uses StackLit (generated or main shader)
                    if (mat == null || !mat.name.StartsWith("")) { continue; }
                    bool shaderHasStackLitGeneratedName = mat.shader.name.StartsWith(TextureSamplerSharingShaderGenerator.k_StackLitGeneratedShaderNamePrefix);
                    if (mat.shader != stacklitShader && !shaderHasStackLitGeneratedName) { continue; }

                    if (useGetAllAssetPaths)
                    {
                        if (alreadyUpdatedMaterials.Contains(path)) { continue; }
                        alreadyUpdatedMaterials.Add(path);
                    }

                    // Check if we have texture paths given to check for depencies:
                    bool materialDependsOnReimportedTexture = false;
                    if (reImportedTextures != null && reImportedTextures.Length != 0)
                    {
                        string[] matDependencies = AssetDatabase.GetDependencies(path);
                        var res = matDependencies.Where(p => reImportedTextures.Contains(p));
                        if (res.Count() > 0)
                        {
                            Debug.LogFormat("{0} depends on just (re)imported {1}, will call SetupMaterialKeywordsAndPass.", path, reImportedTextures);
                            materialDependsOnReimportedTexture = true;
                        }
                    }

                    // Check if we needed to update the material property caching the "#define SharedSamplerUsedNum" value from the main shader source file.
                    // (this define hardcodes the reserved number of shared samplers in the main shader)
                    bool refreshSharedSamplerUsedNumDefineProperty = TextureSamplerSharing.CheckUpdateMaterialSharedSamplerUsedNumDefineProperty(mat, definedSharedSamplerUsedNum);

                    // Finally, call the material update function if needed.
                    if (materialDependsOnReimportedTexture || refreshSharedSamplerUsedNumDefineProperty)
                    {
                        CoreEditorUtils.CheckOutFile(VCSEnabled, mat);

                        // Note: it is the responsibility of the material to enable auto-generation when it was generated
                        // and it wants to support a needed config modification (and thus regeneration) in case a texture
                        // importer has changed.
                        //StackLitGUI.SetupMaterialKeywordsAndPassWithOptions(mat, refreshSharedSamplerUsedNumDefineProperty: refreshSharedSamplerUsedNumDefineProperty);
                        //dont need this, we have already refreshed with TextureSamplerSharing.CheckUpdate...
                        StackLitGUI.SetupMaterialKeywordsAndPassWithOptions(mat);
                        EditorUtility.SetDirty(mat);
                        numMaterialsUpdated++;
                    }
                }//while

                if (markSceneDirtyAndSave)
                {
                    if (numMaterialsUpdated > 0)
                    {
                        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                    }
                    // TODO this is too heavy and causes some bugs that require editor restart
                    SaveAssetsAndFreeMemory();
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        public static readonly string[] TextureImageExtensions = { ".bmp", ".exr", ".gif", ".hdr", ".iff", ".jpeg", ".jpg", ".pict", ".png", ".psd", ".tga", ".tiff" };
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool k_RefreshAllMaterials = false; // See note below
            bool masterShaderHasChanged = false;

            List<string> reImportedTextures = new List<string>();
            foreach (var path in importedAssets)
            {
                bool haveReimportedTextures = TextureImageExtensions.Any(s => Path.GetExtension(path).ToLowerInvariant().Equals(s, StringComparison.InvariantCultureIgnoreCase));
                if (haveReimportedTextures)
                {
                    Debug.LogFormat("{0} texture (re)imported...", path);
                    reImportedTextures.Add(path);
                }
                masterShaderHasChanged = path.Equals(StackLitGUI.k_StackLitPackagedShaderPath, StringComparison.InvariantCultureIgnoreCase);
                if (masterShaderHasChanged)
                {
                    Debug.Log("StackLit shader (re)imported...");
                }
            }

            if (masterShaderHasChanged || reImportedTextures.Count != 0)
            {
                if (!k_RefreshAllMaterials)
                {
                    // In the end, it doesn't seem to save time to be discriminating and do this...
                    UpdateStackLitMaterials(reImportedTextures.ToArray());
                }
                else
                {
                    // ...instead of running the refresh for everything like this
                    // (Except maybe for the side effects of setting material dirty
                    // SetupMaterialKeywordsAndPass isn't that much an overhead)
                    HDRenderPipelineMenuItems.ResetAllMaterialAssetsKeywords();
                    // But crawling scenes though is expensive:
                    //HDRenderPipelineMenuItems.ResetAllMaterialKeywordsInProjectAndScenes();
                }
            }
        }
    }
#endif
}
