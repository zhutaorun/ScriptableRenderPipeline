using UnityEditor;
using UnityEditor.Experimental.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    [CanEditMultipleObjects]
    // Disable the GUI for additional camera data
    [CustomEditor(typeof(AdditionalCameraData))]
    public class AdditionalCameraDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
        }
    }
}
