using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.SceneManagement;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public static class EditorHDLightingDataAsset
    {
        public static HDLightingDataAsset GetLightingDataAssetForScene(Scene scene)
        {
            var filePath = GetLightingDataAssetPathForScene(scene);
            var asset = AssetDatabase.LoadAssetAtPath<HDLightingDataAsset>(filePath);
            return asset;
        }

        public static HDLightingDataAsset GetOrCreateLightingDataAssetForScene(Scene scene)
        {
            var filePath = GetLightingDataAssetPathForScene(scene);
            var filePathInfo = new FileInfo(filePath);
            if (!filePathInfo.Directory.Exists)
                filePathInfo.Directory.Create();

            var asset = AssetDatabase.LoadAssetAtPath<HDLightingDataAsset>(filePath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<HDLightingDataAsset>();
                AssetDatabase.CreateAsset(asset, filePath);
            }

            return asset;
        }

        static string GetLightingDataAssetPathForScene(Scene scene)
        {
            var parentFolder = Path.GetFileNameWithoutExtension(scene.path);
            return Path.Combine(Path.GetDirectoryName(scene.path), Path.Combine(parentFolder, "HDLightingData.asset"));
        }
    }
}
