using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
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
                var cubeRT = new RenderTexture(cubemapSize, cubemapSize, 1, GraphicsFormat.R16G16B16A16_SFloat)
                {
                    dimension = TextureDimension.Cube,
                    enableRandomWrite = true,
                    useMipMap = false,
                    autoGenerateMips = false
                };

                handle.EnterStage(
                    (int)BakingStages.ReflectionProbes,
                    string.Format("Reflection Probes | {0} jobs", addCount),
                    0
                );

                // Render probes
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
                    var cacheFile = GetGICacheFileForHDProbe(states[index].probeBakingHash);

                    // Get from cache or render the probe
                    if (!File.Exists(cacheFile))
                        RenderAndWriteToFile(probe, cacheFile, cubeRT);
                }
                cubeRT.Release();
                // Copy texture from cache
                for (int i = 0; i < addCount; ++i)
                {
                    var index = addIndices[i];
                    var instanceId = states[index].instanceID;
                    var probe = (HDProbe)EditorUtility.InstanceIDToObject(instanceId);
                    var cacheFile = GetGICacheFileForHDProbe(states[index].probeBakingHash);

                    Assert.IsTrue(File.Exists(cacheFile));

                    var bakedTexturePath = HDBakingUtilities.GetBakedTextureFilePath(probe);
                    HDBakingUtilities.CreateParentDirectoryIfMissing(bakedTexturePath);
                    File.Copy(cacheFile, bakedTexturePath, true);
                }
                // Import assets
                AssetDatabase.StartAssetEditing();
                for (int i = 0; i < addCount; ++i)
                {
                    var index = addIndices[i];
                    var instanceId = states[index].instanceID;
                    var probe = (HDProbe)EditorUtility.InstanceIDToObject(instanceId);
                    var cacheFile = GetGICacheFileForHDProbe(states[index].probeBakingHash);

                    var bakedTexturePath = HDBakingUtilities.GetBakedTextureFilePath(probe);

                    // Get or create the baked texture asset for the probe
                    var bakedTexture = AssetDatabase.LoadAssetAtPath<Texture>(bakedTexturePath);
                    ImportAssetAt(probe, bakedTexturePath);
                    AssignBakedTexture(probe, bakedTexture);
                }
                AssetDatabase.StopAssetEditing();

                // == 5. ==

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

        void AssignBakedTexture(HDProbe probe, Texture bakedTexture)
        {
            switch (probe.settings.type)
            {
                case ProbeSettings.ProbeType.ReflectionProbe:
                    {
                        var reflectionProbe = probe.GetComponent<ReflectionProbe>();
                        reflectionProbe.bakedTexture = bakedTexture;
                        break;
                    }
                case ProbeSettings.ProbeType.PlanarProbe:
                    Debug.LogWarning("Baked Planar Reflections are not supported yet.");
                    break;
            }
        }

        void RenderAndWriteToFile(HDProbe probe, string cacheFile, RenderTexture cubeRT)
        {
            var settings = probe.settings;
            switch (settings.type)
            {
                case ProbeSettings.ProbeType.ReflectionProbe:
                    {
                        var positionSettings = ProbeCapturePositionSettings.ComputeFrom(probe, null);
                        HDRenderUtilities.Render(probe.settings, positionSettings, cubeRT);
                        HDBakingUtilities.CreateParentDirectoryIfMissing(cacheFile);
                        HDTextureUtilities.WriteTextureFileToDisk(cubeRT, cacheFile);
                        break;
                    }
                case ProbeSettings.ProbeType.PlanarProbe:
                    Debug.LogWarning("Baked Planar Reflections are not supported yet.");
                    break;
            }
        }

        void ImportAssetAt(HDProbe probe, string file)
        {
            switch (probe.settings.type)
            {
                case ProbeSettings.ProbeType.ReflectionProbe:
                    {
                        var importer = (TextureImporter)AssetImporter.GetAtPath(file);
                        importer.sRGBTexture = false;
                        importer.filterMode = FilterMode.Bilinear;
                        importer.generateCubemap = TextureImporterGenerateCubemap.AutoCubemap;
                        importer.mipmapEnabled = false;
                        importer.textureCompression = TextureImporterCompression.Compressed;
                        importer.textureShape = TextureImporterShape.TextureCube;
                        importer.SaveAndReimport();
                        break;
                    }
                case ProbeSettings.ProbeType.PlanarProbe:
                    Debug.LogWarning("Baked Planar Reflections are not supported yet.");
                    break;
            }
        }

        private string GetGICacheFileForHDProbe(Hash128 hash)
        {
            var hashFolder = GetGICacheFolderFor(hash);
            return Path.Combine(hashFolder, string.Format("HDProbe-{0}.exr", hash));
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

        static Func<string> GetGICachePath = Expression.Lambda<Func<string>>(
            Expression.Call(
                typeof(Lightmapping)
                .GetProperty("diskCachePath", BindingFlags.Static | BindingFlags.NonPublic)
                .GetGetMethod(true)
            )
        ).Compile();

        public static string GetGICacheFolderFor(Hash128 hash)
        {
            var cacheFolder = GetGICachePath();
            var hashFolder = Path.Combine(cacheFolder, hash.ToString().Substring(0, 2));
            return hashFolder;
        }
    }
}
