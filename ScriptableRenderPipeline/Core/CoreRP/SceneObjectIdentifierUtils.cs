using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace UnityEngine.Experimental.Rendering
{
    public static class SceneObjectIdentifierUtils
    {
        public static SceneObjectIdentifier GetSceneObjectIdentifierFor(Component obj)
        {
            return GetSceneObjectIdentifierFor(obj.gameObject);
        }

        public static SceneObjectIdentifier GetSceneObjectIdentifierFor(GameObject obj)
        {
            // Not a scene object
            if (!obj.scene.IsValid())
                return SceneObjectIdentifier.Invalid;

            var dict = GetSceneDictionary(obj.scene);

            return dict.GetIdFor(obj);
        }

        static SceneObjectIdentifierDictionary GetSceneDictionary(Scene scene)
        {
            Assert.IsTrue(scene.IsValid());
            Assert.IsTrue(scene.isLoaded);

            SceneObjectIdentifierDictionary result = null;

            var roots = scene.GetRootGameObjects();
            for (int i = 0, c = roots.Length; i < c; i++)
            {
                var root = roots[i];
                result = root.GetComponent<SceneObjectIdentifierDictionary>();
                if (result != null)
                    break;
            }

            if (result == null)
            {
                var go = new GameObject("Scene Object Identifier Dictionary")
                {
                    hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector 
                };
                result = go.AddComponent<SceneObjectIdentifierDictionary>();
                SceneManager.MoveGameObjectToScene(go, scene);
            }

            result.gameObject.hideFlags = HideFlags.None;

            return result;
        }
    }
}
