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
    class HDLightingSceneAsset : MonoBehaviour
    {
        [Serializable]
        struct Data
        {
            public GameObject gameObject;
            public Texture bakedTexture;
            public RenderData renderData;
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

        [SerializeField]
        Data[] m_Data;

        internal bool TryGetBakedTextureFor(HDProbe probe, out Texture bakedTexture)
        {
            int index = IndexOf(probe);
            if (index >= 0)
            {
                var data = m_Data[index];
                bakedTexture = data.bakedTexture;
                return true;
            }

            bakedTexture = null;
            return false;
        }

        internal void SetBakedTextureFor(HDProbe probe, Texture bakedTexture, RenderData renderData)
        {
            int index = IndexOf(probe);
            if (index == -1)
            {
                Array.Resize(ref m_Data, m_Data.Length + 1);
                index = m_Data.Length - 1;
                m_Data[index] = new Data { gameObject = probe.gameObject };
            }

            var data = m_Data[index];
            data.bakedTexture = bakedTexture;
            data.renderData = renderData;
            m_Data[index] = data;
        }

        int IndexOf(HDProbe probe)
        {
            for (int i = 0; i < m_Data.Length; ++i)
            {
                if (m_Data[i].gameObject == probe.gameObject)
                    return i;
            }
            return -1;
        }
    }
}
