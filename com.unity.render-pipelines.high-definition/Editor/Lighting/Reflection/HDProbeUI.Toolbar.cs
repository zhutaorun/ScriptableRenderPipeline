using UnityEditorInternal;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class HDProbeUI
    {
        internal static bool IsProbeEditMode(EditMode.SceneViewEditMode editMode)
        {
            return editMode == EditBaseShape
                || editMode == EditInfluenceShape
                || editMode == EditInfluenceNormalShape
                || editMode == EditCapturePosition;
        }
    }
}
