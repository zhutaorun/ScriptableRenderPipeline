using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    using RTHandle = RTHandleSystem.RTHandle;

    // Main class for all post-processing related features - only includes camera effects, no
    // lighting/surface effect like SSR/AO
    public sealed class PostProcessSystem
    {
        RenderPipelineResources m_Resources;
        bool m_FirstFrame = true;

        // Exposure data
        const int k_ExposureCurvePrecision = 128;
        Color[] m_ExposureCurveColorArray = new Color[k_ExposureCurvePrecision];
        int[] m_ExposureVariants = new int[4];

        Texture2D m_ExposureCurveTexture;
        RTHandle m_EmptyExposureTexture;

        // Chromatic aberration data
        Texture2D m_InternalSpectralLut;

        // Misc (re-usable)
        RTHandle m_TempTexture1024;
        RTHandle m_TempTexture32;

        // Uber feature map to workaround the lack of multi_compile in compute shaders
        readonly Dictionary<int, string> m_UberPostFeatureMap = new Dictionary<int, string>();

        public PostProcessSystem(HDRenderPipelineAsset hdAsset)
        {
            m_Resources = hdAsset.renderPipelineResources;

            // Feature maps
            // Must be kept in sync with variants defined in UberPost.compute
            PushUberFeature(UberPostFeatureFlags.None);
            PushUberFeature(UberPostFeatureFlags.ChromaticAberration);
            PushUberFeature(UberPostFeatureFlags.Vignette);
            PushUberFeature(UberPostFeatureFlags.ChromaticAberration | UberPostFeatureFlags.Vignette);

            // Setup a default exposure textures
            m_EmptyExposureTexture = RTHandles.Alloc(1, 1, colorFormat: RenderTextureFormat.RGHalf,
                sRGB: false, enableRandomWrite: true, name: "Empty EV100 Exposure"
            );

            var tempTex = new Texture2D(1, 1, TextureFormat.RGHalf, false, true);
            tempTex.SetPixel(0, 0, Color.clear);
            tempTex.Apply();
            Graphics.Blit(tempTex, m_EmptyExposureTexture);
            CoreUtils.Destroy(tempTex);
        }

        public void Cleanup()
        {
            RTHandles.Release(m_EmptyExposureTexture);
            m_EmptyExposureTexture = null;

            RTHandles.Release(m_TempTexture1024);
            m_TempTexture1024 = null;

            RTHandles.Release(m_TempTexture32);
            m_TempTexture32 = null;

            CoreUtils.Destroy(m_ExposureCurveTexture);
            m_ExposureCurveTexture = null;

            CoreUtils.Destroy(m_InternalSpectralLut);
            m_InternalSpectralLut = null;
        }

        public void BeginFrame(CommandBuffer cmd, HDCamera camera)
        {
            if (IsExposureFixed())
            {
                using (new ProfilingSample(cmd, "Fixed Exposure", CustomSamplerId.Exposure.GetSampler()))
                {
                    DoFixedExposure(cmd, camera);
                }
            }
            
            cmd.SetGlobalTexture(HDShaderIDs._ExposureTexture, GetExposureTexture(camera));
        }

        public void Render(CommandBuffer cmd, HDCamera camera, RTHandle colorBuffer, RTHandle lightingBuffer)
        {
            using (new ProfilingSample(cmd, "Post-processing", CustomSamplerId.PostProcessing.GetSampler()))
            {
                // Start with exposure - will be applied in the next frame
                if (!IsExposureFixed())
                {
                    using (new ProfilingSample(cmd, "Dynamic Exposure", CustomSamplerId.Exposure.GetSampler()))
                    {
                        DoDynamicExposure(cmd, camera, colorBuffer, lightingBuffer);
                    }
                }

                // Combined post-processing stack
                // Feature flags are passed to all effects and it's their reponsability to check if
                // they are used or not so they can set default values if needed
                var cs = m_Resources.uberPostCS;
                var featureFlags = GetUberFeatureFlags();

                if (featureFlags != UberPostFeatureFlags.None)
                {
                    using (new ProfilingSample(cmd, "Uber", CustomSamplerId.UberPost.GetSampler()))
                    {
                        int kernel = GetUberKernel(cs, featureFlags);

                        DoChromaticAberration(cmd, cs, kernel, featureFlags);
                        DoVignette(cmd, cs, kernel, featureFlags);
                        
                        // TODO: Review this and remove the temporary target & blit once the whole stack is done
                        int tempRemoveMe = Shader.PropertyToID("_TempTargetRemoveMe");
                        cmd.GetTemporaryRT(tempRemoveMe, camera.actualWidth, camera.actualHeight, 0, FilterMode.Bilinear, RenderTextureFormat.RGB111110Float);
                        cmd.Blit(colorBuffer, tempRemoveMe);
                        cmd.SetComputeVectorParam(cs, HDShaderIDs._TexelSize, new Vector4(camera.actualWidth, camera.actualHeight, 1f / camera.actualWidth, 1f / camera.actualHeight));
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, tempRemoveMe);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OuputTexture, colorBuffer);
                        cmd.DispatchCompute(cs, kernel, (camera.actualWidth + 9) / 8, (camera.actualHeight + 9) / 8, 1);
                        cmd.ReleaseTemporaryRT(tempRemoveMe);
                    }
                }
            }

            m_FirstFrame = false;
        }

        void PushUberFeature(UberPostFeatureFlags flags)
        {
            // Use an int for the key instead of the enum itself to avoid GC pressure due to the
            // lack of a default comparer
            int iflags = (int)flags;
            m_UberPostFeatureMap.Add(iflags, "KMain_Variant" + iflags);
        }

        int GetUberKernel(ComputeShader cs, UberPostFeatureFlags flags)
        {
            string kernelName;
            bool success = m_UberPostFeatureMap.TryGetValue((int)flags, out kernelName);
            Assert.IsTrue(success);
            return cs.FindKernel(kernelName);
        }

        // Grabs all active feature flags
        UberPostFeatureFlags GetUberFeatureFlags()
        {
            var flags = UberPostFeatureFlags.None;

            if (VolumeManager.instance.stack.GetComponent<ChromaticAberration>().IsActive())
                flags |= UberPostFeatureFlags.ChromaticAberration;

            if (VolumeManager.instance.stack.GetComponent<Vignette>().IsActive())
                flags |= UberPostFeatureFlags.Vignette;

            return flags;
        }

        #region Exposure

        public RTHandle GetExposureTexture(HDCamera camera)
        {
            // 1x1 pixel, holds the current exposure value in EV100 in the red channel
            // One frame delay + history RTs being flipped at the beginning of the frame means we
            // have to grab the exposure marked as "previous"
            var rt = camera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.Exposure);
            return rt ?? m_EmptyExposureTexture;
        }

        bool IsExposureFixed(CameraControls settings = null)
        {
            if (settings == null)
                settings = VolumeManager.instance.stack.GetComponent<CameraControls>();

            return settings.exposureMode == ExposureMode.Fixed
                || (settings.exposureMode == ExposureMode.UseCameraSettings && settings.cameraShootingMode == ShootingMode.Manual);
        }

        void DoFixedExposure(CommandBuffer cmd, HDCamera camera)
        {
            var cs = m_Resources.exposureCS;
            var settings = VolumeManager.instance.stack.GetComponent<CameraControls>();
            
            RTHandle prevExposure, nextExposure;
            GrabExposureHistoryTextures(camera, out prevExposure, out nextExposure);

            int kernel = 0;

            if (settings.exposureMode == ExposureMode.Fixed)
            {
                kernel = cs.FindKernel("KFixedExposure");
                cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams, new Vector4(settings.fixedExposure, 0f, 0f, 0f));
            }
            else if (settings.exposureMode == ExposureMode.UseCameraSettings && settings.cameraShootingMode == ShootingMode.Manual)
            {
                kernel = cs.FindKernel("KManualCameraExposure");
                cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams, new Vector4(settings.exposureCompensation, settings.lensAperture, settings.cameraShutterSpeed, settings.cameraIso));
            }

            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PreviousExposureTexture, prevExposure);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OuputTexture, prevExposure);
            cmd.DispatchCompute(cs, kernel, 1, 1, 1);
        }

        RTHandle ExposureHistoryAllocator(string id, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            // R: Exposure in EV100
            // G: Discard
            return rtHandleSystem.Alloc(1, 1, colorFormat: RenderTextureFormat.RGHalf,
                sRGB: false, enableRandomWrite: true, name: string.Format("EV100 Exposure ({0}) {1}", id, frameIndex)
            );
        }

        void GrabExposureHistoryTextures(HDCamera camera, out RTHandle previous, out RTHandle next)
        {
            // We rely on the RT history system that comes with HDCamera, but because it is swapped
            // at the beginning of the frame and exposure is applied with a one-frame delay it means
            // that 'current' and 'previous' are swapped
            next = camera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Exposure)
                ?? camera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.Exposure, ExposureHistoryAllocator);
            previous = camera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.Exposure);
        }

        void CheckAverageLuminanceTargets()
        {
            if (m_TempTexture1024 != null)
                return;
            
            // R: Exposure in EV100
            // G: Weight (used for metering modes when downscaling)
            const RenderTextureFormat kFormat = RenderTextureFormat.RGHalf;

            m_TempTexture1024 = RTHandles.Alloc(
                1024, 1024, colorFormat: kFormat, sRGB: false,
                enableRandomWrite: true, name: "Average Luminance Temp 1024"
            );
            m_TempTexture32 = RTHandles.Alloc(
                32, 32, colorFormat: kFormat, sRGB: false,
                enableRandomWrite: true, name: "Average Luminance Temp 32"
            );
        }

        void PrepareExposureCurveData(AnimationCurve curve, out float min, out float max)
        {
            if (m_ExposureCurveTexture == null)
            {
                m_ExposureCurveTexture = new Texture2D(k_ExposureCurvePrecision, 1, TextureFormat.RHalf, false, true)
                {
                    name = "Exposure Curve",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
            }

            var pixels = m_ExposureCurveColorArray;

            // Fail safe in case the curve is deleted / has 0 point
            if (curve == null || curve.length == 0)
            {
                min = 0f;
                max = 0f;

                for (int i = 0; i < k_ExposureCurvePrecision; i++)
                    pixels[i] = Color.clear;
            }
            else
            {
                min = curve[0].time;
                max = curve[curve.length - 1].time;
                float step = (max - min) / (k_ExposureCurvePrecision - 1f);

                for (int i = 0; i < k_ExposureCurvePrecision; i++)
                    pixels[i] = new Color(curve.Evaluate(min + step * i), 0f, 0f, 0f);
            }

            m_ExposureCurveTexture.SetPixels(pixels);
            m_ExposureCurveTexture.Apply();
        }

        void DoDynamicExposure(CommandBuffer cmd, HDCamera camera, RTHandle colorBuffer, RTHandle lightingBuffer)
        {
            var cs = m_Resources.exposureCS;
            var settings = VolumeManager.instance.stack.GetComponent<CameraControls>();

            RTHandle prevExposure, nextExposure;
            GrabExposureHistoryTextures(camera, out prevExposure, out nextExposure);

            CheckAverageLuminanceTargets();

            // Setup variants
            var adaptationMode = settings.adaptationMode.value;

            if (!Application.isPlaying || m_FirstFrame)
                adaptationMode = AdaptationMode.Fixed;
            
            m_ExposureVariants[0] = (int)settings.luminanceSource.value;
            m_ExposureVariants[1] = (int)settings.exposureMeteringMode.value;
            m_ExposureVariants[2] = (int)adaptationMode;
            m_ExposureVariants[3] = 0;

            // Pre-pass
            var sourceTex = settings.luminanceSource == LuminanceSource.LightingBuffer
                ? lightingBuffer
                : colorBuffer;

            int kernel = cs.FindKernel("KPrePass");
            cmd.SetComputeIntParams(cs, HDShaderIDs._Variants, m_ExposureVariants);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PreviousExposureTexture, prevExposure);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, sourceTex);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OuputTexture, m_TempTexture1024);
            cmd.DispatchCompute(cs, kernel, 1024 / 8, 1024 / 8, 1);

            // Reduction: 1st pass (1024 -> 32)
            kernel = cs.FindKernel("KReduction");
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PreviousExposureTexture, prevExposure);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ExposureCurveTexture, Texture2D.blackTexture);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, m_TempTexture1024);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OuputTexture, m_TempTexture32);
            cmd.DispatchCompute(cs, kernel, 32, 32, 1);

            // Reduction: 2nd pass (32 -> 1) + evaluate exposure
            if (settings.exposureMode == ExposureMode.Automatic)
            {
                cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams, new Vector4(settings.exposureCompensation, settings.exposureLimitMin, settings.exposureLimitMax, 0f));
                m_ExposureVariants[3] = 1;
            }
            else if (settings.exposureMode == ExposureMode.CurveMapping)
            {
                float min, max;
                PrepareExposureCurveData(settings.exposureCurveMap.value, out min, out max);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ExposureCurveTexture, m_ExposureCurveTexture);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams, new Vector4(settings.exposureCompensation, min, max, 0f));
                m_ExposureVariants[3] = 2;
            }

            cmd.SetComputeVectorParam(cs, HDShaderIDs._AdaptationParams, new Vector4(settings.adaptationSpeedDown, settings.adaptationSpeedUp, 0f, 0f));
            cmd.SetComputeIntParams(cs, HDShaderIDs._Variants, m_ExposureVariants);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PreviousExposureTexture, prevExposure);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, m_TempTexture32);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OuputTexture, nextExposure);
            cmd.DispatchCompute(cs, kernel, 1, 1, 1);
        }

        #endregion

        #region Chromatic Aberration

        void DoChromaticAberration(CommandBuffer cmd, ComputeShader cs, int kernel, UberPostFeatureFlags flags)
        {
            if ((flags & UberPostFeatureFlags.ChromaticAberration) != UberPostFeatureFlags.ChromaticAberration)
                return;
            
            var settings = VolumeManager.instance.stack.GetComponent<ChromaticAberration>();
            var spectralLut = settings.spectralLut.value;

            // If no spectral lut is set, use a pre-generated one
            if (spectralLut == null)
            {
                if (m_InternalSpectralLut == null)
                {
                    m_InternalSpectralLut = new Texture2D(3, 1, TextureFormat.RGB24, false)
                    {
                        name = "Chromatic Aberration Spectral LUT",
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp,
                        anisoLevel = 0,
                        hideFlags = HideFlags.DontSave
                    };

                    m_InternalSpectralLut.SetPixels(new []
                    {
                        new Color(1f, 0f, 0f),
                        new Color(0f, 1f, 0f),
                        new Color(0f, 0f, 1f)
                    });

                    m_InternalSpectralLut.Apply();
                }

                spectralLut = m_InternalSpectralLut;
            }

            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ChromaSpectralLut, spectralLut);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._ChromaParams, new Vector4(settings.intensity * 0.05f, settings.maxSamples, 0f, 0f));
        }

        #endregion

        #region Vignette

        void DoVignette(CommandBuffer cmd, ComputeShader cs, int kernel, UberPostFeatureFlags flags)
        {
            if ((flags & UberPostFeatureFlags.Vignette) != UberPostFeatureFlags.Vignette)
                return;

            var settings = VolumeManager.instance.stack.GetComponent<Vignette>();

            if (settings.mode.value == VignetteMode.Procedural)
            {
                float roundness = (1f - settings.roundness.value) * 6f + settings.roundness.value;
                cmd.SetComputeVectorParam(cs, HDShaderIDs._VignetteParams1, new Vector4(settings.center.value.x, settings.center.value.y, 0f, 0f));
                cmd.SetComputeVectorParam(cs, HDShaderIDs._VignetteParams2, new Vector4(settings.intensity.value * 3f, settings.smoothness.value * 5f, roundness, settings.rounded.value ? 1f : 0f));
                cmd.SetComputeVectorParam(cs, HDShaderIDs._VignetteColor, settings.color.value);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._VignetteMask, Texture2D.blackTexture);
            }
            else // Masked
            {
                var color = settings.color.value;
                color.a = Mathf.Clamp01(settings.opacity.value);

                cmd.SetComputeVectorParam(cs, HDShaderIDs._VignetteParams1, new Vector4(0f, 0f, 1f, 0f));
                cmd.SetComputeVectorParam(cs, HDShaderIDs._VignetteColor, color);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._VignetteMask, settings.mask.value);
            }
        }

        #endregion
    }
}
