using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public struct ProbeSettings
    {
        public enum ProbeType
        {
            ReflectionProbe,
            PlanarProbe
        }

        public enum Mode
        {
            Baked,
            Custom,
            Realtime
        }

        public struct Lighting
        {
            public float multiplier;
            public float weight;
        }

        public ProbeType type;
        public Mode mode;
        public Lighting lighting;
        public InfluenceVolume influence;
        public ProxyVolume proxy;
        public CameraSettings camera;
    }

    [Serializable]
    public struct ProbeRenderSettings
    {
        [Serializable]
        public struct CapturePosition
        {
            public enum Mode
            {
                UseProbeTransformFields,
                MirrorReferencePosition
            }

            public Mode mode;
            public Vector3 probePosition;
            public Quaternion probeRotation;
            public Vector3 referencePosition;
            public Quaternion referenceRotation;
        }

        public ProbeSettings probe;
        public CapturePosition position;
    }

    public struct HDProbeRenderer
    {
        HDCameraRenderer renderer;

        public void Render(ProbeRenderSettings settings, Texture target)
        {
            var renderSettings = new CameraRenderSettings
            {
                camera = settings.probe.camera
            };

            settings.UpdateSettings(ref renderSettings);

            renderer.Render(renderSettings, target);
        }
    }

    public static class ProbeRenderSettingsUtilities
    {
        public static void UpdateSettings(
            this ref ProbeRenderSettings settings,
            ref CameraRenderSettings renderSettings
        )
        {
            throw new NotImplementedException();
        }
    }
}
