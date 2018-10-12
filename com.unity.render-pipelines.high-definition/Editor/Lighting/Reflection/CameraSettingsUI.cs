using UnityEngine.Experimental.Rendering.HDPipeline;

using static UnityEditor.Experimental.Rendering.HDPipeline.HDEditorUtils;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;

    internal partial class CameraSettingsUI : IUpdateable<SerializedCameraSettings>
    {
        public FrameSettingsUI frameSettings = new FrameSettingsUI();

        public void Update(SerializedCameraSettings s)
        {
            frameSettings.Reset(s.frameSettings, null);
        }
    }
}
