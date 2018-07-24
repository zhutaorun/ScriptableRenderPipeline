using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    /// <summary>
    /// Reference all generated assets for probes.
    ///
    /// It is intended to be a storage only for the editor.
    /// </summary>
    class HDLightingSceneAsset : MonoBehaviour, ISerializationCallbackReceiver
    {
        [Serializable]
        struct Data
        {
            public GameObject gameObject;
            public Texture bakedTexture;
        }

        public static HDLightingSceneAsset GetOrCreateForScene(Scene scene)
        {
            var list = new List<GameObject>(scene.rootCount);
            scene.GetRootGameObjects(list);

            HDLightingSceneAsset asset = null;
            for (int i = 0, c = list.Count; i < c && asset == null; ++i)
                asset = list[i].GetComponent<HDLightingSceneAsset>();

            if (asset == null)
            {
                var go = new GameObject("HDLightSceneAsset")
                {
                    hideFlags = HideFlags.DontSaveInBuild
                };
                SceneManager.MoveGameObjectToScene(go, scene);
                asset = go.AddComponent<HDLightingSceneAsset>();
            }

            return asset;
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
