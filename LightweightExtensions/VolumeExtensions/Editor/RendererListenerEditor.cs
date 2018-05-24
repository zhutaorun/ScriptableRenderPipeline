using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering
{
    [CustomEditor(typeof(RendererListener))]
	public class RendererListenerEditor : Editor 
	{
		internal class Styles
        {
            public static GUIContent volumeLayerLabel = new GUIContent("Layer");
		}
		
		SerializedProperty m_VolumeLayer;

		void OnEnable()
		{
			m_VolumeLayer = serializedObject.FindProperty("m_VolumeLayer");
		}

		public override void OnInspectorGUI()
        {	
			serializedObject.Update();

			EditorGUILayout.PropertyField(m_VolumeLayer, Styles.volumeLayerLabel);

			serializedObject.ApplyModifiedProperties();
		}
	}
}
