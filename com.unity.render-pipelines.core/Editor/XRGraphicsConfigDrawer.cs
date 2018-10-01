using UnityEditor;

namespace UnityEngine.Experimental.Rendering
{
    [CustomPropertyDrawer(typeof(XRGraphicsConfig))]
    public class XRGraphicsConfigDrawer : PropertyDrawer
    {
        internal class Styles
        {
            public static GUIContent XRSettingsLabel = new GUIContent("XR Config", "Enable XR in Player Settings, then enable SRP Override of XRSettings. Then the below values will be set to XRSettings by SRP.");
            public static GUIContent srpOverrideLabel = new GUIContent("Override XRSettings with SRP", "Overwrite default and non-SRP-programmed XRSettings with the values chosen in this SRP asset.");
            public static GUIContent useOcclusionMeshLabel = new GUIContent("Use Occlusion Mesh", "Determines whether or not to draw the occlusion mesh (goggles-shaped overlay) when rendering");
            public static GUIContent occlusionScaleLabel = new GUIContent("Occlusion Mesh Scale", "Scales the occlusion mesh");
            public static GUIContent warnVRDisabled = EditorGUIUtility.TrTextContent("VR is disabled in Player Settings.");

        }

        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var drawUseOcclusionMesh = property.FindPropertyRelative("useOcclusionMesh");
            var drawOcclusionMeshScale = property.FindPropertyRelative("occlusionMeshScale");
            var drawSRPOverride = property.FindPropertyRelative("useSRPOverride");

            EditorGUILayout.LabelField(Styles.XRSettingsLabel, EditorStyles.boldLabel);
            if (XRGraphicsConfig.tryEnable)
            {
                EditorGUILayout.PropertyField(drawSRPOverride, Styles.srpOverrideLabel);
                EditorGUI.BeginDisabledGroup(!drawSRPOverride.boolValue);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(drawUseOcclusionMesh, Styles.useOcclusionMeshLabel);
                EditorGUILayout.PropertyField(drawOcclusionMeshScale, Styles.occlusionScaleLabel);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUILayout.HelpBox(Styles.warnVRDisabled.text, MessageType.Warning);
            }
        }
    }
}
