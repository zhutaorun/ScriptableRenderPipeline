using System;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class HDReflectionProbeEditor
    {
        static Mesh sphere;
        static Material material;

        [DrawGizmo(GizmoType.Selected)]
        static void DrawSelectedGizmo(ReflectionProbe reflectionProbe, GizmoType gizmoType)
        {
            var e = GetEditorFor(reflectionProbe);
            if (e == null)
                return;

            var mat = Matrix4x4.TRS(reflectionProbe.transform.position, reflectionProbe.transform.rotation, Vector3.one);
            var hdprobe = reflectionProbe.GetComponent<HDAdditionalReflectionData>();
            InfluenceVolumeUI.DrawGizmos(
                e.m_UIState.probeSettings.influence,
                hdprobe.influenceVolume,
                mat,
                InfluenceVolumeUI.HandleType.None,
                InfluenceVolumeUI.HandleType.Base | InfluenceVolumeUI.HandleType.Influence
            );

            Gizmos_CapturePoint(reflectionProbe);
            DrawVerticalRay(reflectionProbe.transform);
        }

        static void DrawVerticalRay(Transform transform)
        {
            var ray = new Ray(transform.position, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Handles.color = Color.green;
                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                Handles.DrawLine(transform.position - Vector3.up * 0.5f, hit.point);
                Handles.DrawWireDisc(hit.point, hit.normal, 0.5f);

                Handles.color = Color.red;
                Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                Handles.DrawLine(transform.position, hit.point);
                Handles.DrawWireDisc(hit.point, hit.normal, 0.5f);
            }
        }

        static void Gizmos_CapturePoint(ReflectionProbe target)
        {
            if(sphere == null)
                sphere = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            if(material == null)
                material = new Material(Shader.Find("Debug/ReflectionProbePreview"));

            material.SetTexture("_Cubemap", GetTexture(e, target));
            material.SetPass(0);
            Graphics.DrawMeshNow(sphere, Matrix4x4.TRS(target.transform.position, Quaternion.identity, Vector3.one * capturePointPreviewSize));
        }

        static Func<float> s_CapturePointPreviewSizeGetter = ComputeCapturePointPreviewSizeGetter();
        static Func<float> ComputeCapturePointPreviewSizeGetter()
        {
            var type = Type.GetType("UnityEditor.AnnotationUtility,UnityEditor");
            var property = type.GetProperty("iconSize", BindingFlags.Static | BindingFlags.NonPublic);
            var lambda = Expression.Lambda<Func<float>>(
                Expression.Multiply(
                    Expression.Property(null, property),
                    Expression.Constant(30.0f)
                )
            );
            return lambda.Compile();
        }
        static float capturePointPreviewSize
        { get { return s_CapturePointPreviewSizeGetter(); } }
    }
}
