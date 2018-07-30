using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    class HDReflectionEntitySystem
    {
        class IterableHashSet<T>
        {
            HashSet<T> m_Storage = new HashSet<T>();
            T[] m_LinearStorage = null;
            bool m_LinearStorageDirty = true;

            public bool Add(T value)
            {
                var success = m_Storage.Add(value);
                m_LinearStorageDirty |= m_LinearStorageDirty;
                return success;
            }

            public bool Remove(T value)
            {
                var success = m_Storage.Remove(value);
                m_LinearStorageDirty |= m_LinearStorageDirty;
                return success;
            }

            public T[] linearStorage
            {
                get
                {
                    if (m_LinearStorageDirty)
                    {
                        m_LinearStorageDirty = false;
                        Array.Resize(ref m_LinearStorage, m_Storage.Count);
                        m_Storage.CopyTo(m_LinearStorage);
                    }
                    return m_LinearStorage;
                }
            }
        }

        static HDReflectionEntitySystem s_Instance = null;
        public static HDReflectionEntitySystem instance
        { get { return s_Instance ?? (s_Instance = new HDReflectionEntitySystem()); } }

        IterableHashSet<HDProbe> m_ActiveBakedProbes = new IterableHashSet<HDProbe>();
        IterableHashSet<HDProbe> m_ActiveCustomProbes = new IterableHashSet<HDProbe>();

        internal HDProbe[] GetActiveBakedProbes() { return m_ActiveBakedProbes.linearStorage; }
        internal HDProbe[] GetActiveCustomProbes() { return m_ActiveCustomProbes.linearStorage; }

        internal void Register(HDProbe probe)
        {
            switch (probe.captureProperties.mode)
            {
                case HDReflectionProbeMode.Baked:
                    m_ActiveBakedProbes.Add(probe);
                    break;
                case HDReflectionProbeMode.Custom:
                    m_ActiveCustomProbes.Add(probe);
                    break;
            }
        }

        internal void Unregister(HDProbe probe)
        {
            m_ActiveBakedProbes.Remove(probe);
            m_ActiveCustomProbes.Remove(probe);
        }
    }
}
