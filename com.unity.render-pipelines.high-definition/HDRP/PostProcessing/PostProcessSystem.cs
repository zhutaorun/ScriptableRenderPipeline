using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    using RTHandle = RTHandleSystem.RTHandle;
    using AntialiasingMode = HDAdditionalCameraData.AntialiasingMode;

    // Main class for all post-processing related features - only includes camera effects, no
    // lighting/surface effect like SSR/AO
    public sealed class PostProcessSystem
    {
        RenderPipelineResources m_Resources;
        bool m_ResetHistory;

        // Exposure data
        const int k_ExposureCurvePrecision = 128;
        Color[] m_ExposureCurveColorArray = new Color[k_ExposureCurvePrecision];
        int[] m_ExposureVariants = new int[4];

        Texture2D m_ExposureCurveTexture;
        RTHandle m_EmptyExposureTexture; // RGHalf

        // Chromatic aberration data
        Texture2D m_InternalSpectralLut;

        // Color grading data
        const int k_LogLutSize = 33;
        RTHandle m_InternalLogLut; // ARGBHalf
        readonly HableCurve m_HableCurve;

        // Prefetched components (updated on every frame)
        PhysicalCamera m_PhysicalCamera;
        Exposure m_Exposure;
        DepthOfField m_DepthOfField;
        ChromaticAberration m_ChromaticAberration;
        LensDistortion m_LensDistortion;
        Vignette m_Vignette;
        Tonemapping m_Tonemapping;
        WhiteBalance m_WhiteBalance;
        ColorAdjustments m_ColorAdjustments;
        ChannelMixer m_ChannelMixer;
        SplitToning m_SplitToning;
        LiftGammaGain m_LiftGammaGain;
        ShadowsMidtonesHighlights m_ShadowsMidtonesHighlights;

        // Misc (re-usable)
        RTHandle[] m_TempFullSizePingPong = new RTHandle[2]; // R11G11B10F
        int m_CurrentTemporaryPingPong;

        RTHandle m_HalfResTexture;  // R11G11B10
        RTHandle m_TempTexture1024; // RGHalf
        RTHandle m_TempTexture32;   // RGHalf

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
            PushUberFeature(UberPostFeatureFlags.LensDistortion);
            PushUberFeature(UberPostFeatureFlags.ChromaticAberration | UberPostFeatureFlags.Vignette);
            PushUberFeature(UberPostFeatureFlags.ChromaticAberration | UberPostFeatureFlags.LensDistortion);
            PushUberFeature(UberPostFeatureFlags.Vignette | UberPostFeatureFlags.LensDistortion);
            PushUberFeature(UberPostFeatureFlags.ChromaticAberration | UberPostFeatureFlags.Vignette | UberPostFeatureFlags.LensDistortion);

            // Grading specific
            m_HableCurve = new HableCurve();
            m_InternalLogLut = RTHandles.Alloc(
                name: "Color Grading Log Lut",
                dimension: TextureDimension.Tex3D,
                width: k_LogLutSize,
                height: k_LogLutSize,
                slices: k_LogLutSize,
                depthBufferBits: DepthBits.None,
                colorFormat: RenderTextureFormat.ARGBHalf,
                filterMode: FilterMode.Bilinear,
                wrapMode: TextureWrapMode.Clamp,
                anisoLevel: 0,
                useMipMap: false,
                enableRandomWrite: true,
                sRGB: false
            );

            // Setup a default exposure textures
            m_EmptyExposureTexture = RTHandles.Alloc(1, 1, colorFormat: RenderTextureFormat.RGHalf,
                sRGB: false, enableRandomWrite: true, name: "Empty EV100 Exposure"
            );

            var tempTex = new Texture2D(1, 1, TextureFormat.RGHalf, false, true);
            tempTex.SetPixel(0, 0, Color.clear);
            tempTex.Apply();
            Graphics.Blit(tempTex, m_EmptyExposureTexture);
            CoreUtils.Destroy(tempTex);

            // Ping pong targets
            for (int i = 0; i < 2; i++)
            {
                m_TempFullSizePingPong[i] = RTHandles.Alloc(
                    Vector2.one, depthBufferBits: DepthBits.None,
                    filterMode: FilterMode.Bilinear, colorFormat: RenderTextureFormat.RGB111110Float,
                    enableRandomWrite: true, name: "Post-processing PingPong Target " + i
                );
            }

            // Misc targets
            m_HalfResTexture = RTHandles.Alloc(
                new Vector2(0.5f, 0.5f), colorFormat: RenderTextureFormat.RGB111110Float, sRGB: false,
                enableRandomWrite: true, name: "Post-processing Half-res Temp"
            );

            m_TempTexture1024 = RTHandles.Alloc(
                1024, 1024, colorFormat: RenderTextureFormat.RGHalf, sRGB: false,
                enableRandomWrite: true, name: "Average Luminance Temp 1024"
            );

            m_TempTexture32 = RTHandles.Alloc(
                32, 32, colorFormat: RenderTextureFormat.RGHalf, sRGB: false,
                enableRandomWrite: true, name: "Average Luminance Temp 32"
            );

            m_ResetHistory = true;
        }

        public void Cleanup()
        {
            RTHandles.Release(m_EmptyExposureTexture);
            RTHandles.Release(m_TempFullSizePingPong[0]);
            RTHandles.Release(m_TempFullSizePingPong[1]);
            RTHandles.Release(m_HalfResTexture);
            RTHandles.Release(m_TempTexture1024);
            RTHandles.Release(m_TempTexture32);
            CoreUtils.Destroy(m_ExposureCurveTexture);
            CoreUtils.Destroy(m_InternalSpectralLut);
            RTHandles.Release(m_InternalLogLut);

            m_EmptyExposureTexture    = null;
            m_TempFullSizePingPong[0] = null;
            m_TempFullSizePingPong[1] = null;
            m_HalfResTexture          = null;
            m_TempTexture1024         = null;
            m_TempTexture32           = null;
            m_ExposureCurveTexture    = null;
            m_InternalSpectralLut     = null;
            m_InternalLogLut          = null;
        }

        public void ResetHistory()
        {
            m_ResetHistory = true;
        }

        public void BeginFrame(CommandBuffer cmd, HDCamera camera)
        {
            // Prefetch all the volume components we need to save some cycles as most of these will
            // be needed in multiple places
            var stack = VolumeManager.instance.stack;
            m_PhysicalCamera            = stack.GetComponent<PhysicalCamera>();
            m_Exposure                  = stack.GetComponent<Exposure>();
            m_DepthOfField              = stack.GetComponent<DepthOfField>();
            m_ChromaticAberration       = stack.GetComponent<ChromaticAberration>();
            m_LensDistortion            = stack.GetComponent<LensDistortion>();
            m_Vignette                  = stack.GetComponent<Vignette>();
            m_Tonemapping               = stack.GetComponent<Tonemapping>();
            m_WhiteBalance              = stack.GetComponent<WhiteBalance>();
            m_ColorAdjustments          = stack.GetComponent<ColorAdjustments>();
            m_ChannelMixer              = stack.GetComponent<ChannelMixer>();
            m_SplitToning               = stack.GetComponent<SplitToning>();
            m_LiftGammaGain             = stack.GetComponent<LiftGammaGain>();
            m_ShadowsMidtonesHighlights = stack.GetComponent<ShadowsMidtonesHighlights>();

            // Check if motion vectors are needed, if so we need to enable a flag on the camera so
            // that Unity properly generate motion vectors (internal engine dependency)
            // TODO: Check for motion blur as well
            if (camera.antialiasing == AntialiasingMode.TemporalAntialiasing)
            {
                camera.camera.depthTextureMode |= DepthTextureMode.MotionVectors;
            }

            // Handle fixed exposure
            if (IsExposureFixed())
            {
                using (new ProfilingSample(cmd, "Fixed Exposure", CustomSamplerId.Exposure.GetSampler()))
                {
                    DoFixedExposure(cmd, camera);
                }
            }
            
            cmd.SetGlobalTexture(HDShaderIDs._ExposureTexture, GetExposureTexture(camera));
        }

        public void Render(CommandBuffer cmd, HDCamera camera, RTHandle colorBuffer, RTHandle lightingBuffer, RTHandle depthBuffer, RTHandle velocityBuffer)
        {
            using (new ProfilingSample(cmd, "Post-processing", CustomSamplerId.PostProcessing.GetSampler()))
            {
                var source = colorBuffer;

                // TODO: Do we want user effects before post?

                // Start with exposure - will be applied in the next frame
                if (!IsExposureFixed())
                {
                    using (new ProfilingSample(cmd, "Dynamic Exposure", CustomSamplerId.Exposure.GetSampler()))
                    {
                        DoDynamicExposure(cmd, camera, colorBuffer, lightingBuffer);
                    }
                }

                // Temporal anti-aliasing goes first
                bool taaEnabled = camera.antialiasing == AntialiasingMode.TemporalAntialiasing
                    && camera.camera.cameraType == CameraType.Game;

                if (taaEnabled)
                {
                    using (new ProfilingSample(cmd, "Temporal Anti-aliasing", CustomSamplerId.TemporalAntialiasing.GetSampler()))
                    {
                        DoTemporalAntialiasing(cmd, camera, source, PingPongTarget(), depthBuffer, velocityBuffer);
                        source = GetLastPingPongTarget();
                    }
                }

                // Depth of Field is done right after TAA as it's easier to just reproject the CoC
                // map rather than having to deal with all the implications of doing it before TAA
                if (m_DepthOfField.mode.value != DepthOfFieldMode.Off)
                {
                    using (new ProfilingSample(cmd, "Depth of Field", CustomSamplerId.DepthOfField.GetSampler()))
                    {
                        DoDepthOfField(cmd, camera, source, PingPongTarget(), depthBuffer, taaEnabled ? velocityBuffer : null);
                        source = GetLastPingPongTarget();
                    }
                }

                // TODO: Motion blur goes here

                // Combined post-processing stack - always runs if postfx is enabled
                using (new ProfilingSample(cmd, "Uber", CustomSamplerId.UberPost.GetSampler()))
                {
                    // Feature flags are passed to all effects and it's their reponsability to check
                    // if they are used or not so they can set default values if needed
                    var cs = m_Resources.uberPostCS;
                    var featureFlags = GetUberFeatureFlags();
                    int kernel = GetUberKernel(cs, featureFlags);

                    // Build the color grading lut
                    using (new ProfilingSample(cmd, "Color Grading LUT Builder", CustomSamplerId.ColorGradingLUTBuilder.GetSampler()))
                    {
                        DoColorGrading(cmd, cs, kernel);
                    }

                    // Setup the rest of the effects
                    DoLensDistortion(cmd, cs, kernel, featureFlags);
                    DoChromaticAberration(cmd, cs, kernel, featureFlags);
                    DoVignette(cmd, cs, kernel, featureFlags);

                    // Run
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, source);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, PingPongTarget());
                    cmd.DispatchCompute(cs, kernel, (camera.actualWidth + 7) / 8, (camera.actualHeight + 7) / 8, 1);

                    source = GetLastPingPongTarget();
                }

                // TODO: User effects go here

                // Final pass
                // TODO: this pass should be the one writing to the backbuffer and do all the remaining stuff
                using (new ProfilingSample(cmd, "Final Pass", CustomSamplerId.FinalPost.GetSampler()))
                {
                    DoFinalPass(cmd, camera, source, colorBuffer);
                }
            }

            m_ResetHistory = false;
        }

        RTHandle PingPongTarget()
        {
            m_CurrentTemporaryPingPong = (m_CurrentTemporaryPingPong + 1) % 2;
            return m_TempFullSizePingPong[m_CurrentTemporaryPingPong];
        }

        RTHandle GetLastPingPongTarget()
        {
            return m_TempFullSizePingPong[m_CurrentTemporaryPingPong];
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

            if (m_ChromaticAberration.IsActive())
                flags |= UberPostFeatureFlags.ChromaticAberration;

            if (m_Vignette.IsActive())
                flags |= UberPostFeatureFlags.Vignette;

            if (m_LensDistortion.IsActive())
                flags |= UberPostFeatureFlags.LensDistortion;

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

        bool IsExposureFixed()
        {
            return m_Exposure.mode == ExposureMode.Fixed
                || m_Exposure.mode == ExposureMode.UsePhysicalCamera;
        }

        void DoFixedExposure(CommandBuffer cmd, HDCamera camera)
        {
            var cs = m_Resources.exposureCS;
            
            RTHandle prevExposure, nextExposure;
            GrabExposureHistoryTextures(camera, out prevExposure, out nextExposure);

            int kernel = 0;

            if (m_Exposure.mode == ExposureMode.Fixed)
            {
                kernel = cs.FindKernel("KFixedExposure");
                cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams, new Vector4(m_Exposure.fixedExposure, 0f, 0f, 0f));
            }
            else if (m_Exposure.mode == ExposureMode.UsePhysicalCamera)
            {
                kernel = cs.FindKernel("KManualCameraExposure");
                cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams, new Vector4(m_Exposure.compensation, m_PhysicalCamera.aperture, m_PhysicalCamera.shutterSpeed, m_PhysicalCamera.iso));
            }

            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PreviousExposureTexture, prevExposure);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, prevExposure);
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

        // TODO: Handle light buffer as a source for average luminance
        void DoDynamicExposure(CommandBuffer cmd, HDCamera camera, RTHandle colorBuffer, RTHandle lightingBuffer)
        {
            var cs = m_Resources.exposureCS;

            RTHandle prevExposure, nextExposure;
            GrabExposureHistoryTextures(camera, out prevExposure, out nextExposure);

            // Setup variants
            var adaptationMode = m_Exposure.adaptationMode.value;

            if (!Application.isPlaying || m_ResetHistory)
                adaptationMode = AdaptationMode.Fixed;

            m_ExposureVariants[0] = 1; // (int)exposureSettings.luminanceSource.value;
            m_ExposureVariants[1] = (int)m_Exposure.meteringMode.value;
            m_ExposureVariants[2] = (int)adaptationMode;
            m_ExposureVariants[3] = 0;

            // Pre-pass
            //var sourceTex = exposureSettings.luminanceSource == LuminanceSource.LightingBuffer
            //    ? lightingBuffer
            //    : colorBuffer;
            var sourceTex = colorBuffer;

            int kernel = cs.FindKernel("KPrePass");
            cmd.SetComputeIntParams(cs, HDShaderIDs._Variants, m_ExposureVariants);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PreviousExposureTexture, prevExposure);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, sourceTex);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, m_TempTexture1024);
            cmd.DispatchCompute(cs, kernel, 1024 / 8, 1024 / 8, 1);

            // Reduction: 1st pass (1024 -> 32)
            kernel = cs.FindKernel("KReduction");
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PreviousExposureTexture, prevExposure);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ExposureCurveTexture, Texture2D.blackTexture);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, m_TempTexture1024);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, m_TempTexture32);
            cmd.DispatchCompute(cs, kernel, 32, 32, 1);

            // Reduction: 2nd pass (32 -> 1) + evaluate exposure
            if (m_Exposure.mode == ExposureMode.Automatic)
            {
                cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams, new Vector4(m_Exposure.compensation, m_Exposure.limitMin, m_Exposure.limitMax, 0f));
                m_ExposureVariants[3] = 1;
            }
            else if (m_Exposure.mode == ExposureMode.CurveMapping)
            {
                float min, max;
                PrepareExposureCurveData(m_Exposure.curveMap.value, out min, out max);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ExposureCurveTexture, m_ExposureCurveTexture);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams, new Vector4(m_Exposure.compensation, min, max, 0f));
                m_ExposureVariants[3] = 2;
            }

            cmd.SetComputeVectorParam(cs, HDShaderIDs._AdaptationParams, new Vector4(m_Exposure.adaptationSpeedLightToDark, m_Exposure.adaptationSpeedDarkToLight, 0f, 0f));
            cmd.SetComputeIntParams(cs, HDShaderIDs._Variants, m_ExposureVariants);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PreviousExposureTexture, prevExposure);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, m_TempTexture32);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, nextExposure);
            cmd.DispatchCompute(cs, kernel, 1, 1, 1);
        }

        #endregion

        #region Temporal Anti-aliasing

        void DoTemporalAntialiasing(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination, RTHandle depthBuffer, RTHandle velocityBuffer)
        {
            var cs = m_Resources.taaCS;
            var kernel = cs.FindKernel(camera.camera.orthographic ? "KTAA_Ortho" : "KTAA_Persp");

            RTHandle prevHistory, nextHistory;
            GrabTemporalAntialiasingHistoryTextures(camera, out prevHistory, out nextHistory);

            if (m_ResetHistory)
            {
                CopyTemporalAntialiasingHistory(cmd, camera, source, prevHistory);
                CopyTemporalAntialiasingHistory(cmd, camera, source, nextHistory);
            }

            var historyScale = new Vector2(camera.actualWidth / (float)prevHistory.rt.width, camera.actualHeight / (float)prevHistory.rt.height);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._ScreenToTargetScaleHistory, historyScale);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, source);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputHistoryTexture, prevHistory);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._DepthTexture, depthBuffer);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._VelocityTexture, velocityBuffer);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputHistoryTexture, nextHistory);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, destination);
            cmd.DispatchCompute(cs, kernel, (camera.actualWidth + 7) / 8, (camera.actualHeight + 7) / 8, 1);
        }

        void GrabTemporalAntialiasingHistoryTextures(HDCamera camera, out RTHandle previous, out RTHandle next)
        {
            next = camera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.TemporalAntialiasing)
                ?? camera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.TemporalAntialiasing, TemporalAntialiasingHistoryAllocator);
            previous = camera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.TemporalAntialiasing);
        }

        RTHandle TemporalAntialiasingHistoryAllocator(string id, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(
                Vector2.one, depthBufferBits: DepthBits.None,
                filterMode: FilterMode.Bilinear, colorFormat: RenderTextureFormat.RGB111110Float,
                enableRandomWrite: true, name: "TAA History"
            );
        }

        void CopyTemporalAntialiasingHistory(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle history)
        {
            var cs = m_Resources.taaCS;
            var kernel = cs.FindKernel("KCopyHistory");

            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, source);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, history);
            cmd.DispatchCompute(cs, kernel, (camera.actualWidth + 7) / 8, (camera.actualHeight + 7) / 8, 1);
        }

        #endregion

        #region Depth Of Field

        void DoDepthOfField(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination, RTHandle depthBuffer, RTHandle velocityBuffer)
        {
            bool taaEnabled = velocityBuffer == null;
            RTHandle prevCoCHistory, nextCoCHistory;
            GrabDepthOfFieldCoCHistoryTextures(camera, out prevCoCHistory, out nextCoCHistory);

            if (m_ResetHistory)
            {
                // TODO: Handle DoF history reset
            }

            // Pass 1: downsample color & generate CoC

        }

        void GrabDepthOfFieldCoCHistoryTextures(HDCamera camera, out RTHandle previous, out RTHandle next)
        {
            next = camera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.DepthOfFieldCoC)
                ?? camera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.DepthOfFieldCoC, DepthOfFieldCoCHistoryAllocator);
            previous = camera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.DepthOfFieldCoC);
        }

        RTHandle DepthOfFieldCoCHistoryAllocator(string id, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(
                Vector3.one, depthBufferBits: DepthBits.None,
                filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.RHalf,
                enableRandomWrite: true, name: "CoC History"
            );
        }

        #endregion

        #region Lens Distortion

        void DoLensDistortion(CommandBuffer cmd, ComputeShader cs, int kernel, UberPostFeatureFlags flags)
        {
            if ((flags & UberPostFeatureFlags.LensDistortion) != UberPostFeatureFlags.LensDistortion)
                return;

            float amount = 1.6f * Mathf.Max(Mathf.Abs(m_LensDistortion.intensity.value * 100f), 1f);
            float theta = Mathf.Deg2Rad * Mathf.Min(160f, amount);
            float sigma = 2f * Mathf.Tan(theta * 0.5f);
            var center = m_LensDistortion.center.value * 2f - Vector2.one;
            var p1 = new Vector4(
                center.x,
                center.y,
                Mathf.Max(m_LensDistortion.xMultiplier.value, 1e-4f),
                Mathf.Max(m_LensDistortion.yMultiplier.value, 1e-4f)
            );
            var p2 = new Vector4(
                m_LensDistortion.intensity.value >= 0f ? theta : 1f / theta,
                sigma,
                1f / m_LensDistortion.scale.value,
                m_LensDistortion.intensity.value * 100f
            );
            
            cmd.SetComputeVectorParam(cs, HDShaderIDs._DistortionParams1, p1);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._DistortionParams2, p2);
        }

        #endregion

        #region Chromatic Aberration

        void DoChromaticAberration(CommandBuffer cmd, ComputeShader cs, int kernel, UberPostFeatureFlags flags)
        {
            if ((flags & UberPostFeatureFlags.ChromaticAberration) != UberPostFeatureFlags.ChromaticAberration)
                return;

            var spectralLut = m_ChromaticAberration.spectralLut.value;

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

            var settings = new Vector4(m_ChromaticAberration.intensity * 0.05f, m_ChromaticAberration.maxSamples, 0f, 0f);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ChromaSpectralLut, spectralLut);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._ChromaParams, settings);
        }

        #endregion

        #region Vignette

        void DoVignette(CommandBuffer cmd, ComputeShader cs, int kernel, UberPostFeatureFlags flags)
        {
            if ((flags & UberPostFeatureFlags.Vignette) != UberPostFeatureFlags.Vignette)
                return;

            if (m_Vignette.mode.value == VignetteMode.Procedural)
            {
                float roundness = (1f - m_Vignette.roundness.value) * 6f + m_Vignette.roundness.value;
                cmd.SetComputeVectorParam(cs, HDShaderIDs._VignetteParams1, new Vector4(m_Vignette.center.value.x, m_Vignette.center.value.y, 0f, 0f));
                cmd.SetComputeVectorParam(cs, HDShaderIDs._VignetteParams2, new Vector4(m_Vignette.intensity.value * 3f, m_Vignette.smoothness.value * 5f, roundness, m_Vignette.rounded.value ? 1f : 0f));
                cmd.SetComputeVectorParam(cs, HDShaderIDs._VignetteColor, m_Vignette.color.value);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._VignetteMask, Texture2D.blackTexture);
            }
            else // Masked
            {
                var color = m_Vignette.color.value;
                color.a = Mathf.Clamp01(m_Vignette.opacity.value);

                cmd.SetComputeVectorParam(cs, HDShaderIDs._VignetteParams1, new Vector4(0f, 0f, 1f, 0f));
                cmd.SetComputeVectorParam(cs, HDShaderIDs._VignetteColor, color);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._VignetteMask, m_Vignette.mask.value);
            }
        }

        #endregion

        #region Color Grading

        void DoColorGrading(CommandBuffer cmd, ComputeShader cs, int kernel)
        {
            // TODO: User lut support
            // TODO: Curves

            // Prepare data
            var lmsColorBalance = GetColorBalanceCoeffs(m_WhiteBalance.temperature.value, m_WhiteBalance.tint.value);
            var hueSatCon = new Vector4(m_ColorAdjustments.hueShift / 360f, m_ColorAdjustments.saturation / 100f + 1f, m_ColorAdjustments.contrast / 100f + 1f, 0f);
            var channelMixerR = new Vector4(m_ChannelMixer.redOutRedIn   / 100f, m_ChannelMixer.redOutGreenIn   / 100f, m_ChannelMixer.redOutBlueIn   / 100f, 0f);
            var channelMixerG = new Vector4(m_ChannelMixer.greenOutRedIn / 100f, m_ChannelMixer.greenOutGreenIn / 100f, m_ChannelMixer.greenOutBlueIn / 100f, 0f);
            var channelMixerB = new Vector4(m_ChannelMixer.blueOutRedIn  / 100f, m_ChannelMixer.blueOutGreenIn  / 100f, m_ChannelMixer.blueOutBlueIn  / 100f, 0f);

            Vector4 shadows, midtones, highlights, shadowsHighlightsLimits;
            ComputeShadowsMidtonesHighlights(out shadows, out midtones, out highlights, out shadowsHighlightsLimits);

            Vector4 lift, gamma, gain;
            ComputeLiftGammaGain(out lift, out gamma, out gain);

            Vector4 splitShadows, splitHighlights;
            ComputeSplitToning(out splitShadows, out splitHighlights);

            // Setup lut builder compute & grab the kernel we need
            var builderCS = m_Resources.lutBuilder3DCS;
            string kernelName;

            switch (m_Tonemapping.mode.value)
            {
                case TonemappingMode.Neutral: kernelName = "KBuild_NeutralTonemap"; break;
                case TonemappingMode.ACES:    kernelName = "KBuild_AcesTonemap"; break;
                case TonemappingMode.Custom:  kernelName = "KBuild_CustomTonemap"; break;
                default:                      kernelName = "KBuild_NoTonemap"; break;
            }

            int builderKernel = builderCS.FindKernel(kernelName);
            
            // Fill-in constant buffers & textures
            cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._OutputTexture, m_InternalLogLut);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Size, new Vector4(k_LogLutSize, 1f / (k_LogLutSize - 1f), 0f, 0f));
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ColorBalance, lmsColorBalance);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ColorFilter, m_ColorAdjustments.colorFilter.value.linear);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ChannelMixerRed, channelMixerR);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ChannelMixerGreen, channelMixerG);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ChannelMixerBlue, channelMixerB);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._HueSatCon, hueSatCon);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Lift, lift);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Gamma, gamma);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Gain, gain);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Shadows, shadows);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Midtones, midtones);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Highlights, highlights);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ShaHiLimits, shadowsHighlightsLimits);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._SplitShadows, splitShadows);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._SplitHighlights, splitHighlights);

            if (m_Tonemapping.mode.value == TonemappingMode.Custom)
            {
                m_HableCurve.Init(
                    m_Tonemapping.toeStrength.value,
                    m_Tonemapping.toeLength.value,
                    m_Tonemapping.shoulderStrength.value,
                    m_Tonemapping.shoulderLength.value,
                    m_Tonemapping.shoulderAngle.value,
                    m_Tonemapping.gamma.value
                );

                cmd.SetComputeVectorParam(builderCS, HDShaderIDs._CustomToneCurve, m_HableCurve.uniforms.curve);
                cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ToeSegmentA, m_HableCurve.uniforms.toeSegmentA);
                cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ToeSegmentB, m_HableCurve.uniforms.toeSegmentB);
                cmd.SetComputeVectorParam(builderCS, HDShaderIDs._MidSegmentA, m_HableCurve.uniforms.midSegmentA);
                cmd.SetComputeVectorParam(builderCS, HDShaderIDs._MidSegmentB, m_HableCurve.uniforms.midSegmentB);
                cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ShoSegmentA, m_HableCurve.uniforms.shoSegmentA);
                cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ShoSegmentB, m_HableCurve.uniforms.shoSegmentB);
            }

            // Generate the lut
            // See the note about Metal & Intel in LutBuilder3D.compute
            uint threadX, threadY, threadZ;
            builderCS.GetKernelThreadGroupSizes(builderKernel, out threadX, out threadY, out threadZ);
            cmd.DispatchCompute(builderCS, builderKernel,
                (int)((k_LogLutSize + threadX - 1u) / threadX),
                (int)((k_LogLutSize + threadY - 1u) / threadY),
                (int)((k_LogLutSize + threadZ - 1u) / threadZ)
            );

            // Setup the uber shader
            var logLutSettings = new Vector4(1f / k_LogLutSize, k_LogLutSize - 1f, Mathf.Pow(2f, m_ColorAdjustments.postExposure), 0f);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._LogLut3D, m_InternalLogLut);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._LogLut3D_Params, logLutSettings);
        }

        // Returns color balance coefficients in the LMS space
        public static Vector3 GetColorBalanceCoeffs(float temperature, float tint)
        {
            // Range ~[-1.5;1.5] works best
            float t1 = temperature / 65f;
            float t2 = tint / 65f;

            // Get the CIE xy chromaticity of the reference white point.
            // Note: 0.31271 = x value on the D65 white point
            float x = 0.31271f - t1 * (t1 < 0f ? 0.1f : 0.05f);
            float y = ColorUtils.StandardIlluminantY(x) + t2 * 0.05f;

            // Calculate the coefficients in the LMS space.
            var w1 = new Vector3(0.949237f, 1.03542f, 1.08728f); // D65 white point
            var w2 = ColorUtils.CIExyToLMS(x, y);
            return new Vector3(w1.x / w2.x, w1.y / w2.y, w1.z / w2.z);
        }

        void ComputeShadowsMidtonesHighlights(out Vector4 shadows, out Vector4 midtones, out Vector4 highlights, out Vector4 limits)
        {
            float weight;

            shadows = m_ShadowsMidtonesHighlights.shadows.value;
            shadows.x = Mathf.GammaToLinearSpace(shadows.x);
            shadows.y = Mathf.GammaToLinearSpace(shadows.y);
            shadows.z = Mathf.GammaToLinearSpace(shadows.z);
            weight = shadows.w * (Mathf.Sign(shadows.w) < 0f ? 1f : 4f);
            shadows.x = Mathf.Max(shadows.x + weight, 0f);
            shadows.y = Mathf.Max(shadows.y + weight, 0f);
            shadows.z = Mathf.Max(shadows.z + weight, 0f);
            shadows.w = 0f;

            midtones = m_ShadowsMidtonesHighlights.midtones.value;
            midtones.x = Mathf.GammaToLinearSpace(midtones.x);
            midtones.y = Mathf.GammaToLinearSpace(midtones.y);
            midtones.z = Mathf.GammaToLinearSpace(midtones.z);
            weight = midtones.w * (Mathf.Sign(midtones.w) < 0f ? 1f : 4f);
            midtones.x = Mathf.Max(midtones.x + weight, 0f);
            midtones.y = Mathf.Max(midtones.y + weight, 0f);
            midtones.z = Mathf.Max(midtones.z + weight, 0f);
            midtones.w = 0f;

            highlights = m_ShadowsMidtonesHighlights.highlights.value;
            highlights.x = Mathf.GammaToLinearSpace(highlights.x);
            highlights.y = Mathf.GammaToLinearSpace(highlights.y);
            highlights.z = Mathf.GammaToLinearSpace(highlights.z);
            weight = highlights.w * (Mathf.Sign(highlights.w) < 0f ? 1f : 4f);
            highlights.x = Mathf.Max(highlights.x + weight, 0f);
            highlights.y = Mathf.Max(highlights.y + weight, 0f);
            highlights.z = Mathf.Max(highlights.z + weight, 0f);
            highlights.w = 0f;

            limits = new Vector4(
                m_ShadowsMidtonesHighlights.shadowsStart.value,
                m_ShadowsMidtonesHighlights.shadowsEnd.value,
                m_ShadowsMidtonesHighlights.highlightsStart.value,
                m_ShadowsMidtonesHighlights.highlightsEnd.value
            );
        }

        void ComputeLiftGammaGain(out Vector4 lift, out Vector4 gamma, out Vector4 gain)
        {
            lift = m_LiftGammaGain.lift.value;
            lift.x = Mathf.GammaToLinearSpace(lift.x) * 0.15f;
            lift.y = Mathf.GammaToLinearSpace(lift.y) * 0.15f;
            lift.z = Mathf.GammaToLinearSpace(lift.z) * 0.15f;

            float lumLift = lift.x * 0.2126f + lift.y * 0.7152f + lift.z * 0.0722f;
            lift.x = lift.x - lumLift + lift.w;
            lift.y = lift.y - lumLift + lift.w;
            lift.z = lift.z - lumLift + lift.w;
            lift.w = 0f;

            gamma = m_LiftGammaGain.gamma.value;
            gamma.x = Mathf.GammaToLinearSpace(gamma.x) * 0.8f;
            gamma.y = Mathf.GammaToLinearSpace(gamma.y) * 0.8f;
            gamma.z = Mathf.GammaToLinearSpace(gamma.z) * 0.8f;

            float lumGamma = gamma.x * 0.2126f + gamma.y * 0.7152f + gamma.z * 0.0722f;
            gamma.w += 1f;
            gamma.x = 1f / Mathf.Max(gamma.x - lumGamma + gamma.w, 1e-03f);
            gamma.y = 1f / Mathf.Max(gamma.y - lumGamma + gamma.w, 1e-03f);
            gamma.z = 1f / Mathf.Max(gamma.z - lumGamma + gamma.w, 1e-03f);
            gamma.w = 0f;

            gain = m_LiftGammaGain.gain.value;
            gain.x = Mathf.GammaToLinearSpace(gain.x) * 0.8f;
            gain.y = Mathf.GammaToLinearSpace(gain.y) * 0.8f;
            gain.z = Mathf.GammaToLinearSpace(gain.z) * 0.8f;

            float lumGain = gain.x * 0.2126f + gain.y * 0.7152f + gain.z * 0.0722f;
            gain.w += 1f;
            gain.x = gain.x - lumGain + gain.w;
            gain.y = gain.y - lumGain + gain.w;
            gain.z = gain.z - lumGain + gain.w;
            gain.w = 0f;
        }

        void ComputeSplitToning(out Vector4 shadows, out Vector4 highlights)
        {
            // As counter-intuitive as it is, to make split-toning work the same way it does in
            // Adobe products we have to do all the maths in sRGB... So do not convert these to
            // linear before sending them to the shader, this isn't a bug!
            shadows = m_SplitToning.shadows.value;
            highlights = m_SplitToning.highlights.value;

            // Balance is stored in `shadows.w`
            shadows.w = m_SplitToning.balance.value / 100f;
            highlights.w = 0f;
        }

        #endregion

        #region Final Pass

        void DoFinalPass(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
        {
            // TODO: FIXME
            // This pass should be the one writing to the backbuffer, but we can't write to it using
            // a compute so it should be converted to a fragment.
            // Also missing: 8-bit dithering, ODT transform
            if (camera.antialiasing == AntialiasingMode.FastApproximateAntialiasing)
            {
                var cs = m_Resources.fxaaCS;
                int kernel = cs.FindKernel("KFXAA");
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, source);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, destination);
                cmd.DispatchCompute(cs, kernel, (camera.actualWidth + 7) / 8, (camera.actualHeight + 7) / 8, 1);
            }
            else
            {
                cmd.Blit(source, destination);
            }
        }

        #endregion
    }
}
