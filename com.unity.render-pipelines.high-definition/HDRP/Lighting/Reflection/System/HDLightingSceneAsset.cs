using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    class HDLightingSceneAsset : MonoBehaviour, ISerializationCallbackReceiver
    {
        [Serializable]
        struct Data
        {
            public GameObject gameObject;
            public Texture bakedTexture;
        }

        Dictionary<GameObject, Data> m_DataDictionary = new Dictionary<GameObject, Data>();

        [SerializeField]
        Data[] m_Data;

        public void OnBeforeSerialize()
        {
            Array.Resize(ref m_Data, m_DataDictionary.Count);
            var i = 0;
            foreach (var p in m_DataDictionary)
                m_Data[i++] = p.Value;
        }

        public void OnAfterDeserialize()
        {
            m_DataDictionary.Clear();

            if (m_Data == null)
                return;

            for (int i = 0; i < m_Data.Length; ++i)
                m_DataDictionary.Add(m_Data[i].gameObject, m_Data[i]);
        }
    }
}
