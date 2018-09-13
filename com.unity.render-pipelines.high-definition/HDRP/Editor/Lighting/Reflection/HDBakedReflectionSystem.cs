using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental.Rendering;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    unsafe class HDBakedReflectionSystem : ScriptableBakedReflectionSystem
    {
        struct HDProbeBakingState
        {
            public struct ProbeBakingHash : CoreUnsafeUtils.IKeyGetter<HDProbeBakingState, Hash128>
            { public Hash128 Get(ref HDProbeBakingState v) { return v.probeBakingHash; } }

            public int instanceID;
            public Hash128 probeSettingsHash;
            public Hash128 probeBakingHash;
        }

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            ScriptableBakedReflectionSystemSettings.system = new HDBakedReflectionSystem();
        }

        enum BakingStages
        {
            ReflectionProbes
        }

        HDBakedReflectionSystem() : base(1)
        {
        }

        public override void Tick(
            SceneStateHash sceneStateHash,
            IScriptableBakedReflectionSystemStageNotifier handle
        )
        {
            var hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (hdPipeline == null)
            {
                Debug.LogWarning("HDBakedReflectionSystem work with HDRP, " +
                    "please switch your render pipeline or use another reflection system");
                handle.ExitStage((int)BakingStages.ReflectionProbes);
                return;
            }

            var ambientProbeHash = sceneStateHash.ambientProbeHash;
            var sceneObjectsHash = sceneStateHash.sceneObjectsHash;
            var skySettingsHash = sceneStateHash.skySettingsHash;

            // Explanation of the algorithm:
            // 1. First we create the hash of the world that can impact the reflection probes.
            // 2. Then for each probe, we calculate a hash that represent what this specific probe should have baked.
            // 3. We compare those hashes against the baked one and decide:
            //   a. If we have to remove a baked data
            //   b. If we have to bake a probe
            // 4. Bake all required probes
            // 5. Remove unused baked data
            // 6. Update probe assets

            var bakedProbes = HDProbeSystem.bakedProbes;

            var states = stackalloc HDProbeBakingState[bakedProbes.Count];
            ComputeProbeInstanceID(bakedProbes, states);
            ComputeProbeSettingsHashes(bakedProbes, states);
            // TODO: Handle bounce dependency here

            var allProbeDependencyHash = new Hash128();
            // TODO: All baked probes depend on custom probes (hash all custom probes and set as dependency)
            // TODO: All baked probes depend on HDRP specific Light settings
            HashUtilities.AppendHash(ref ambientProbeHash, ref allProbeDependencyHash);
            HashUtilities.AppendHash(ref sceneObjectsHash, ref allProbeDependencyHash);
            HashUtilities.AppendHash(ref skySettingsHash, ref allProbeDependencyHash);
            ComputeProbeBakingHashes(bakedProbes.Count, allProbeDependencyHash, states);

            CoreUnsafeUtils.QuickSort<HDProbeBakingState, Hash128, HDProbeBakingState.ProbeBakingHash>(
                bakedProbes.Count, states
            );

            // TODO: Compare hashes
            // TODO: baked added probes
            // TODO: delete data for removed probes
            // TODO: Batch import/delete baked assets

            handle.EnterStage((int)BakingStages.ReflectionProbes, "Baking Reflection Probes", 0);
            var cubemapSize = (int)hdPipeline.renderPipelineSettings.lightLoopSettings.reflectionCubemapSize;
            for (int i = 0; i < bakedProbes.Count; ++i)
            {
                var probe = bakedProbes[i];
                var settings = probe.settings;
                switch (settings.type)
                {
                    case ProbeSettings.ProbeType.ReflectionProbe:
                        {
                            var rt = new RenderTexture(cubemapSize, cubemapSize, 1)
                            {
                                dimension = TextureDimension.Cube,
                                useMipMap = false,
                                autoGenerateMips = false,
                                format = RenderTextureFormat.ARGBHalf,
                                name = "Temporary Reflection Probe Target"
                            };
                            var positionSettings = ProbeCapturePositionSettings.ComputeFrom(probe, null);
                            HDRenderUtilities.Render(probe.settings, positionSettings, rt);
                            var bakedTexture = CreateBakedTextureFromRenderTexture(rt, probe);
                            var reflectionProbe = probe.GetComponent<ReflectionProbe>();
                            reflectionProbe.bakedTexture = bakedTexture;
                            break;
                        }
                }
            }
            handle.ExitStage((int)BakingStages.ReflectionProbes);

            handle.SetIsDone(true);
        }

        static void ComputeProbeInstanceID(IList<HDProbe> probes, HDProbeBakingState* states)
        {
            for (int i = 0; i < probes.Count; ++i)
                states[i].instanceID = probes[i].GetInstanceID();
        }

        static void ComputeProbeSettingsHashes(IList<HDProbe> probes, HDProbeBakingState* states)
        {
            for (int i = 0; i < probes.Count; ++i)
            {
                var probe = probes[i];
                var settings = probe.settings;
                HashUtilities.ComputeHash128(ref settings, ref states[i].probeSettingsHash);
            }
        }

        static void ComputeProbeBakingHashes(int count, Hash128 allProbeDependencyHash, HDProbeBakingState* states)
        {
            for (int i = 0; i < count; ++i)
            {
                states[i].probeBakingHash = states[i].probeSettingsHash;
                HashUtilities.ComputeHash128(ref allProbeDependencyHash, ref states[i].probeBakingHash);
            }
        }

        static Texture CreateBakedTextureFromRenderTexture(RenderTexture rt, HDProbe probe)
        {
            Assert.IsNotNull(rt);
            Assert.IsNotNull(probe);

            var targetFile = HDBakingUtilities.GetBakedTextureFilePath(probe);
            HDBakingUtilities.CreateParentDirectoryIfMissing(targetFile);
            HDTextureUtilities.WriteTextureFileToDisk(rt, targetFile);

            AssetDatabase.ImportAsset(targetFile);

            var importer = (TextureImporter)AssetImporter.GetAtPath(targetFile);
            importer.filterMode = FilterMode.Bilinear;
            importer.generateCubemap = TextureImporterGenerateCubemap.AutoCubemap;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.textureShape = TextureImporterShape.TextureCube;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture>(targetFile);
        }
    }
}
