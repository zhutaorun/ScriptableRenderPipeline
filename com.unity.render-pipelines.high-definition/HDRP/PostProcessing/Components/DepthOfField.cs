using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public enum DepthOfFieldMode
    {
        Off,
        UsePhysicalCamera
    }

    [Serializable, VolumeComponentMenu("Post-processing/Depth Of Field")]
    public sealed class DepthOfField : VolumeComponent, IPostProcessComponent
    {
        public DepthOfFieldModeParameter mode = new DepthOfFieldModeParameter(DepthOfFieldMode.Off);

        public bool IsActive()
        {
            return mode != DepthOfFieldMode.Off;
        }
    }

    [Serializable]
    public sealed class DepthOfFieldModeParameter : VolumeParameter<DepthOfFieldMode> { public DepthOfFieldModeParameter(DepthOfFieldMode value, bool overriden = false) : base(value, overriden) { } }
}
