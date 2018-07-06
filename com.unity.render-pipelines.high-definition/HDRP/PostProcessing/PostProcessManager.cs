using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    using RTHandle = RTHandleSystem.RTHandle;

    public struct PostProcessParameters
    {
        public HDCamera camera;
        public CommandBuffer cmd;
        public RTHandle colorBuffer;
        public RTHandle lightingBuffer;
    }

    // Main class for all post-processing related features - only includes camera effects, no
    // lighting/surface effect like SSR/AO
    public sealed class PostProcessManager
    {
        // On some GPU/driver version combos, using a max fp16 value of 65504 will just warp around
        // and break everything so we want to use the first valid value before fp16_max
        const float k_HalfMaxMinusOne = 65472f; // (2 - 2^-9) * 2^15

        RenderPipelineResources m_Resources;

        const int k_ExposureCurvePrecision = 128;
        Color[] m_TempColorArray = new Color[k_ExposureCurvePrecision];
        int[] m_ExposureVariants = new int[4];

        bool m_FirstFrame = true;

        Texture2D m_ExposureCurveTexture;
        RTHandle m_TempTexture1024;
        RTHandle m_TempTexture32;
        RTHandle m_EmptyExposureTexture;

        public PostProcessManager(HDRenderPipelineAsset hdAsset)
        {
            m_Resources = hdAsset.renderPipelineResources;
            
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
                if (!IsExposureFixed())
                {
                    using (new ProfilingSample(cmd, "Dynamic Exposure", CustomSamplerId.Exposure.GetSampler()))
                    {
                        DoDynamicExposure(cmd, camera, colorBuffer, lightingBuffer);
                    }
                }
            }

            m_FirstFrame = false;
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

            var pixels = m_TempColorArray;

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
    }
}
