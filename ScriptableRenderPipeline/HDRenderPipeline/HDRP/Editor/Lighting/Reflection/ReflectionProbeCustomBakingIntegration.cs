using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    static class ReflectionProbeCustomBakingIntegration
    {
        static ReflectionBakeJob s_CurrentBakeJob = null;

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            GraphicsSettings.useBuiltinReflectionProbeSystem = false;
            Lightmapping.BakeReflectionProbeRequest += LightmappingOnBakeReflectionProbeRequest;
            Lightmapping.ClearBakedReflectionProbeRequest += LightmappingOnClearBakedReflectionProbeRequest;
            EditorSceneManager.sceneOpened += EditorSceneManagerOnSceneOpened;
            EditorApplication.update += Update;
        }

        static void EditorSceneManagerOnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            var asset = EditorHDLightingDataAsset.GetLightingDataAssetForScene(scene);
            if (asset == null)
                return;

            var roots = scene.GetRootGameObjects();
            var probes = new List<ReflectionProbe>();
            for (var i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                root.GetComponentsInChildren(probes);
                for (var j = 0; j < probes.Count; j++)
                {
                    var probe = probes[j];
                    var id = SceneObjectIdentifierUtils.GetSceneObjectIdentifierFor(probe);
                    if (id == SceneObjectIdentifier.Invalid)
                        continue;
                    var bakedTexture = asset.GetBakedTextureFor(id);
                    if (bakedTexture == null)
                        continue;
                    probe.bakedTexture = bakedTexture;
                }
            }
        }

        static void LightmappingOnClearBakedReflectionProbeRequest()
        {
            for (int i = 0, c = SceneManager.sceneCount; i < c; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded)
                    continue;

                var asset = EditorHDLightingDataAsset.GetLightingDataAssetForScene(scene);
                if (asset == null)
                    continue;

                asset.DeleteAssets();

                var path = AssetDatabase.GetAssetPath(asset);
                Assert.IsFalse(string.IsNullOrEmpty(path));

                AssetDatabase.DeleteAsset(path);
            }
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
            if (request.IsCompleted)
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

            var job = new ReflectionBakeJob(request);
            ReflectionSystem.QueryReflectionProbes(job.reflectionProbesToBake, mode: ReflectionProbeMode.Baked);
            ReflectionSystem.QueryPlanarProbes(job.planarReflectionProbesToBake, mode: ReflectionProbeMode.Baked);
            s_CurrentBakeJob = job;

            request.Cancelled += LightmappingOnBakeReflectionProbeRequestCancelled;
        }

        static void LightmappingOnBakeReflectionProbeRequestCancelled(BakeReflectionProbeRequest request)
        {
            Debug.Log("Cancel: " + request.RequestHash);
            request.Cancelled -= LightmappingOnBakeReflectionProbeRequestCancelled;
            if (s_CurrentBakeJob != null && s_CurrentBakeJob.request == request)
            {
                s_CurrentBakeJob.Dispose();
                s_CurrentBakeJob = null;
            }
        }
    }
}
