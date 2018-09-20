using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class SceneObjectIDMap
    {
        public static bool TryGetSceneObjectID(GameObject gameObject, out int index)
        {
            if (gameObject == null)
                throw new ArgumentNullException("gameObject");

            var map = GetOrCreateSceneIDMapFor(gameObject.scene);
            return map.TryGetSceneIDFor(gameObject, out index);
        }

        public static int GetSceneObjectID(GameObject gameObject)
        {
            if (gameObject == null)
                throw new ArgumentNullException("gameObject");

            var map = GetOrCreateSceneIDMapFor(gameObject.scene);
            int index;
            if (map.TryGetSceneIDFor(gameObject, out index))
                return index;

            var insertion = map.TryInsert(gameObject, out index);
            Assert.IsTrue(insertion);
            return index;
        }

        static bool TryGetSceneIDMapFor(Scene scene, out SceneObjectIDMapSceneAsset map)
        {
            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; ++i)
            {
                if (roots[i].name == SceneObjectIDMapSceneAsset.k_GameObjectName)
                {
                    map = roots[i].GetComponent<SceneObjectIDMapSceneAsset>();
                    return true;
                }
            }
            map = null;
            return false;
        }

        static SceneObjectIDMapSceneAsset CreateSceneIDMapFor(Scene scene)
        {
            var gameObject = new GameObject(SceneObjectIDMapSceneAsset.k_GameObjectName)
            {
                hideFlags = HideFlags.DontSaveInBuild
                | HideFlags.HideInHierarchy
                | HideFlags.HideInInspector
            };
            var result = gameObject.AddComponent<SceneObjectIDMapSceneAsset>();
            SceneManager.MoveGameObjectToScene(gameObject, scene);
            return result;
        }

        static SceneObjectIDMapSceneAsset GetOrCreateSceneIDMapFor(Scene scene)
        {
            SceneObjectIDMapSceneAsset map;
            if (!TryGetSceneIDMapFor(scene, out map))
                map = CreateSceneIDMapFor(scene);

            return map;
        }
    }

    class SceneObjectIDMapSceneAsset : MonoBehaviour, ISerializationCallbackReceiver
    {
        internal const string k_GameObjectName = "SceneIDMap";

        [SerializeField]
        List<GameObject> m_GameObjects = new List<GameObject>();

        Dictionary<GameObject, int> m_Index = new Dictionary<GameObject, int>();

        internal bool TryGetSceneIDFor(GameObject gameObject, out int index)
        {
            return m_Index.TryGetValue(gameObject, out index);
        }

        internal bool TryInsert(GameObject gameObject, out int index)
        {
            if (gameObject.scene != this.gameObject.scene)
            {
                index = -1;
                return false;
            }
            if (TryGetSceneIDFor(gameObject, out index))
                return false;

            index = Insert(gameObject);
            return true;
        }

        int Insert(GameObject gameObject)
        {
            Assert.IsFalse(m_Index.ContainsKey(gameObject));
            Assert.AreEqual(gameObject.scene, this.gameObject.scene);

            var index = m_GameObjects.Count;
            m_Index.Add(gameObject, index);
            m_GameObjects.Add(gameObject);
            return index;
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            m_Index.Clear();
            for (int i = 0; i < m_GameObjects.Count; ++i)
                m_Index.Add(m_GameObjects[i], i);
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }
    }
}
