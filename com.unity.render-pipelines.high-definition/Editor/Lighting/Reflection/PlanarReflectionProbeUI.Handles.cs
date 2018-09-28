using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class PlanarReflectionProbeUI
    {
        static readonly Color k_GizmoMirrorPlaneCamera = new Color(128f / 255f, 128f / 255f, 233f / 255f, 128f / 255f);

        internal static void DrawHandles(PlanarReflectionProbeUI s, SerializedPlanarReflectionProbe d, Editor o)
        {
            PlanarReflectionProbe probe = d.target;
            HDProbeUI.DrawHandles(s, d, o);

            var referencePosition = d.target.transform.TransformPoint(d.localReferencePosition.vector3Value);
            referencePosition = Handles.PositionHandle(referencePosition, d.target.transform.rotation);
            d.localReferencePosition.vector3Value = d.target.transform.InverseTransformPoint(referencePosition);
            d.serializedObject.ApplyModifiedProperties();
        }

        [DrawGizmo(GizmoType.Selected)]
        internal static void DrawGizmos(PlanarReflectionProbe d, GizmoType gizmoType)
        {
            HDProbeUI.DrawGizmos(d, gizmoType);
        }

        static void DrawGizmos_CaptureMirror(PlanarReflectionProbe d)
        {
            var c = Gizmos.color;
            var m = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(
                    d.captureMirrorPlanePosition,
                    Quaternion.LookRotation(d.captureMirrorPlaneNormal, Vector3.up),
                    Vector3.one);
            Gizmos.color = k_GizmoMirrorPlaneCamera;

            Gizmos.DrawCube(Vector3.zero, new Vector3(1, 1, 0));

            Gizmos.matrix = m;
            Gizmos.color = c;
        }
    }
}
