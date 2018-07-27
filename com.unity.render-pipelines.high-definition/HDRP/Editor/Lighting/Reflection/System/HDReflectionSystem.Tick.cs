using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    /// <summary>
    /// Contains the logic of the reflection probe baking ticking.
    /// </summary>
    unsafe struct HDReflectionSystemTick
    {
        // Algorithm settings
        public ReflectionSettings settings;

        // Injected resources by callee
        public BakedProbeHashes bakedProbeHashes;
        public HDProbeTickedRenderer tickedRenderer;
        public HDReflectionEntitySystem entitySystem;
        public HDProbeTextureImporter textureImporter;

        internal void Tick(SceneStateHash sceneStateHash, IScriptableBakedReflectionSystemStageNotifier handle)
        {
            DestroyUnusedCubemaps();

            // Explanation of the algorithm:
            // 1. First we create the hash of the world that can impact the reflection probes.
            // 2. Then for each probe, we calculate a hash that represent what this specific probe should have baked.
            // 3. We compare those hashes against the baked one and decide:
            //   a. If we have to remove a baked data
            //   b. If we have to bake a probe
            // 4. Then we check with the renderer what to bake:
            //   a. If the baking is in flight, we continue and another tick will acknowledge its completion
            //   b. Otherwise, we cancel the baking a restart a new one
            // 5. When baking is complete:
            //   a. Remove the unused baked data
            //   b. Get baked file from cache, apply proper import settings and link to the probe.

            // = Step 1 =
            // Allocate stack variables
            var bakedProbeCount = entitySystem.BakedProbeCount;
            var bakedProbeOnlyHashes = stackalloc Hash128[bakedProbeCount];
            var bakedProbeIDs = stackalloc int[bakedProbeCount];
            ComputeProbeStateHashesAndGetEntityIDs(
                entitySystem.GetActiveBakedProbeEnumerator(),
                bakedProbeOnlyHashes, bakedProbeIDs
            );

            var allBakedProbeHash = new Hash128();
            // Baked probes depends on other baked probes when baking several bounces
            ComputeAllBakedProbeHashWithBounces(
                &allBakedProbeHash, bakedProbeCount, bakedProbeOnlyHashes, settings.bounces
            );

            // Baked probes depends on probes with custom textures
            var allCustomProbeHash = new Hash128();
            ComputeAllCustomProbeHash(entitySystem.GetActiveCustomProbeEnumerator(), ref allCustomProbeHash);
            HashUtilities.AppendHash(ref allCustomProbeHash, ref allBakedProbeHash);

            // TODO: calculate a custom hash for light that hashes additional data as well.
            // Baked probes depends on scene gameobjects (static geometry, lights)
            var sceneObjectHash = sceneStateHash.sceneObjectsHash;
            HashUtilities.AppendHash(ref sceneObjectHash, ref allBakedProbeHash);
            // Baked probes depends on sky settings
            var skySettingsHash = sceneStateHash.skySettingsHash;
            HashUtilities.AppendHash(ref skySettingsHash, ref allBakedProbeHash);

            // = Step 2 =
            // Compute the hash of the data that should have been baked for each probe.
            var bakedProbeOutputHashes = stackalloc Hash128[bakedProbeCount];
            ComputeBakedProbeOutputHashes(
                &allBakedProbeHash,
                bakedProbeCount,
                bakedProbeOnlyHashes, bakedProbeOutputHashes
            );

            // = Step 3 =
            var maxProbeCount = Mathf.Max(bakedProbeCount, bakedProbeHashes.count);
            var addIndices = stackalloc int[maxProbeCount];
            var remIndices = stackalloc int[maxProbeCount];
            var oldHashes = stackalloc Hash128[bakedProbeHashes.count];
            bakedProbeHashes.probeOutputHashes.CopyTo(oldHashes, bakedProbeHashes.count);
            // Sort the hashes to have a consistent comparison
            CoreUnsafeUtils.QuickSort<Hash128>(bakedProbeCount, bakedProbeOutputHashes);
            int addCount = 0, remCount = 0;
            // The actual comparison happens here
            // addIndicies will hold indices of probes to bake
            // remIndicies will hold indices of baked data to delete
            if (CoreUnsafeUtils.CompareHashes(
                bakedProbeHashes.count, oldHashes,          // old hashes
                bakedProbeCount, bakedProbeOutputHashes,    // new hashes
                addIndices, remIndices,
                out addCount, out remCount
                ) > 0)
            {
                // Notify Unity we are baking probes
                var progress = (bakedProbeCount != 0) ? 1.0f - ((float)addCount / bakedProbeCount) : 1.0f; ;
                handle.EnterStage(
                    (int)HDReflectionSystem.BakeStages.ReflectionProbes,
                    string.Format("Reflection Probes | {0} jobs", addCount),
                    progress
                );

                // = Step 4 =
                // No probe to bake == we already baked all probes properly
                var bakingComplete = addCount == 0;
                // Check if the renderer is currently baking the probes
                if (!bakingComplete)
                {
                    var allProbeOutputHash = new Hash128();
                    CoreUnsafeUtils.CombineHashes(bakedProbeCount, bakedProbeOutputHashes, &allProbeOutputHash);
                    if (tickedRenderer.isComplete && tickedRenderer.inputHash == allProbeOutputHash)
                        bakingComplete = true;
                    else
                    {
                        // We must restart the renderer with the new data
                        tickedRenderer.Cancel();
                        var toBakeIDs = stackalloc int[addCount];
                        CoreUnsafeUtils.CopyToIndirect(
                            addCount, addIndices,
                            (byte*)bakedProbeIDs, (byte*)toBakeIDs,
                            UnsafeUtility.SizeOf<int>()
                        );
                        tickedRenderer.Start(
                            allProbeOutputHash,
                            settings,
                            addCount,
                            toBakeIDs, bakedProbeOutputHashes
                        );
                    }
                }

                if (!bakingComplete && !tickedRenderer.isComplete)
                    // Do one job this tick
                    bakingComplete = tickedRenderer.Tick();

                // = Step 5 =
                if (bakingComplete)
                {
                    if (remCount > 0)
                        bakedProbeHashes.RemoveIndices(remCount, remIndices);

                    for (int i = 0; i < addCount; ++i)
                    {
                        var index = addIndices[i];
                        var probeId = bakedProbeIDs[index];
                        var probe = (HDProbe)EditorUtility.InstanceIDToObject(probeId);
                        var probeScene = probe.gameObject.scene;
                        var bakedOutputHash = bakedProbeOutputHashes[index];
                        var probeOnlyHash = bakedProbeOnlyHashes[index];
                        var bakedTexturePathInCache = textureImporter.GetCacheBakePathFor(probe, bakedOutputHash);

                        if (!File.Exists(bakedTexturePathInCache))
                            continue;

                        var lightingAsset = HDLightingSceneAsset.GetOrCreateForScene(probeScene);
                        Texture bakedTexture;
                        if (lightingAsset.TryGetBakedTextureFor(probe, out bakedTexture))
                        {
                            var path = AssetDatabase.GetAssetPath(bakedTexture);
                            AssetDatabase.DeleteAsset(path);
                        }

                        var bakedPath = textureImporter.GetBakedPathFor(probe);
                        bakedTexture = textureImporter.ImportBakedTextureFromFile(
                            probe,
                            bakedTexturePathInCache,
                            bakedPath
                        );

                        lightingAsset.SetBakedTextureFor(probe, bakedTexture);
                        probe.bakedTexture = bakedTexture;

                        EditorUtility.SetDirty(lightingAsset);

                        bakedProbeHashes.Add(probeId, probeOnlyHash, bakedOutputHash);
                    }
                }

                return;
            }

            // Notify Unity we completed the baking
            handle.ExitStage((int)HDReflectionSystem.BakeStages.ReflectionProbes);
            handle.SetIsDone(true);
        }

        void DestroyUnusedCubemaps()
        {
            // TODO
        }

        void ComputeBakedProbeOutputHashes(
            Hash128* allBakedProbeHash,
            int bakedProbeCount,
            Hash128* bakedProbeOnlyHashes, Hash128* bakedProbeOutputHashes
        )
        {
            for (int i = 0; i < bakedProbeCount; ++i)
            {
                bakedProbeOutputHashes[i] = bakedProbeOnlyHashes[i]; // copy probe only data
                HashUtilities.AppendHash(ref *allBakedProbeHash, ref bakedProbeOutputHashes[i]);
            }
        }

        void ComputeProbeStateHashesAndGetEntityIDs(
            IEnumerator<HDProbe> enumerator,
            Hash128* bakedProbeOnlyHashes,
            int* bakedProbeIDs
        )
        {
            var i = 0;
            while (enumerator.MoveNext())
            {
                var bakedProbe = enumerator.Current;
                bakedProbeIDs[i] = bakedProbe.GetInstanceID();
                bakedProbeOnlyHashes[i] = bakedProbe.ComputeBakePropertyHashes();
                ++i;
            }
        }

        void ComputeAllCustomProbeHash(IEnumerator<HDProbe> enumerator, ref Hash128 hash)
        {
            while (enumerator.MoveNext())
            {
                var customProbe = enumerator.Current;
                if (customProbe.assets.customTexture == null)
                    continue;

                var customHash = customProbe.assets.customTexture.imageContentsHash;
                HashUtilities.AppendHash(ref customHash, ref hash);
            }
        }

        void ComputeAllBakedProbeHashWithBounces(
            Hash128* allProbeHash,
            int bakedProbeCount, Hash128* bakedProbeOnlyHashes,
            uint bounces
        )
        {
            if (bounces > 1)
            {
                // Adding a dependency to other baked probes
                for (int i = 0; i < bakedProbeCount; ++i)
                    HashUtilities.AppendHash(ref bakedProbeOnlyHashes[i], ref *allProbeHash);
            }
        }
    }
}
