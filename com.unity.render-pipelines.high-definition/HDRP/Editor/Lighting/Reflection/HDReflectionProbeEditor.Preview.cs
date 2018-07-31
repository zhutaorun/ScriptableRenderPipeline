using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class HDReflectionProbeEditor
    {
        HDCubemapInspector m_CubemapEditor;

        public override bool HasPreviewGUI()
        {
            if (targets.Length > 1)
                return false;  // We only handle one preview for reflection probes

            // Ensure valid cube map editor (if possible)
            ReflectionProbe reflectionProbe;
            HDProbe probe;
            if (TryGetPreviewSetup(out reflectionProbe, out probe) && m_CubemapEditor == null)
            {
                Editor editor = m_CubemapEditor;
                CreateCachedEditor(probe.texture, typeof(HDCubemapInspector), ref editor);
                m_CubemapEditor = editor as HDCubemapInspector;
            }

            // If having one probe selected we always want preview (to prevent preview window from popping)
            return true;
        }

        public override void OnPreviewSettings()
        {
            ReflectionProbe reflectionProbe;
            HDProbe probe;
            if (!TryGetPreviewSetup(out reflectionProbe, out probe)
                || m_CubemapEditor == null)
                return;

            m_CubemapEditor.OnPreviewSettings();
        }

        public override void OnPreviewGUI(Rect position, GUIStyle style)
        {
            ReflectionProbe reflectionProbe;
            HDProbe probe;
            if (!TryGetPreviewSetup(out reflectionProbe, out probe)
                || m_CubemapEditor == null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                Color prevColor = GUI.color;
                GUI.color = new Color(1, 1, 1, 0.5f);
                GUILayout.Label("Reflection Probe not baked yet");
                GUI.color = prevColor;
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                return;
            }

            var p = target as ReflectionProbe;
            if (p != null && p.texture != null && targets.Length == 1)
                m_CubemapEditor.DrawPreview(position);
        }

        bool TryGetPreviewSetup(out ReflectionProbe reflectionProbe, out HDProbe probe)
        {
            reflectionProbe = target as ReflectionProbe;
            probe = reflectionProbe != null ? reflectionProbe.GetComponent<HDProbe>() : null;
            return probe != null && probe.texture != null;
        }

        private void OnDestroy()
        {
            DestroyImmediate(m_CubemapEditor);
        }
    }
}
