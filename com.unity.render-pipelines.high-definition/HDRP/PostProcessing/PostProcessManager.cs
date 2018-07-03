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
        // 1x1 pixel, holds the current exposure value in EV100 in the red channel
        public RTHandle exposureTexture { get { return m_ExposureTexture[m_ExposureTextureId]; } }

        // On some GPU/driver version combos, using a max fp16 value of 65504 will just warp around
        // and break everything so we want to use the first valid value before fp16_max
        const float k_HalfMaxMinusOne = 65472f; // (2 - 2^-9) * 2^15

        RenderPipelineResources m_Resources;
        CameraControls m_Settings;

        const int k_ExposureCurvePrecision = 128;
        Color[] m_TempColorArray = new Color[k_ExposureCurvePrecision];
        int[] m_ExposureVariants = new int[4];

        bool m_FirstFrame = true;

        Texture2D m_ExposureCurveTexture;
        RTHandle m_TempTarget1024;
        RTHandle m_TempTarget32;

        // Ping-pong exposure textures used for adaptation
        RTHandle[] m_ExposureTexture = new RTHandle[2];
        int m_ExposureTextureId = 0;

        public PostProcessManager(HDRenderPipelineAsset hdAsset)
        {
            m_Resources = hdAsset.renderPipelineResources;

            for (int i = 0; i < 2; i++)
            {
                m_ExposureTexture[i] = RTHandles.Alloc(1, 1, colorFormat: RenderTextureFormat.RGHalf,
                    sRGB: false, enableRandomWrite: true, name: "EV100 Exposure"
                );
            }

            // Setup a default exposure textures for the first frame
            var tempTex = new Texture2D(1, 1, TextureFormat.RGHalf, false, true);
            tempTex.SetPixel(0, 0, Color.clear);
            tempTex.Apply();
            Graphics.Blit(tempTex, m_ExposureTexture[0]);
            Graphics.Blit(tempTex, m_ExposureTexture[1]);
            CoreUtils.Destroy(tempTex);
        }

        public void Cleanup()
        {
            for (int i = 0; i < 2; i++)
            {
                RTHandles.Release(m_ExposureTexture[i]);
                m_ExposureTexture[i] = null;
            }

            RTHandles.Release(m_TempTarget1024);
            m_TempTarget1024 = null;

            RTHandles.Release(m_TempTarget32);
            m_TempTarget32 = null;

            CoreUtils.Destroy(m_ExposureCurveTexture);
            m_ExposureCurveTexture = null;
        }

        public void PushGlobalParams(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(HDShaderIDs._ExposureTexture, exposureTexture);
        }

        public void Render(ref PostProcessParameters parameters)
        {
            m_Settings = VolumeManager.instance.stack.GetComponent<CameraControls>();
            var cmd = parameters.cmd;

            using (new ProfilingSample(cmd, "Post-processing", CustomSamplerId.PostProcessing.GetSampler()))
            {
                using (new ProfilingSample(cmd, "Exposure", CustomSamplerId.Exposure.GetSampler()))
                {
                    DoExposure(ref parameters);
                }
            }

            m_FirstFrame = false;
        }

        #region Exposure

        void CheckAverageLuminanceTargets()
        {
            if (m_TempTarget1024 != null)
                return;

            const RenderTextureFormat kFormat = RenderTextureFormat.RGHalf;

            m_TempTarget1024 = RTHandles.Alloc(
                1024, 1024, colorFormat: kFormat, sRGB: false,
                enableRandomWrite: true, name: "Average Luminance Temp 1024"
            );
            m_TempTarget32 = RTHandles.Alloc(
                32, 32, colorFormat: kFormat, sRGB: false,
                enableRandomWrite: true, name: "Average Luminance Temp 32"
            );
        }

        void PrepareExposureCurveData(out float min, out float max)
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
            
            var curve = m_Settings.exposureCurveMap.value;
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

        void DoExposure(ref PostProcessParameters parameters)
        {
            var cs = m_Resources.exposureCS;
            var cmd = parameters.cmd;

            if (m_Settings.exposureMode == ExposureMode.Fixed)
            {
                int kernel = cs.FindKernel("KFixedExposure");
                cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams, new Vector4(m_Settings.fixedExposure, 0f, 0f, 0f));
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OuputTexture, exposureTexture);
                cmd.DispatchCompute(cs, kernel, 1, 1, 1);
            }
            else if (m_Settings.exposureMode == ExposureMode.UseCameraSettings && m_Settings.cameraShootingMode == ShootingMode.Manual)
            {
                int kernel = cs.FindKernel("KManualCameraExposure");
                cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams, new Vector4(m_Settings.exposureCompensation, m_Settings.lensAperture, m_Settings.cameraShutterSpeed, m_Settings.cameraIso));
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OuputTexture, exposureTexture);
                cmd.DispatchCompute(cs, kernel, 1, 1, 1);
            }
            else
            {
                CheckAverageLuminanceTargets();

                // Setup variants
                // TODO: bake array
                var adaptationMode = m_Settings.adaptationMode.value;

                if (!Application.isPlaying || m_FirstFrame)
                    adaptationMode = AdaptationMode.Fixed;
                
                m_ExposureVariants[0] = (int)m_Settings.luminanceSource.value;
                m_ExposureVariants[1] = (int)m_Settings.exposureMeteringMode.value;
                m_ExposureVariants[2] = (int)adaptationMode;
                m_ExposureVariants[3] = 0;

                // Ping pong exposure textures
                int pp = m_ExposureTextureId;
                var nextExposure = m_ExposureTexture[++pp % 2];
                var prevExposure = m_ExposureTexture[++pp % 2];
                m_ExposureTextureId = ++pp % 2;

                // Pre-pass
                var sourceTex = m_Settings.luminanceSource == LuminanceSource.LightingBuffer
                    ? parameters.lightingBuffer
                    : parameters.colorBuffer;

                int kernel = cs.FindKernel("KPrePass");
                cmd.SetComputeIntParams(cs, HDShaderIDs._Variants, m_ExposureVariants);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PreviousExposureTexture, prevExposure);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, sourceTex);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OuputTexture, m_TempTarget1024);
                cmd.DispatchCompute(cs, kernel, 1024 / 8, 1024 / 8, 1);

                // Reduction: 1st pass (1024 -> 32)
                kernel = cs.FindKernel("KReduction");
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, m_TempTarget1024);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OuputTexture, m_TempTarget32);
                cmd.DispatchCompute(cs, kernel, 32, 32, 1);

                // Reduction: 2nd pass (32 -> 1) + evaluate exposure
                if (m_Settings.exposureMode == ExposureMode.Automatic)
                {
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ExposureCurveTexture, Texture2D.blackTexture);
                    cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams, new Vector4(m_Settings.exposureCompensation, 0f, m_Settings.adaptationSpeedDown, m_Settings.adaptationSpeedUp));
                    m_ExposureVariants[3] = 1;
                }
                else if (m_Settings.exposureMode == ExposureMode.CurveMapping)
                {
                    float min, max;
                    PrepareExposureCurveData(out min, out max);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ExposureCurveTexture, m_ExposureCurveTexture);
                    cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams, new Vector4(min, max, m_Settings.adaptationSpeedDown, m_Settings.adaptationSpeedUp));
                    m_ExposureVariants[3] = 2;
                }
                
                cmd.SetComputeIntParams(cs, HDShaderIDs._Variants, m_ExposureVariants);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PreviousExposureTexture, prevExposure);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, m_TempTarget32);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OuputTexture, nextExposure);
                cmd.DispatchCompute(cs, kernel, 1, 1, 1);
            }
        }

        #endregion
    }
}
