using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Experimental.Rendering.HDPipeline.Internal;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public static class EditorReflectionSystem
    {
        static int _Cubemap = Shader.PropertyToID("_Cubemap");
        const HideFlags k_ReflectionSystemDictionaryHideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector | HideFlags.DontSaveInBuild;
        static PlanarReflectionProbeBaker s_PlanarReflectionProbeBaker = new PlanarReflectionProbeBaker();
        static ReflectionProbeBaker s_ReflectionProbeBaker = new ReflectionProbeBaker();

        static List<ReflectionProbe> s_TmpReflectionProbeList = new List<ReflectionProbe>();
        static List<PlanarReflectionProbe> s_TmpPlanarReflectionProbeList = new List<PlanarReflectionProbe>();

        public static bool IsCollidingWithOtherProbes(string targetPath, ReflectionProbe targetProbe, out ReflectionProbe collidingProbe)
        {
            ReflectionProbe[] probes = Object.FindObjectsOfType<ReflectionProbe>().ToArray();
            collidingProbe = null;
            foreach (var probe in probes)
            {
                if (probe == targetProbe || probe.customBakedTexture == null)
                    continue;
                string path = AssetDatabase.GetAssetPath(probe.customBakedTexture);
                if (path == targetPath)
                {
                    collidingProbe = probe;
                    return true;
                }
            }
            return false;
        }

        public static bool IsCollidingWithOtherProbes(string targetPath, PlanarReflectionProbe targetProbe, out PlanarReflectionProbe collidingProbe)
        {
            PlanarReflectionProbe[] probes = Object.FindObjectsOfType<PlanarReflectionProbe>().ToArray();
            collidingProbe = null;
            foreach (var probe in probes)
            {
                if (probe == targetProbe || probe.customTexture == null)
                    continue;
                var path = AssetDatabase.GetAssetPath(probe.customTexture);
                if (path == targetPath)
                {
                    collidingProbe = probe;
                    return true;
                }
            }
            return false;
        }

        public static void BakeCustomReflectionProbe(PlanarReflectionProbe probe, bool usePreviousAssetPath)
        {
            string path;
            if (!GetCustomBakePath(probe.name, probe.customTexture, true, usePreviousAssetPath, out path))
                return;

            PlanarReflectionProbe collidingProbe;
            if (IsCollidingWithOtherProbes(path, probe, out collidingProbe))
            {
                if (!EditorUtility.DisplayDialog("Texture is used by other reflection probe",
                    string.Format("'{0}' path is used by the game object '{1}', do you really want to overwrite it?",
                        path, collidingProbe.name), "Yes", "No"))
                {
                    return;
                }
            }

            EditorUtility.DisplayProgressBar("Planar Reflection Probes", "Baking " + path, 0.5f);
            if (!BakePlanarReflectionProbe(probe, path))
                Debug.LogError("Failed to bake reflection probe to " + path);
            EditorUtility.ClearProgressBar();

            AssetDatabase.ImportAsset(path);
            probe.customTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            EditorUtility.SetDirty(probe);
        }

        public static void BakeAllPlanarReflectionProbes()
        {
            var probes = Object.FindObjectsOfType<PlanarReflectionProbe>();
            for (var i = 0; i < probes.Length; i++)
            {
                EditorUtility.DisplayProgressBar(
                    "Baking Planar Probes",
                    string.Format("Probe {0} / {1}", i + 1, probes.Length),
                    (i + 1) / (float)probes.Length);

                var probe = probes[i];
                var bakePath = GetBakePathFor(probe);
                var bakePathInfo = new FileInfo(bakePath);
                if (!bakePathInfo.Directory.Exists)
                    bakePathInfo.Directory.Create();
                BakePlanarReflectionProbe(probe, bakePath);
            }
        }

        static string GetBakePathFor(PlanarReflectionProbe probe)
        {
            var scene = probe.gameObject.scene;
            var directory = Path.Combine(Path.GetDirectoryName(scene.path), Path.GetFileNameWithoutExtension(scene.path));
            var filename = string.Format("PlanarReflectionProbe-{0}.exr", 0);
            
            return Path.Combine(directory, filename);
        }

        public static bool BakePlanarReflectionProbe(PlanarReflectionProbe probe, string path)
        {
            var rt = s_PlanarReflectionProbeBaker.NewRenderTarget(probe, ReflectionSystem.parameters.planarReflectionProbeSize);
            s_PlanarReflectionProbeBaker.Render(probe, rt);

            var target = new Texture2D(rt.width, rt.height, TextureFormat.RGBAHalf, false, true);
            var a = RenderTexture.active;
            RenderTexture.active = rt;
            target.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
            RenderTexture.active = a;
            rt.Release();

            probe.bakedTexture = target;

            if (!WriteAndImportTexture(path, target))
                return false;

            return true;
        }

        public static void BakeCustomReflectionProbe(ReflectionProbe probe, bool usePreviousAssetPath)
        {
            string path;
            if (!GetCustomBakePath(probe.name, probe.customBakedTexture, probe.hdr, usePreviousAssetPath, out path))
                return;

            ReflectionProbe collidingProbe;
            if (IsCollidingWithOtherProbes(path, probe, out collidingProbe))
            {
                if (!EditorUtility.DisplayDialog("Cubemap is used by other reflection probe",
                    string.Format("'{0}' path is used by the game object '{1}', do you really want to overwrite it?",
                        path, collidingProbe.name), "Yes", "No"))
                {
                    return;
                }
            }

            EditorUtility.DisplayProgressBar("Reflection Probes", "Baking " + path, 0.5f);
            if (!UnityEditor.Lightmapping.BakeReflectionProbe(probe, path))
                Debug.LogError("Failed to bake reflection probe to " + path);
            EditorUtility.ClearProgressBar();
        }

        public static void ResetProbeSceneTextureInMaterial(ReflectionProbe p)
        {
            var renderer = p.GetComponent<Renderer>();
            renderer.sharedMaterial.SetTexture(_Cubemap, p.texture);
        }

        public static void ResetProbeSceneTextureInMaterial(PlanarReflectionProbe p)
        {
        }

        static MethodInfo k_Lightmapping_BakeReflectionProbeSnapshot = typeof(UnityEditor.Lightmapping).GetMethod("BakeReflectionProbeSnapshot", BindingFlags.Static | BindingFlags.NonPublic);
        public static bool BakeReflectionProbeSnapshot(ReflectionProbe probe)
        {
            return (bool)k_Lightmapping_BakeReflectionProbeSnapshot.Invoke(null, new object[] { probe });
        }

        public static bool BakeReflectionProbeSnapshot(PlanarReflectionProbe probe)
        {
            var rt = s_PlanarReflectionProbeBaker.NewRenderTarget(probe, ReflectionSystem.parameters.planarReflectionProbeSize);
            var bakedTexture = probe.bakedTexture as Texture2D;
            var assetPath = string.Empty;
            if (bakedTexture != null)
                assetPath = AssetDatabase.GetAssetPath(bakedTexture);
            if (string.IsNullOrEmpty(assetPath))
                assetPath = GetBakePath(probe);

            if (bakedTexture == null || string.IsNullOrEmpty(assetPath))
            {
                bakedTexture = new Texture2D(rt.width, rt.height, TextureFormat.RGBAHalf, true, false);
                probe.bakedTexture = bakedTexture;

                EditorUtility.SetDirty(probe);
            }

            s_PlanarReflectionProbeBaker.Render(probe, rt);

            var art = RenderTexture.active;
            RenderTexture.active = rt;
            bakedTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
            RenderTexture.active = art;

            WriteAndImportTexture(assetPath, bakedTexture);

            return true;
        }

        static MethodInfo k_Lightmapping_BakeAllReflectionProbesSnapshots = typeof(UnityEditor.Lightmapping).GetMethod("BakeAllReflectionProbesSnapshots", BindingFlags.Static | BindingFlags.NonPublic);
        public static bool BakeAllReflectionProbesSnapshots()
        {
            return (bool)k_Lightmapping_BakeAllReflectionProbesSnapshots.Invoke(null, new object[0]);
        }

        static bool GetCustomBakePath(string probeName, Texture customBakedTexture, bool hdr, bool usePreviousAssetPath, out string path)
        {
            path = "";
            if (usePreviousAssetPath)
                path = AssetDatabase.GetAssetPath(customBakedTexture);

            var targetExtension = hdr ? "exr" : "png";
            if (string.IsNullOrEmpty(path) || Path.GetExtension(path) != "." + targetExtension)
            {
                // We use the path of the active scene as the target path
                var targetPath = GetSceneBakeDirectoryPath(SceneManager.GetActiveScene());
                if (Directory.Exists(targetPath) == false)
                    Directory.CreateDirectory(targetPath);

                var fileName = probeName + (hdr ? "-reflectionHDR" : "-reflection") + "." + targetExtension;
                fileName = Path.GetFileNameWithoutExtension(AssetDatabase.GenerateUniqueAssetPath(Path.Combine(targetPath, fileName)));

                path = EditorUtility.SaveFilePanelInProject("Save reflection probe's cubemap.", fileName, targetExtension, "", targetPath);
                if (string.IsNullOrEmpty(path))
                    return false;
            }
            return true;
        }

        static string GetBakePath(Component probe)
        {
            var id = GetComponentPersistentID(probe);
            if (id == -1)
                return string.Empty;

            var scene = probe.gameObject.scene;

            var targetPath = GetSceneBakeDirectoryPath(scene);
            if (!Directory.Exists(targetPath))
                Directory.CreateDirectory(targetPath);

            var fileName = probe.name + "-reflectionHDR.exr";
            return AssetDatabase.GenerateUniqueAssetPath(Path.Combine(targetPath, fileName));
        }

        static string GetSceneBakeDirectoryPath(Scene scene)
        {
            var targetPath = scene.path;
            targetPath = Path.Combine(Path.GetDirectoryName(targetPath), Path.GetFileNameWithoutExtension(targetPath));
            if (string.IsNullOrEmpty(targetPath))
                targetPath = "Assets";
            else if (Directory.Exists(targetPath) == false)
                Directory.CreateDirectory(targetPath);
            return targetPath;
        }

        static int GetComponentPersistentID(Component probe)
        {
            var scene = probe.gameObject.scene;
            if (!scene.IsValid())
                return -1;

            var reflectionDictionary = GetReflectionDictionaryFor(scene);
            return reflectionDictionary.GetIdFor(probe);
        }

        static ReflectionSystemSceneDictionary GetReflectionDictionaryFor(Scene scene)
        {
            ReflectionSystemSceneDictionary result = null;

            var roots = new List<GameObject>();
            scene.GetRootGameObjects(roots);
            for (var i = 0; i < roots.Count; i++)
            {
                result = roots[i].GetComponent<ReflectionSystemSceneDictionary>();
                if (result != null)
                    break;
            }

            if (result == null)
            {
                result = EditorUtility.CreateGameObjectWithHideFlags(
                    "Reflection System Dictionary",
                    k_ReflectionSystemDictionaryHideFlags,
                    typeof(ReflectionSystemSceneDictionary))
                    .GetComponent<ReflectionSystemSceneDictionary>();

                SceneManager.MoveGameObjectToScene(result.gameObject, scene);
                result.gameObject.SetActive(false);
            }

            result.gameObject.hideFlags = k_ReflectionSystemDictionaryHideFlags;

            return result;
        }

        static bool WriteAndImportTexture(string path, Texture2D target)
        {
            var bytes = target.EncodeToEXR();
            try
            {
                var targetFile = new FileInfo(path);
                if (!targetFile.Directory.Exists)
                    targetFile.Directory.Create();
                File.WriteAllBytes(path, bytes);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }

            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.alphaSource = TextureImporterAlphaSource.None;
                importer.sRGBTexture = false;
                importer.mipmapEnabled = false;

                var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
                if (hdrp != null)
                {
                    importer.textureCompression = hdrp.renderPipelineSettings.lightLoopSettings.planarReflectionCacheCompressed
                        ? TextureImporterCompression.Compressed
                        : TextureImporterCompression.Uncompressed;
                }

                importer.SaveAndReimport();
            }
            return true;
        }

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            RenderSettings.enableLegacyReflectionProbeSystem = false;
            Lightmapping.BakeReflectionProbeRequest += LightmappingOnBakeReflectionProbeRequest;
            Lightmapping.BakeReflectionProbeRequestCancelled += LightmappingOnBakeReflectionProbeRequestCancelled;
        }

        static void LightmappingOnBakeReflectionProbeRequest(Hash128 dependencyHash)
        {
            // Custom probe hashes should be handled here by the user
            // var customProbeHash = CalculateCustomProbeHashes()
            // dependencyHash = CombineHashes(dependencyHash, customProbeHash);

            // Currently only one bounce is handled
            // TODO: Use  UnityEngine.RenderSettings.reflectionBounces and handle bounces

            ReflectionSystem.QueryReflectionProbes(
                s_TmpReflectionProbeList,
                mode: ReflectionProbeMode.Baked);
            var reflectionProbeTargets = new RenderTexture[s_TmpReflectionProbeList.Count];
            for (var i = 0; i < reflectionProbeTargets.Length; ++i)
                reflectionProbeTargets[i] = s_ReflectionProbeBaker.NewRenderTarget(
                    s_TmpReflectionProbeList[i], 
                    ReflectionSystem.parameters.reflectionProbeSize
                );
            for (var i = 0; i < s_TmpReflectionProbeList.Count; ++i)
                s_ReflectionProbeBaker.Render(s_TmpReflectionProbeList[i], reflectionProbeTargets[i]);


            ReflectionSystem.QueryPlanarProbes(
                s_TmpPlanarReflectionProbeList,
                mode: ReflectionProbeMode.Baked);
            var planarProbeTargets = new RenderTexture[s_TmpPlanarReflectionProbeList.Count];
            for (var i = 0; i < planarProbeTargets.Length; ++i)
                planarProbeTargets[i] = s_PlanarReflectionProbeBaker.NewRenderTarget(
                    s_TmpPlanarReflectionProbeList[i], 
                    ReflectionSystem.parameters.planarReflectionProbeSize
                );
            for (var i = 0; i < s_TmpPlanarReflectionProbeList.Count; ++i)
                s_PlanarReflectionProbeBaker.Render(s_TmpPlanarReflectionProbeList[i], planarProbeTargets[i]);


            AssetDatabase.StartAssetEditing();
            for (var i = 0; i < reflectionProbeTargets.Length; i++)
            {
                var probe = s_TmpReflectionProbeList[i];
                var bakedTexture = probe.bakedTexture;
                var target = reflectionProbeTargets[i];

                var assetPath = string.Empty;
                if (bakedTexture != null)
                    assetPath = AssetDatabase.GetAssetPath(bakedTexture);
                if (string.IsNullOrEmpty(assetPath))
                    assetPath = GetBakePath(probe);

                var createAsset = false;

                if (bakedTexture == null || string.IsNullOrEmpty(assetPath))
                {
                    bakedTexture = new Cubemap(target.width, GraphicsFormat.R16G16B16A16_SFloat, TextureCreationFlags.None);
                    probe.bakedTexture = bakedTexture;
                    createAsset = true;

                    EditorUtility.SetDirty(probe);
                }

                for (var j = 0 ; j < 6; ++j)
                    Graphics.CopyTexture(target, i, bakedTexture, i);

                if (createAsset)
                    AssetDatabase.CreateAsset(bakedTexture, assetPath);
                else
                    EditorUtility.SetDirty(bakedTexture);
            }

            for (var i = 0; i < planarProbeTargets.Length; i++)
            {
                var probe = s_TmpPlanarReflectionProbeList[i];
                var bakedTexture = probe.bakedTexture;
                var target = planarProbeTargets[i];

                var assetPath = string.Empty;
                if (bakedTexture != null)
                    assetPath = AssetDatabase.GetAssetPath(bakedTexture);
                if (string.IsNullOrEmpty(assetPath))
                    assetPath = GetBakePath(probe);

                var createAsset = false;

                if (bakedTexture == null || string.IsNullOrEmpty(assetPath))
                {
                    bakedTexture = new Texture2D(target.width, target.height, GraphicsFormat.R16G16B16A16_SFloat, TextureCreationFlags.None);
                    probe.bakedTexture = bakedTexture;
                    createAsset = true;

                    EditorUtility.SetDirty(probe);
                }

                Graphics.CopyTexture(target, 0, bakedTexture, 0);

                if (createAsset)
                    AssetDatabase.CreateAsset(bakedTexture, assetPath);
                else
                    EditorUtility.SetDirty(bakedTexture);
            }
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();


            AssetDatabase.StartAssetEditing();
            for (var i = 0; i < reflectionProbeTargets.Length; i++)
            {
                var probe = s_TmpReflectionProbeList[i];
                var bakedTexture = probe.bakedTexture;
                var path = AssetDatabase.GetAssetPath(bakedTexture);

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.alphaSource = TextureImporterAlphaSource.None;
                    importer.sRGBTexture = false;
                    importer.mipmapEnabled = false;

                    var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
                    if (hdrp != null)
                    {
                        importer.textureCompression = hdrp.renderPipelineSettings.lightLoopSettings.reflectionCacheCompressed
                            ? TextureImporterCompression.Compressed
                            : TextureImporterCompression.Uncompressed;
                    }

                    importer.SaveAndReimport();
                }
            }

            for (var i = 0; i < planarProbeTargets.Length; i++)
            {
                var probe = s_TmpPlanarReflectionProbeList[i];
                var bakedTexture = probe.bakedTexture;
                var path = AssetDatabase.GetAssetPath(bakedTexture);

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.alphaSource = TextureImporterAlphaSource.None;
                    importer.sRGBTexture = false;
                    importer.mipmapEnabled = false;

                    var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
                    if (hdrp != null)
                    {
                        importer.textureCompression = hdrp.renderPipelineSettings.lightLoopSettings.planarReflectionCacheCompressed
                            ? TextureImporterCompression.Compressed
                            : TextureImporterCompression.Uncompressed;
                    }

                    importer.SaveAndReimport();
                }
            }
            AssetDatabase.StopAssetEditing();
        }

        static void LightmappingOnBakeReflectionProbeRequestCancelled(Hash128 dependencyHash)
        {
            Debug.Log("Cancel: " + dependencyHash);
        }
    }
}
