using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    internal enum HDReflectionEntityType
    {
        ReflectionProbe,
        PlanarProbe
    }

    public struct HDReflectionEntityID
    {
        internal struct SetID
        {
            public readonly int entityId;
            public readonly int version;

            public SetID(int entityId, int version)
            {
                this.entityId = entityId;
                this.version = version;
            }
        }
        internal SetID setId;
        internal readonly HDReflectionEntityType type;

        internal HDReflectionEntityID(SetID setId, HDReflectionEntityType type)
        {
            this.setId = setId;
            this.type = type;
        }
    }

    // Entity manager to query linearly GameObject registered for a specific type.
    public class HDReflectionEntityManager : IDisposable
    {
        class EntitySet : IDisposable
        {
            Stack<int> m_FreedEntityIds = new Stack<int>();
            List<int> m_EntityIDMap = new List<int>();
            List<int> m_EntityIDVersion = new List<int>();
            List<GameObject> m_Entities = new List<GameObject>();

            public void Dispose()
            {
                m_Entities.Clear();
                m_Entities = null;
                m_EntityIDMap.Clear();
                m_EntityIDMap = null;
                m_EntityIDVersion.Clear();
                m_EntityIDVersion = null;
                m_FreedEntityIds.Clear();
                m_FreedEntityIds = null;
            }

            public HDReflectionEntityID.SetID Register(GameObject obj)
            {
                var entityId = GetOrCreateNextEntityId();

                // Insert entity
                var index = m_Entities.Count;
                m_Entities.Add(obj);

                // Update entity table
                m_EntityIDMap[entityId] = index;
                var version = ++m_EntityIDVersion[entityId];

                return new HDReflectionEntityID.SetID(entityId, version);
            }

            public void Unregister(HDReflectionEntityID.SetID setId)
            {
                if (setId.entityId >= m_EntityIDVersion.Count
                    || setId.entityId < 0
                    || m_Entities.Count == 0)
                    return;

                var version = m_EntityIDVersion[setId.entityId];
                if (version != setId.version)
                    return;

                // Swap and remove
                var entityId = setId.entityId;
                var mapId = m_EntityIDMap[entityId];
                if (mapId < m_Entities.Count - 1)
                {
                    var lastEntityId = m_EntityIDMap.IndexOf(m_Entities.Count - 1);
                    m_Entities[mapId] = m_Entities[m_Entities.Count - 1];
                    m_EntityIDMap[lastEntityId] = mapId;
                    m_Entities.RemoveAt(m_Entities.Count - 1);
                }

                // Mark as freed
                m_EntityIDMap[entityId] = -1;
                ++m_EntityIDVersion[entityId];
                m_FreedEntityIds.Push(entityId);
            }

            int GetOrCreateNextEntityId()
            {
                var entityId = -1;
                if (m_FreedEntityIds.Count > 0)
                    entityId = m_FreedEntityIds.Pop();
                else
                {
                    entityId = m_EntityIDMap.Count;
                    m_EntityIDMap.Add(-1);
                    m_EntityIDVersion.Add(0);
                }
                return entityId;
            }
        }

        EntitySet[] m_Entities;

        public HDReflectionEntityManager()
        {
            m_Entities = new EntitySet[CoreUtils.GetEnumLength<HDReflectionEntityType>()];
            for (int i = 0; i < m_Entities.Length; ++i)
                m_Entities[i] = new EntitySet();
        }

        internal HDReflectionEntityID RegisterEntity(GameObject obj, HDReflectionEntityType type)
        {
            var entitySet = m_Entities[(int)type];
            var index = entitySet.Register(obj);
            return new HDReflectionEntityID(index, type);
        }

        internal void UnregisterEntity(HDReflectionEntityID id)
        {
            var entitySet = m_Entities[(int)id.type];
            entitySet.Unregister(id.setId);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        void Dispose(bool disposing)
        {
            if (disposing)
            {
                CoreUtils.DisposeArray(ref m_Entities);
            }
        }
    }

    struct HDReflectionEntityManager2
    {
        public int BakedProbeCount { get; internal set; }

        internal IEnumerator<HDProbe> GetActiveBakedProbeEnumerator()
        {
            throw new NotImplementedException();
        }

        internal IEnumerator<HDProbe> GetActiveCustomProbeEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    struct HDProbe
    {
        internal HDReflectionEntityID entityId;
        public Vector3 capturePosition;
        internal Hash128 customTextureHash;
    }

    struct ReflectionSettings
    {
        public uint bounces;
    }

    unsafe struct HDReflectionSystemTick
    {
        public ReflectionSettings settings;

        internal void Tick(SceneStateHash sceneStateHash, IScriptableBakedReflectionSystemStageNotifier handle)
        {
            // Temp
            var entityManager = new HDReflectionEntityManager2();
            // End temp

            // Allocate stack variables
            var bakedProbeCount = entityManager.BakedProbeCount;
            var bakedProbeOnlyHashes = stackalloc Hash128[bakedProbeCount];
            var bakedProbeIDs = stackalloc HDReflectionEntityID[bakedProbeCount];
            // Initialize
            ComputeProbeStateHashes(entityManager, bakedProbeOnlyHashes, bakedProbeIDs);

            var allBakedProbeHash = new Hash128();
            // Baked probes depends on other baked probes when baking several bounces
            ComputeAllBakedProbeHashWithBounces(
                &allBakedProbeHash, bakedProbeCount, bakedProbeOnlyHashes, settings.bounces
            );

            // Baked probes depends on probes with custom textures
            var allCustomProbeHash = new Hash128();
            ComputeAllCustomProbeHash(entityManager.GetActiveCustomProbeEnumerator(), ref allCustomProbeHash);
            HashUtilities.AppendHash(ref allCustomProbeHash, ref allBakedProbeHash);

            // Baked probes depends on scene gameobjects (static geometry, lights)
            var sceneObjectHash = sceneStateHash.sceneObjectsHash;
            HashUtilities.AppendHash(ref sceneObjectHash, ref allBakedProbeHash);
            // Baked probes depends on sky settings
            var skySettingsHash = sceneStateHash.skySettingsHash;
            HashUtilities.AppendHash(ref skySettingsHash, ref allBakedProbeHash);

            // Compute the hash of the data that should have been baked for each probe.
            var bakedProbeOutputHashes = stackalloc Hash128[bakedProbeCount];
            ComputeBakedProbeOutputHashes(
                &allBakedProbeHash,
                bakedProbeCount,
                bakedProbeOnlyHashes, bakedProbeOutputHashes
            );

            // Now, we can compare the expected hash to the current baked hash of the probes
            // If those are different, it means that the probe has obsolete baked data
            // We must then trigger a baking of these probes.
            // But we must also take care their baking may be currently running.

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

        void ComputeProbeStateHashes(
            HDReflectionEntityManager2 entityManager,
            Hash128* bakedProbeOnlyHashes,
            HDReflectionEntityID* bakedProbeIDs
        )
        {
            var i = 0;
            var enumerator = entityManager.GetActiveBakedProbeEnumerator();
            while (enumerator.MoveNext())
            {
                var bakedProbe = enumerator.Current;
                bakedProbeIDs[i] = bakedProbe.entityId;
                ComputeProbeStateHash(&bakedProbe, bakedProbeOnlyHashes + i);
                ++i;
            }
        }

        void ComputeAllCustomProbeHash(IEnumerator<HDProbe> enumerator, ref Hash128 hash)
        {
            while (enumerator.MoveNext())
            {
                var customProbe = enumerator.Current;
                var customHash = customProbe.customTextureHash;
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

        void ComputeProbeStateHash(HDProbe* probe, Hash128* outHash)
        {
            var hash = new Hash128();
            HashUtilities.QuantisedVectorHash(ref probe->capturePosition, ref hash);
            *outHash = hash;
        }
    }


    unsafe class HDReflectionSystem : IScriptableBakedReflectionSystem
    {
        [InitializeOnLoadMethod]
        static void RegisterSystem()
        {
            ScriptableBakedReflectionSystemSettings.system = new HDReflectionSystem();
        }

        public int stageCount { get { throw new System.NotImplementedException(); } }

        public Hash128 stateHash { get { throw new System.NotImplementedException(); } }

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
