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

        Dictionary<HDProbe2, int> m_IndexByProbe = new Dictionary<HDProbe2, int>();

        [SerializeField]
        Data[] m_Data;

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            m_IndexByProbe.Clear();

            if (m_Data == null)
                return;

            for (int i = 0; i < m_Data.Length; ++i)
            {
                var probe = m_Data[i].gameObject.GetComponent<HDProbe2>();
                if (probe != null)
                    m_IndexByProbe.Add(probe, i);
            }
        }

        internal bool TryGetBakedTextureFor(HDProbe2 probe, out Texture bakedTexture)
        {
            int index;
            if (m_IndexByProbe.TryGetValue(probe, out index))
            {
                var data = m_Data[index];
                bakedTexture = data.bakedTexture;
                return true;
            }

            bakedTexture = null;
            return false;
        }

        internal void SetBakedTextureFor(HDProbe2 probe, Texture bakedTexture)
        {
            int index;
            if (!m_IndexByProbe.TryGetValue(probe, out index))
            {
                Array.Resize(ref m_Data, m_Data.Length + 1);
                index = m_Data.Length - 1;
                m_Data[index] = new Data { gameObject = probe.gameObject };
            }

            var data = m_Data[index];
            data.bakedTexture = bakedTexture;
            m_Data[index] = data;
        }
    }
}
