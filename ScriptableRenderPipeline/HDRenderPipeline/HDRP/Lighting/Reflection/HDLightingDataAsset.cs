using System;
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class HDLightingDataAsset : ScriptableObject, ISerializationCallbackReceiver
    {
        [Serializable]
        class ObjectIDPair
        {
            public SceneObjectIdentifier Key;
            public Object Value;
        }

        [SerializeField]
        List<ObjectIDPair> m_ObjectList = new List<ObjectIDPair>();

        Dictionary<Object, SceneObjectIdentifier> m_ObjectIndex = new Dictionary<Object, SceneObjectIdentifier>();
        Dictionary<SceneObjectIdentifier, Object> m_IDIndex = new Dictionary<SceneObjectIdentifier, Object>();

        public SceneObjectIdentifier GetIdFor(Object probe)
        {
            if (m_ObjectIndex.ContainsKey(probe))
                return m_ObjectIndex[probe];

            var id = EditorUtility.GetSceneObjectIdentifierFor(probe);
            if (id == SceneObjectIdentifier.Null)
            {
                Debug.LogWarningFormat("Could not get the scene object id for {0}", probe);
                return SceneObjectIdentifier.Null;
            }

            m_ObjectList.Add(new ObjectIDPair
            {
                Key = id,
                Value = probe
            });

            m_ObjectIndex[probe] = id;
            m_IDIndex[id] = probe;

            return id;
        }

        public void OnBeforeSerialize()
        {
            for (var i = m_ObjectList.Count - 1; i >= 0; --i)
            {
                if (m_ObjectList[i].Value == null)
                    m_ObjectList.RemoveAt(i);
            }
        }

        public void OnAfterDeserialize()
        {
            for (var i = 0; i < m_ObjectList.Count; i++)
            {
                if (m_IDIndex.ContainsKey(m_ObjectList[i].Key))
                    Debug.LogErrorFormat(this, "ID {0} is a duplicated in ReflectionSystemSceneDictionary ({1}) for {2}", m_ObjectList[i].Key, this, m_ObjectList[i].Value);

                m_ObjectIndex[m_ObjectList[i].Value] = m_ObjectList[i].Key;
                m_IDIndex[m_ObjectList[i].Key] = m_ObjectList[i].Value;
            }
        }
    }
}
