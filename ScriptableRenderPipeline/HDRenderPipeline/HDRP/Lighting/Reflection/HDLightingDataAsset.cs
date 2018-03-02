using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.Assertions;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class HDLightingDataAsset : ScriptableObject, ISerializationCallbackReceiver
    {
        [Serializable]
        class ProbeInformation
        {
            public SerializableSceneObjectIdentifier ID;
            public Texture BakedTexture;
        }

        [SerializeField]
        List<ProbeInformation> m_ObjectList = new List<ProbeInformation>();

        Dictionary<SceneObjectIdentifier, ProbeInformation> m_IndexByID = new Dictionary<SceneObjectIdentifier, ProbeInformation>();

        public void SetBakedTexture(SceneObjectIdentifier id, Texture bakedTexture)
        {
            Assert.AreNotEqual(SceneObjectIdentifier.Null, id);

            if (!m_IndexByID.ContainsKey(id))
                m_IndexByID[id] = new ProbeInformation { ID = id };

            m_IndexByID[id].BakedTexture = bakedTexture;
        }

        public void DeleteAssets()
        {
            foreach (var probeInformation in m_IndexByID)
            {
                if (probeInformation.Value.BakedTexture == null)
                    continue;

                var path = AssetDatabase.GetAssetPath(probeInformation.Value.BakedTexture);
                if (string.IsNullOrEmpty(path))
                    continue;

                AssetDatabase.DeleteAsset(path);
            }
        }

        public void OnBeforeSerialize()
        {
            m_ObjectList.Clear();
            m_ObjectList.AddRange(m_IndexByID.Select(s => s.Value));
        }

        public void OnAfterDeserialize()
        {
            for (var i = 0; i < m_ObjectList.Count; i++)
                m_IndexByID.Add(m_ObjectList[i].ID, m_ObjectList[i]);
        }
    }
}
