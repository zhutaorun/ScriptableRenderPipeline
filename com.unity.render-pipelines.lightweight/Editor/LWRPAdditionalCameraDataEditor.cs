using UnityEditor;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    [CanEditMultipleObjects]
    // Disable the GUI for additional camera data
    [CustomEditor(typeof(LWRPAdditionalCameraData))]
    class AdditionalCameraDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
        }
    }
}
