using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public sealed class ChromaticAberration : VolumeComponent
    {
        [Tooltip("Shifts the hue of chromatic aberrations.")]
        public TextureParameter spectralLut = new TextureParameter(null);

        [Tooltip("Amount of tangential distortion.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

        [Tooltip("Maximum amount of samples used to render the effect. Lower count means better performance.")]
        public ClampedIntParameter maxSamples = new ClampedIntParameter(8, 3, 16);

        public bool IsActive()
        {
            return intensity > 0f;
        }
    }
}
