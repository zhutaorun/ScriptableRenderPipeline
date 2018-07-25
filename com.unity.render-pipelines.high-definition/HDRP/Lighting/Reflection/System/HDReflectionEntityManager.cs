using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.RenderPipelines.HighDefinition.Editor")]

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
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

    // Runtime

    struct HDReflectionEntityManager2
    {
        public int BakedProbeCount { get; internal set; }

        internal IEnumerator<HDProbe2> GetActiveBakedProbeEnumerator()
        {
            throw new NotImplementedException();
        }

        internal IEnumerator<HDProbe2> GetActiveCustomProbeEnumerator()
        {
            throw new NotImplementedException();
        }

        public HDProbe2 GetProbeByID(HDReflectionEntityID probeId)
        {
            throw new NotImplementedException();
        }
    }
}
