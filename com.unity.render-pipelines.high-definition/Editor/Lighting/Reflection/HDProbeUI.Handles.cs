using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class HDProbeUI
    {

        internal static void DrawHandles(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            HDProbe probe = d.target as HDProbe;
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
                case EditCenter:
                    {
                        using (new Handles.DrawingScope(Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one)))
                        {
                            Vector3 offsetWorld = probe.transform.position + probe.transform.rotation * probe.influenceVolume.offset;
                            EditorGUI.BeginChangeCheck();
                            var newOffsetWorld = Handles.PositionHandle(offsetWorld, probe.transform.rotation);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Vector3 newOffset = Quaternion.Inverse(probe.transform.rotation) * (newOffsetWorld - probe.transform.position);
                                Undo.RecordObjects(new Object[] { probe, probe.transform }, "Translate Influence Position");
                                d.probeSettings.influence.offset.vector3Value = newOffset;
                                d.probeSettings.influence.Apply();

                                //call modification to legacy ReflectionProbe
                                probe.influenceVolume.offset = newOffset;
                            }
                        }
                        break;
                    }
            }
        }
    }
}
