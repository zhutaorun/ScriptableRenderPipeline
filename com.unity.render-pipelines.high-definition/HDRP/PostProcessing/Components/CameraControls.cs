using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public sealed class CameraControls : VolumeComponent, IPostProcessComponent
    {
        // Camera & lens settings
        [Header("Camera & Lens")]
        public ShootingModeParameter cameraShootingMode = new ShootingModeParameter(ShootingMode.Manual);
        public MinIntParameter cameraIso = new MinIntParameter(200, 1);
        public MinFloatParameter cameraShutterSpeed = new MinFloatParameter(1f / 200f, 0f);
        public MinFloatParameter lensAperture = new MinFloatParameter(16f, 0.7f);

        // Exposure settings
        [Header("Exposure")]
        public MeteringModeParameter exposureMeteringMode = new MeteringModeParameter(MeteringMode.CenterWeighted);
        public ExposureModeParameter exposureMode = new ExposureModeParameter(ExposureMode.Automatic);
        public FloatParameter fixedExposure = new FloatParameter(0f);
        public FloatParameter exposureCompensation = new FloatParameter(0f);
        public FloatParameter exposureLimitMin = new FloatParameter(-10f);
        public FloatParameter exposureLimitMax = new FloatParameter(20f);
        //public MinFloatParameter absoluteExposureClamp = new MinFloatParameter(65472f, 0f);
        public LuminanceSourceParameter luminanceSource = new LuminanceSourceParameter(LuminanceSource.ColorBuffer);
        public AnimationCurveParameter exposureCurveMap = new AnimationCurveParameter(AnimationCurve.Linear(-10f, -10f, 20f, 20f));
        public AdaptationModeParameter adaptationMode = new AdaptationModeParameter(AdaptationMode.Progressive);
        public MinFloatParameter adaptationSpeedUp = new MinFloatParameter(3f, 0.001f);
        public MinFloatParameter adaptationSpeedDown = new MinFloatParameter(1f, 0.001f);

        public bool IsActive()
        {
            return true;
        }
    }

    public enum ShootingMode
    {
        Manual,
        //Automatic,
        //AutomaticISO,
        //AperturePriority,
        //ShutterPriority
    }

    public enum ExposureMode
    {
        Fixed,
        Automatic,
        UseCameraSettings,
        CurveMapping
    }

    public enum AdaptationMode
    {
        Fixed,
        Progressive
    }

    public enum LuminanceSource
    {
        LightingBuffer,
        ColorBuffer
    }

    public enum MeteringMode
    {
        Average,
        Spot,
        CenterWeighted,
        Tracking
    }

    [Serializable]
    public sealed class ShootingModeParameter : VolumeParameter<ShootingMode> { public ShootingModeParameter(ShootingMode value, bool overriden = false) : base(value, overriden) {} }

    [Serializable]
    public sealed class ExposureModeParameter : VolumeParameter<ExposureMode> { public ExposureModeParameter(ExposureMode value, bool overriden = false) : base(value, overriden) {} }

    [Serializable]
    public sealed class AdaptationModeParameter : VolumeParameter<AdaptationMode> { public AdaptationModeParameter(AdaptationMode value, bool overriden = false) : base(value, overriden) {} }
    
    [Serializable]
    public sealed class LuminanceSourceParameter : VolumeParameter<LuminanceSource> { public LuminanceSourceParameter(LuminanceSource value, bool overriden = false) : base(value, overriden) {} }

    [Serializable]
    public sealed class MeteringModeParameter : VolumeParameter<MeteringMode> { public MeteringModeParameter(MeteringMode value, bool overriden = false) : base(value, overriden) { } }
}
