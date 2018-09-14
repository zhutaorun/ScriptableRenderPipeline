using UnityEditor;

namespace UnityEngine.Experimental.Rendering
{
    [CustomPropertyDrawer(typeof(XRGraphicsConfig))]
    public class XRGraphicsConfigDrawer : PropertyDrawer
    {
        bool xrSettingsFoldout = false;
        internal class Styles
        {
            public static GUIContent XRSettingsLabel = new GUIContent("XR Configuration", "Enable XR in Player Settings. Then SetConfig can be used to set this configuration to XRSettings.");
            public static GUIContent useOcclusionMeshLabel = new GUIContent("Use Occlusion Mesh", "Determines whether or not to draw the occlusion mesh (goggles-shaped overlay) when rendering");
            public static GUIContent occlusionScaleLabel = new GUIContent("Occlusion Mask Scale", "Scales the occlusion mask");

        }
        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var drawUseOcclusionMesh = property.FindPropertyRelative("useOcclusionMesh");
            var drawOcclusionMaskScale = property.FindPropertyRelative("occlusionMaskScale");

            xrSettingsFoldout = EditorGUILayout.Foldout(xrSettingsFoldout, Styles.XRSettingsLabel, true);
            if (xrSettingsFoldout)
            {
                EditorGUI.BeginDisabledGroup(!XRGraphicsConfig.tryEnable);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(drawUseOcclusionMesh, Styles.useOcclusionMeshLabel);
                EditorGUILayout.PropertyField(drawOcclusionMaskScale, Styles.occlusionScaleLabel);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
                EditorGUI.EndDisabledGroup();
            }
        }
    }
}
