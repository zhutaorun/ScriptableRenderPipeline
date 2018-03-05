using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering
{
    public class SceneObjectIdentifierDictionary : MonoBehaviour, ISerializationCallbackReceiver
    {
        [Serializable]
        struct Entry
        {
            public SceneObjectIdentifier id;
            public Object value;
        }

        [SerializeField]
        List<Entry> m_Entries = new List<Entry>();

        Dictionary<SceneObjectIdentifier, Entry> m_IndexById = new Dictionary<SceneObjectIdentifier, Entry>();
        Dictionary<Object, Entry> m_IndexByValue = new Dictionary<Object, Entry>();

        public SceneObjectIdentifier GetIdFor(Component obj)
        {
            return GetIdFor(obj.gameObject);
        }

        public SceneObjectIdentifier GetIdFor(GameObject obj)
        {
            var scene = obj.scene;
            if (!scene.IsValid() || gameObject.scene != scene)
                return SceneObjectIdentifier.Invalid;

            Entry e;
            if (!m_IndexByValue.TryGetValue(obj, out e))
            {
                e = new Entry
                {
                    id = new SceneObjectIdentifier(NextId()),
                    value = obj
                };

                if (e.id == SceneObjectIdentifier.Invalid)
                {
                    Debug.LogError("Reached maximum limit for SceneObjectIdentifier.");
                    return SceneObjectIdentifier.Invalid;
                }

                Insert(e);
            }

            return e.id;
        }

        int NextId()
        {
            var candidateId = 0;

            // Ids goes 0 -> Int.MaxValue, Int.MinValue -> -2
            // -1 is invalid
            while (candidateId != -1
                && m_IndexById.ContainsKey(new SceneObjectIdentifier(candidateId)))
                ++candidateId;

            return candidateId;
        }

        public void OnBeforeSerialize()
        {
            m_Entries.Clear();
            m_Entries.AddRange(m_IndexById.Values);
        }

        public void OnAfterDeserialize()
        {
            m_IndexById.Clear();
            m_IndexByValue.Clear();

            for (var i = 0; i < m_Entries.Count; i++)
            {
                var entry = m_Entries[i];
                Insert(entry);
            }
        }

        void Insert(Entry entry)
        {
            m_IndexById[entry.id] = entry;
            m_IndexByValue[entry.value] = entry;
        }
    }
}
