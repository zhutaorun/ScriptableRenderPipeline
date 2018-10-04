using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public sealed class PhysicalCamera : VolumeComponent, IPostProcessComponent
    {
        [Header("Camera Body")]
        public MinIntParameter iso = new MinIntParameter(200, 1);
        public MinFloatParameter shutterSpeed = new MinFloatParameter(1f / 200f, 0f);

        [Header("Lens")]
        public MinFloatParameter aperture = new MinFloatParameter(16f, 1f);
        public MinFloatParameter focalLength = new MinFloatParameter(50f, 1f);

        public bool IsActive()
        {
            return true;
        }
    }
}
