using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class PlanarReflectionProbeUI
    {
        static readonly Color k_GizmoMirrorPlaneCamera = new Color(128f / 255f, 128f / 255f, 233f / 255f, 128f / 255f);

        [DrawGizmo(GizmoType.Selected)]
        internal static void DrawGizmos(PlanarReflectionProbe d, GizmoType gizmoType)
        {
            //HDProbeUI.DrawGizmos(d, gizmoType);
        }
    }
}
