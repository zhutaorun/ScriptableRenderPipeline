using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class BakedProbeHashes
    {
        public int count;
        public Hash128[] probeOnlyHashes;
        public Hash128[] probeOutputHashes;
        public HDReflectionEntityID[] IDs;
    }

    struct ReflectionSettings
    {
        public uint bounces;
    }

    unsafe class HDReflectionSystem : IScriptableBakedReflectionSystem
    {
        public enum BakeStages
        {
            ReflectionProbes
        }

        [InitializeOnLoadMethod]
        static void RegisterSystem()
        {
            ScriptableBakedReflectionSystemSettings.system = new HDReflectionSystem();
        }

        BakedProbeHashes m_BakedProbeState;

        public int stageCount { get { return 1; } }

        public Hash128 stateHash
        {
            get
            {
                // TODO: cache the hash when updating the baked state
                var hash = new Hash128();
                if (m_BakedProbeState != null && m_BakedProbeState.probeOutputHashes != null)
                {
                    fixed (Hash128* hashes = m_BakedProbeState.probeOutputHashes)
                        Utilities.CombineHashes(m_BakedProbeState.count, hashes, &hash);
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

        public void Tick(SceneStateHash sceneStateHash, IScriptableBakedReflectionSystemStageNotifier handle)
        {
            var tick = new HDReflectionSystemTick();
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
