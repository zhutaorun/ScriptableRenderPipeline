using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class HDProbeUI
    {
        static List<HDProbe> s_DrawHandles_Target = new List<HDProbe>();
        internal static void DrawHandles(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            var probe = d.target;
            var mat = Matrix4x4.TRS(probe.transform.position, probe.transform.rotation, Vector3.one);

            switch (EditMode.editMode)
            {
                case EditBaseShape:
                    InfluenceVolumeUI.DrawHandles_EditBase(s.probeSettings.influence, d.probeSettings.influence, o, mat, probe);
                    break;
                case EditInfluenceShape:
                    InfluenceVolumeUI.DrawHandles_EditInfluence(s.probeSettings.influence, d.probeSettings.influence, o, mat, probe);
                    break;
                case EditInfluenceNormalShape:
                    InfluenceVolumeUI.DrawHandles_EditInfluenceNormal(s.probeSettings.influence, d.probeSettings.influence, o, mat, probe);
                    break;
                case EditCapturePosition:
                case EditMirrorPosition:
                    {
                        var proxyToWorldMatrix = probe.proxyToWorld;
                        // Set scale to 1
                        proxyToWorldMatrix.m00 = 1.0f;
                        proxyToWorldMatrix.m11 = 1.0f;

                        SerializedProperty target;
                        switch (EditMode.editMode)
                        {
                            case EditCapturePosition: target = d.probeSettings.proxyCapturePositionProxySpace; break;
                            case EditMirrorPosition: target = d.probeSettings.proxyMirrorPositionProxySpace; break;
                            default: throw new ArgumentOutOfRangeException();
                        }

                        using (new Handles.DrawingScope(proxyToWorldMatrix))
                        {
                            var position = target.vector3Value;
                            EditorGUI.BeginChangeCheck();
                            position = Handles.PositionHandle(position, Quaternion.identity);
                            if (EditorGUI.EndChangeCheck())
                                target.vector3Value = position;
                        }
                        break;
                    }
                case EditMirrorRotation:
                    {
                        var proxyToWorldMatrix = probe.proxyToWorld;
                        // Set scale to 1
                        proxyToWorldMatrix.m00 = 1.0f;
                        proxyToWorldMatrix.m11 = 1.0f;

                        var target = d.probeSettings.proxyMirrorRotationProxySpace;
                        var position = d.probeSettings.proxyMirrorPositionProxySpace.vector3Value;

                        using (new Handles.DrawingScope(proxyToWorldMatrix))
                        {
                            var rotation = target.quaternionValue;
                            EditorGUI.BeginChangeCheck();
                            rotation = Handles.RotationHandle(rotation, position);
                            if (EditorGUI.EndChangeCheck())
                                target.quaternionValue = rotation;
                        }
                        break;
                    }
            }
        }
    }
}
