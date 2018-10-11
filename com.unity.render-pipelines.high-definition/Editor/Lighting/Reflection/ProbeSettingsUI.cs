using UnityEngine.Events;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{

    sealed internal partial class ProbeSettingsUI : BaseUI<SerializedProbeSettings>
    {
        public InfluenceVolumeUI influence = new InfluenceVolumeUI();
        public FrameSettingsUI cameraFrameSettings = new FrameSettingsUI();

        public ProbeSettingsUI() : base(0)
        {
        }

        public override void Reset(SerializedProbeSettings probeSettings, UnityAction repaint)
        {
            base.Reset(probeSettings, repaint);
            influence.Reset(probeSettings.influence, repaint);
            cameraFrameSettings.Reset(probeSettings.cameraSettings.frameSettings, repaint);
        }

        public override void Update()
        {
            //bool frameSettingsOverriden = data.captureSettings.renderingPath.enumValueIndex == (int)HDAdditionalCameraData.RenderingPath.Custom;
            //isFrameSettingsOverriden.value = frameSettingsOverriden;
            //if (frameSettingsOverriden)
            //    frameSettings.Update();

            influence.Update();
            base.Update();
        }
    }
}
