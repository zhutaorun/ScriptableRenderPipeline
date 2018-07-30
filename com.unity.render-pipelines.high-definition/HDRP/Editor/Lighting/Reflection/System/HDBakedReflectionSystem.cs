using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class BakedProbeHashes
    {
        public int count { get; private set; }
        public List<Hash128> probeOnlyHashes { get; private set; }
        public List<Hash128> probeOutputHashes { get; private set; }
        public List<int> InstanceIDs { get; private set; }

        public BakedProbeHashes()
        {
            count = 0;
            probeOnlyHashes = new List<Hash128>();
            probeOutputHashes = new List<Hash128>();
            InstanceIDs = new List<int>();
        }

        internal void Add(int probeId, Hash128 probeOnlyHash, Hash128 bakedOutputHash)
        {
            // Keep the array sorted
            // It is most likely to have small arrays so it is ok concerning performance.
            var insertAt = -1;
            if (probeOutputHashes.Count == 0)
                insertAt = 0;
            else if (probeOutputHashes.Count > 0)
            {
                for (int i = 0; i < probeOutputHashes.Count - 1; ++i)
                {
                    if (probeOutputHashes[i] < bakedOutputHash
                        && (bakedOutputHash < probeOutputHashes[i + 1] || bakedOutputHash == probeOutputHashes[i + 1]))
                    {
                        insertAt = i;
                        break;
                    }
                }
                if (insertAt == -1)
                    insertAt = count;
            }

            ++count;
            InstanceIDs.Insert(insertAt, probeId);
            probeOnlyHashes.Insert(insertAt, probeOnlyHash);
            probeOutputHashes.Insert(insertAt, bakedOutputHash);
        }

        internal unsafe void RemoveIndices(int remCount, int* remIndices)
        {
            for (int i = remCount - 1; i >= 0; --i)
            {
                --count;
                var index = remIndices[i];
                InstanceIDs.RemoveAt(index);
                probeOnlyHashes.RemoveAt(index);
                probeOutputHashes.RemoveAt(index);
            }
        }
    }

    struct ReflectionSettings
    {
        public uint bounces;
    }

    unsafe class HDBakedReflectionSystem : IScriptableBakedReflectionSystem
    {
        public enum BakeStages
        {
            ReflectionProbes
        }

        [InitializeOnLoadMethod]
        static void RegisterSystem()
        {
            ScriptableBakedReflectionSystemSettings.system = new HDBakedReflectionSystem();
        }

        BakedProbeHashes m_BakedProbeState = new BakedProbeHashes();
        HDProbeTickedRenderer m_TickedRenderer = new HDProbeTickedRenderer();

        public int stageCount { get { return 1; } }

        public Hash128 stateHash
        {
            get
            {
                // TODO: cache the hash when updating the baked state
                var hash = new Hash128();
                if (m_BakedProbeState != null)
                {
                    CoreUtils.CombineHashes(m_BakedProbeState.probeOutputHashes, ref hash);
                }
                return hash;
            }
        }

        public void Cancel()
        {
        }

        public void Clear()
        {
        }

        public void SynchronizeReflectionProbes()
        {
        }

        public bool BakeAllReflectionProbes()
        {
            var baker = new HDReflectionSystemBakeAllReflectionProbes
            {
                entitySystem = HDReflectionEntitySystem.instance,
                settings = new ReflectionSettings() // TODO: Set lighting settings here
            };
            return baker.BakeAllReflectionProbes();
        }

        public void Tick(SceneStateHash sceneStateHash, IScriptableBakedReflectionSystemStageNotifier handle)
        {
            var tick = new HDReflectionSystemTick
            {
                bakedProbeHashes = m_BakedProbeState,
                entitySystem = HDReflectionEntitySystem.instance,
                tickedRenderer = m_TickedRenderer,
                settings = new ReflectionSettings() // TODO: Set lighting settings here
            };
            tick.Tick(sceneStateHash, handle);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        void Dispose(bool disposing)
        {
            
        }
    }
}
