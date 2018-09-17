using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEditor.Experimental.Rendering;
using UnityEditor.SceneManagement;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

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

        struct HDProbeBakedState
        {
            public struct ProbeBakedHash : CoreUnsafeUtils.IKeyGetter<HDProbeBakedState, Hash128>
            { public Hash128 Get(ref HDProbeBakedState v) { return v.probeBakedHash; } }

            public int instanceID;
            public Hash128 probeBakedHash;
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

        HDProbeBakedState[] m_HDProbeBakedStates = new HDProbeBakedState[0];

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

            // == 1. ==
            var allProbeDependencyHash = new Hash128();
            // TODO: All baked probes depend on custom probes (hash all custom probes and set as dependency)
            // TODO: All baked probes depend on HDRP specific Light settings
            HashUtilities.AppendHash(ref ambientProbeHash, ref allProbeDependencyHash);
            HashUtilities.AppendHash(ref sceneObjectsHash, ref allProbeDependencyHash);
            HashUtilities.AppendHash(ref skySettingsHash, ref allProbeDependencyHash);

            var bakedProbes = HDProbeSystem.bakedProbes;

            // == 2. ==
            var states = stackalloc HDProbeBakingState[bakedProbes.Count];
            ComputeProbeInstanceID(bakedProbes, states);
            ComputeProbeSettingsHashes(bakedProbes, states);
            // TODO: Handle bounce dependency here
            ComputeProbeBakingHashes(bakedProbes.Count, allProbeDependencyHash, states);

            CoreUnsafeUtils.QuickSort<HDProbeBakingState, Hash128, HDProbeBakingState.ProbeBakingHash>(
                bakedProbes.Count, states
            );

            int operationCount = 0, addCount = 0, remCount = 0;
            var maxProbeCount = Mathf.Max(bakedProbes.Count, m_HDProbeBakedStates.Length);
            var addIndices = stackalloc int[maxProbeCount];
            var remIndices = stackalloc int[maxProbeCount];

            if (m_HDProbeBakedStates.Length == 0)
            {
                for (int i = 0; i < bakedProbes.Count; ++i)
                    addIndices[addCount++] = i;
                operationCount = addCount;
            }
            else
            {
                fixed (HDProbeBakedState* oldBakedStates = &m_HDProbeBakedStates[0])
                {
                    // == 3. ==
                    // Compare hashes between baked probe states and desired probe states
                    operationCount = CoreUnsafeUtils.CompareHashes<
                            HDProbeBakedState, HDProbeBakedState.ProbeBakedHash,
                            HDProbeBakingState, HDProbeBakingState.ProbeBakingHash
                       > (
                       m_HDProbeBakedStates.Length, oldBakedStates, // old hashes
                       bakedProbes.Count, states,                   // new hashes
                       addIndices, remIndices,
                       out addCount, out remCount
                    );
                }
            }
            

            if (operationCount > 0)
            {
                // == 4. ==
                var cubemapSize = (int)hdPipeline.renderPipelineSettings.lightLoopSettings.reflectionCubemapSize;
                var cubeRT = new RenderTexture(cubemapSize, cubemapSize, 1)
                {
                    dimension = TextureDimension.Cube,
                    useMipMap = false,
                    autoGenerateMips = false,
                    format = RenderTextureFormat.ARGBHalf,
                    name = "Temporary Reflection Probe Target"
                };

                handle.EnterStage(
                    (int)BakingStages.ReflectionProbes,
                    string.Format("Reflection Probes | {0} jobs", addCount),
                    0
                );

                for (int i = 0; i < addCount; ++i)
                {
                    handle.EnterStage(
                        (int)BakingStages.ReflectionProbes,
                        string.Format("Reflection Probes | {0} jobs", addCount),
                        i / (float)addCount
                    );

                    var index = addIndices[i];
                    var instanceId = states[index].instanceID;
                    var probe = (HDProbe)EditorUtility.InstanceIDToObject(instanceId);
                    var settings = probe.settings;
                    switch (settings.type)
                    {
                        case ProbeSettings.ProbeType.ReflectionProbe:
                            {
                                var positionSettings = ProbeCapturePositionSettings.ComputeFrom(probe, null);
                                HDRenderUtilities.Render(probe.settings, positionSettings, cubeRT);
                                var bakedTexture = CreateBakedTextureFromRenderTexture(
                                    cubeRT,
                                    probe,
                                    states[index].probeBakingHash
                                );
                                var reflectionProbe = probe.GetComponent<ReflectionProbe>();
                                reflectionProbe.bakedTexture = bakedTexture;
                                break;
                            }
                        case ProbeSettings.ProbeType.PlanarProbe:
                            Debug.LogWarning("Baked Planar Reflections are not supported yet.");
                            break;
                    }
                }
                cubeRT.Release();

                // == 5. ==
                for (int i = 0; i < remCount; ++i)
                {
                    var index = remIndices[i];
                    var hash = m_HDProbeBakedStates[index].probeBakedHash;
                    DeleteBakedTextureWithHash(hash);
                }

                // Create new baked state array
                var targetSize = m_HDProbeBakedStates.Length + addCount - remCount;
                var targetBakedStates = stackalloc HDProbeBakedState[targetSize];
                // Copy baked state that are not removed
                var targetI = 0;
                for (int i = 0; i < m_HDProbeBakedStates.Length; ++i)
                {
                    if (CoreUnsafeUtils.IndexOf(remIndices, remCount, i) != -1)
                        continue;
                    targetBakedStates[targetI++] = m_HDProbeBakedStates[i];
                }
                // Add new baked states
                for (int i = 0; i < addCount; ++i)
                {
                    var state = states[addIndices[i]];
                    targetBakedStates[targetI++] = new HDProbeBakedState
                    {
                        instanceID = state.instanceID,
                        probeBakedHash = state.probeBakingHash
                    };
                }

                Array.Resize(ref m_HDProbeBakedStates, targetSize);
                if (targetSize > 0)
                {
                    fixed (HDProbeBakedState* bakedStates = &m_HDProbeBakedStates[0])
                    {
                        UnsafeUtility.MemCpy(
                            bakedStates,
                            targetBakedStates,
                            sizeof(HDProbeBakedState) * targetSize
                        );
                    }
                }

                // Update state hash
                var allBakedhash = new Hash128();
                for (int i = 0; i < m_HDProbeBakedStates.Length; ++i)
                    HashUtilities.AppendHash(ref m_HDProbeBakedStates[i].probeBakedHash, ref allBakedhash);
                stateHash = allBakedhash;
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
                var positionSettings = ProbeCapturePositionSettings.ComputeFrom(probe, null);
                var positionSettingsHash = new Hash128();
                HashUtilities.ComputeHash128(ref positionSettings, ref positionSettingsHash);
                // TODO: make ProbeSettings and unmanaged type so its hash can be the hash of its memory
                var probeSettingsHash = probe.settings.ComputeHash();
                HashUtilities.AppendHash(ref positionSettingsHash, ref probeSettingsHash);
                states[i].probeSettingsHash = probeSettingsHash;
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

        static Texture CreateBakedTextureFromRenderTexture(RenderTexture rt, HDProbe probe, Hash128 hash)
        {
            Assert.IsNotNull(rt);
            Assert.IsNotNull(probe);

            var targetFile = HDBakingUtilities.GetBakedTextureFilePath(probe, hash);
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

        static void DeleteBakedTextureWithHash(Hash128 hash)
        {
            for (int i = 0, c = SceneManager.sceneCount; i < c; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                var directory = HDBakingUtilities.GetBakedTextureDirectory(scene);
                var files = Directory.GetFiles(directory, string.Format("*{0}*", hash));
                foreach (var file in files)
                    AssetDatabase.DeleteAsset(file);
            }
        }
    }
}
