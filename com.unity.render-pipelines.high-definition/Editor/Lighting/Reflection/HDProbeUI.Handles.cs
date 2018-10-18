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
                    {
                        var proxyToWorldMatrix = probe.proxyToWorld;
                        using (new Handles.DrawingScope(proxyToWorldMatrix))
                        {
                            var capturePosition = d.probeSettings.proxyCapturePositionProxySpace.vector3Value;
                            EditorGUI.BeginChangeCheck();
                            capturePosition = Handles.PositionHandle(capturePosition, Quaternion.identity);
                            if (EditorGUI.EndChangeCheck())
                                d.probeSettings.proxyCapturePositionProxySpace.vector3Value = capturePosition;
                        }
                        break;
                    }
            }
        }
    }
}
