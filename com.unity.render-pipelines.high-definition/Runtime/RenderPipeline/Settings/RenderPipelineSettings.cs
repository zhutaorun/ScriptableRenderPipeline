using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // RenderPipelineSettings define settings that can't be change during runtime. It is equivalent to the GraphicsSettings of Unity (Tiers + shader variant removal).
    // This allow to allocate resource or not for a given feature.
    // FrameSettings control within a frame what is enable or not(enableShadow, enableStereo, enableDistortion...).
    // HDRenderPipelineAsset reference the current RenderPipelineSettings used, there is one per supported platform(Currently this feature is not implemented and only one GlobalFrameSettings is available).
    // A Camera with HDAdditionalData has one FrameSettings that configures how it will render. For example a camera used for reflection will disable distortion and post-process.
    // Additionally, on a Camera there is another FrameSettings called ActiveFrameSettings that is created on the fly based on FrameSettings and allows modifications for debugging purpose at runtime without being serialized on disk.
    // The ActiveFrameSettings is registered in the debug windows at the creation of the camera.
    // A Camera with HDAdditionalData has a RenderPath that defines if it uses a "Default" FrameSettings, a preset of FrameSettings or a custom one.
    // HDRenderPipelineAsset contains a "Default" FrameSettings that can be referenced by any camera with RenderPath.Defaut or when the camera doesn't have HDAdditionalData like the camera of the Editor.
    // It also contains a DefaultActiveFrameSettings

    enum LitShaderMode
    {
        Deferred,
        Forward,
        Both
    }

    // RenderPipelineSettings represents settings that are immutable at runtime.
    // There is a dedicated RenderPipelineSettings for each platform
    [Serializable]
    public class RenderPipelineSettings
    {
        [HideInInspector]
        const int currentVersion = 1;
        // Currently m_Version is not used and produce a warning, remove these pragmas at the next version incrementation
#pragma warning disable 414
        [SerializeField]
        int m_Version = currentVersion;
#pragma warning restore 414

        // Lighting
        public bool supportShadowMask = true;
        public bool supportSSR = false;
        public bool supportSSAO = true;
        public bool supportSubsurfaceScattering = true;
        public bool increaseSssSampleCount = false;
        [FormerlySerializedAs("supportForwardOnly")]
        public bool supportVolumetrics = true;
        public bool increaseResolutionOfVolumetrics = false;
        public bool supportLightLayers = false;
        public LitShaderMode litShaderMode = LitShaderMode.Deferred;

        // Engine
        [FormerlySerializedAs("supportDBuffer")]
        public bool supportDecals = true;
        public bool supportMSAA = false;
        public MSAASamples  msaaSampleCount = MSAASamples.None;
        public bool supportMotionVectors = true;
        public bool supportRuntimeDebugDisplay = true;
        public bool supportDitheringCrossFade = true;
        
        public GlobalLightLoopSettings  lightLoopSettings = new GlobalLightLoopSettings();
        public HDShadowInitParameters   hdShadowInitParams = new HDShadowInitParameters();
        public GlobalDecalSettings      decalSettings = new GlobalDecalSettings();
    }
}
