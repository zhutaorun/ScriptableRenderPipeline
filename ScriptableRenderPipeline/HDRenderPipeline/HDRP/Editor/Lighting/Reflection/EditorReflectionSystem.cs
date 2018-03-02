using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor.Callbacks;
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

        static ReflectionBakeJob s_CurrentBakeJob = null;

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
            EditorApplication.update += Update;
        }

        static void Update()
        {
            if (s_CurrentBakeJob != null)
            {
                s_CurrentBakeJob.Tick();
                if (s_CurrentBakeJob.isComplete)
                {
                    s_CurrentBakeJob.Dispose();
                    s_CurrentBakeJob = null;
                }
            }
        }

        static void LightmappingOnBakeReflectionProbeRequest(BakeReflectionProbeRequest request)
        {
            if (request.completed)
                return;

            // Custom probe hashes should be handled here by the user
            // var customProbeHash = CalculateCustomProbeHashes()
            // dependencyHash = CombineHashes(dependencyHash, customProbeHash);

            // Currently only one bounce is handled
            // TODO: Use  UnityEngine.RenderSettings.reflectionBounces and handle bounces

            if (s_CurrentBakeJob != null)
            {
                s_CurrentBakeJob.Dispose();
                s_CurrentBakeJob = null;
            }
            Debug.Log(request.requestHash.ToString()); 

            var job = new ReflectionBakeJob(request);
            ReflectionSystem.QueryReflectionProbes(job.reflectionProbesToBake, mode: ReflectionProbeMode.Baked);
            ReflectionSystem.QueryPlanarProbes(job.planarReflectionProbesToBake, mode: ReflectionProbeMode.Baked);
            s_CurrentBakeJob = job;

            request.Cancelled += LightmappingOnBakeReflectionProbeRequestCancelled;
        }

        static void LightmappingOnBakeReflectionProbeRequestCancelled(BakeReflectionProbeRequest request)
        {
            Debug.Log("Cancel: " + request.requestHash);
            request.Cancelled -= LightmappingOnBakeReflectionProbeRequestCancelled;
            if (s_CurrentBakeJob != null && s_CurrentBakeJob.request == request)
            {
                s_CurrentBakeJob.Dispose();
                s_CurrentBakeJob = null;
            }
        }

        class ReflectionBakeJob : IDisposable
        {
            enum Stage
            {
                BakeReflectionProbe,
                BakePlanarProbe,
                Completed
            }

            delegate void BakingStage(ReflectionBakeJob job);

            static readonly BakingStage[] s_Stages =
            {
                StageBakeReflectionProbe,
                StageBakePlanarProbe,
            };

            Stage m_CurrentStage = Stage.BakeReflectionProbe;
            int m_StageIndex;

            public bool isComplete { get { return m_CurrentStage == Stage.Completed; } }
            public BakeReflectionProbeRequest request;
            public List<ReflectionProbe> reflectionProbesToBake = new List<ReflectionProbe>();
            public List<PlanarReflectionProbe> planarReflectionProbesToBake = new List<PlanarReflectionProbe>();

            public ReflectionBakeJob(BakeReflectionProbeRequest request)
            {
                this.request = request;
            }

            public void Tick()
            {
                if (m_StageIndex == -1 && m_CurrentStage != Stage.Completed)
                {
                    m_CurrentStage = (Stage)((int)m_CurrentStage + 1);
                    m_StageIndex = 0;
                }

                if (m_CurrentStage == Stage.Completed)
                {
                    request.progress = 1; 
                    return;
                }

                s_Stages[(int)m_CurrentStage](this);
            }

            public void Dispose()
            {
                request.progress = 1;
                m_CurrentStage = Stage.Completed;
                m_StageIndex = 0;
            }

            static void StageBakeReflectionProbe(ReflectionBakeJob job)
            {
                if (job.m_StageIndex >= job.reflectionProbesToBake.Count)
                {
                    job.m_StageIndex = -1;
                    return;
                }

                var stageProgress = job.reflectionProbesToBake.Count > 0
                    ? 1f - job.m_StageIndex / (float)job.reflectionProbesToBake.Count
                    : 1f;

                job.request.progress = ((float)Stage.BakeReflectionProbe + stageProgress) / (float)Stage.Completed;
                job.request.progressMessage = string.Format("Reflection Probes ({0}/{1})", job.m_StageIndex + 1, job.reflectionProbesToBake.Count);

                var probe = job.reflectionProbesToBake[job.m_StageIndex];

                var target = s_ReflectionProbeBaker.NewRenderTarget(
                    probe, 
                    ReflectionSystem.parameters.reflectionProbeSize
                );
                s_ReflectionProbeBaker.Render(probe, target);

                var bakedTexture = probe.bakedTexture;

                var assetPath = string.Empty;
                if (bakedTexture != null)
                    assetPath = AssetDatabase.GetAssetPath(bakedTexture);
                if (string.IsNullOrEmpty(assetPath))
                    assetPath = GetBakePath(probe);

                if (bakedTexture == null || string.IsNullOrEmpty(assetPath))
                {
                    bakedTexture = new Cubemap(target.width, GraphicsFormat.R16G16B16A16_SFloat, TextureCreationFlags.None);
                    probe.bakedTexture = bakedTexture;

                    EditorUtility.SetDirty(probe);
                }

                for (var j = 0; j < 6; ++j)
                    Graphics.CopyTexture(target, j, 0, bakedTexture, j, 0);

                target.Release();

                {
                    var tmp2D = new Texture2D(bakedTexture.width * 6, bakedTexture.height, GraphicsFormat.R16G16B16A16_SFloat, TextureCreationFlags.None);
                    var cols = new Color[bakedTexture.width * 6 * bakedTexture.height];
                    var length = bakedTexture.width * bakedTexture.height;

                    var tmpCols = ((Cubemap)bakedTexture).GetPixels(CubemapFace.PositiveX, 0);
                    Array.Copy(tmpCols, 0, cols, 0, length);
                    tmpCols = ((Cubemap)bakedTexture).GetPixels(CubemapFace.NegativeX, 0);
                    Array.Copy(tmpCols, 0, cols, length, length);
                    tmpCols = ((Cubemap)bakedTexture).GetPixels(CubemapFace.PositiveY, 0);
                    Array.Copy(tmpCols, 0, cols, length * 2, length);
                    tmpCols = ((Cubemap)bakedTexture).GetPixels(CubemapFace.NegativeY, 0);
                    Array.Copy(tmpCols, 0, cols, length * 3, length);
                    tmpCols = ((Cubemap)bakedTexture).GetPixels(CubemapFace.PositiveZ, 0);
                    Array.Copy(tmpCols, 0, cols, length * 4, length);
                    tmpCols = ((Cubemap)bakedTexture).GetPixels(CubemapFace.NegativeZ, 0);
                    Array.Copy(tmpCols, 0, cols, length * 5, length);

                    tmp2D.SetPixels(cols);
                    tmp2D.Apply(false);
                    var bytes = tmp2D.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
                    File.WriteAllBytes(assetPath, bytes);
                }

                AssetDatabase.ImportAsset(assetPath);

                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer != null)
                {
                    importer.alphaSource = TextureImporterAlphaSource.None;
                    importer.sRGBTexture = false;
                    importer.mipmapEnabled = false;
                    importer.textureShape = TextureImporterShape.TextureCube;

                    var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
                    if (hdrp != null)
                    {
                        importer.textureCompression = hdrp.renderPipelineSettings.lightLoopSettings.reflectionCacheCompressed
                            ? TextureImporterCompression.Compressed
                            : TextureImporterCompression.Uncompressed;
                    }

                    importer.SaveAndReimport();
                }
                ++job.m_StageIndex;
            }

            static void StageBakePlanarProbe(ReflectionBakeJob job)
            {
                if (job.m_StageIndex >= job.planarReflectionProbesToBake.Count)
                {
                    job.m_StageIndex = -1;
                    return;
                }

                var stageProgress = job.planarReflectionProbesToBake.Count > 0
                    ? 1f - job.m_StageIndex / (float)job.planarReflectionProbesToBake.Count
                    : 1f;

                job.request.progress = ((float)Stage.BakePlanarProbe + stageProgress) / (float)Stage.Completed;
                job.request.progressMessage = string.Format("Reflection Probes ({0}/{1})", job.m_StageIndex + 1, job.planarReflectionProbesToBake.Count);

                var probe = job.planarReflectionProbesToBake[job.m_StageIndex];
                var target = s_PlanarReflectionProbeBaker.NewRenderTarget(
                    probe,
                    ReflectionSystem.parameters.planarReflectionProbeSize
                );
                s_PlanarReflectionProbeBaker.Render(probe, target);

                var bakedTexture = probe.bakedTexture;

                var assetPath = string.Empty;
                if (bakedTexture != null)
                    assetPath = AssetDatabase.GetAssetPath(bakedTexture);
                if (string.IsNullOrEmpty(assetPath))
                    assetPath = GetBakePath(probe);

                if (bakedTexture == null || string.IsNullOrEmpty(assetPath))
                {
                    bakedTexture = new Texture2D(target.width, target.height, GraphicsFormat.R16G16B16A16_SFloat, TextureCreationFlags.None);
                    probe.bakedTexture = bakedTexture;

                    EditorUtility.SetDirty(probe);

                    var bytes = ((Texture2D)bakedTexture).EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
                    File.WriteAllBytes(assetPath, bytes);
                }

                Graphics.CopyTexture(target, 0, bakedTexture, 0);

                target.Release();

                AssetDatabase.ImportAsset(assetPath);

                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer != null)
                {
                    importer.alphaSource = TextureImporterAlphaSource.None;
                    importer.sRGBTexture = false;
                    importer.mipmapEnabled = false;
                    importer.textureShape = TextureImporterShape.Texture2D;

                    var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
                    if (hdrp != null)
                    {
                        importer.textureCompression = hdrp.renderPipelineSettings.lightLoopSettings.planarReflectionCacheCompressed
                            ? TextureImporterCompression.Compressed
                            : TextureImporterCompression.Uncompressed;
                    }

                    importer.SaveAndReimport();
                }
                ++job.m_StageIndex;
            }
        }
    }
}
