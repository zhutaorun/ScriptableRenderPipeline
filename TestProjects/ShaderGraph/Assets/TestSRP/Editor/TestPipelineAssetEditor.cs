using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.Rendering;

namespace ShaderGraph.Tests
{
    [CustomEditor(typeof(TestPipelineAsset))]
    public class TestPipelineAssetEditor : Editor
    {
        [MenuItem("Assets/Create/Rendering/Test Pipeline Asset", priority = CoreUtils.assetCreateMenuPriority1)]
        static void CreateAsset()
        {
            var asset = ScriptableObject.CreateInstance<TestPipelineAsset>();
            AssetDatabase.CreateAsset(asset, "Assets/New TestPipelineAsset.asset");
            Selection.activeObject = asset;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }
    }
}