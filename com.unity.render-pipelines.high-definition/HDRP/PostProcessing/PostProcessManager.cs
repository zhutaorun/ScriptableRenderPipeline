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
        public RTHandle exposureTexture { get; private set; }

        // On some GPU/driver version combos, using a max fp16 value of 65504 will just warp around
        // and break everything so we want to use the first valid value before fp16_max
        const float k_HalfMaxMinusOne = 65472f; // (2 - 2^-9) * 2^15

        RenderPipelineResources m_Resources;
        CameraControls m_Settings;

        const int k_ExposureCurvePrecision = 128;
        Color[] m_TempColorArray = new Color[k_ExposureCurvePrecision];

        Texture2D m_ExposureCurveTexture;
        RTHandle m_TempTarget1024;
        RTHandle m_TempTarget32;
        RTHandle m_TempTarget1;

        public PostProcessManager(HDRenderPipelineAsset hdAsset)
        {
            m_Resources = hdAsset.renderPipelineResources;

            exposureTexture = RTHandles.Alloc(1, 1, colorFormat: RenderTextureFormat.RHalf,
                sRGB: false, enableRandomWrite: true, name: "EV100 Exposure"
            );

            // Setup a default exposure texture for the first frame
            var tempTex = new Texture2D(1, 1, TextureFormat.RHalf, false, true);
            tempTex.SetPixel(0, 0, Color.clear);
            tempTex.Apply();
            Graphics.Blit(tempTex, exposureTexture);
            CoreUtils.Destroy(tempTex);
        }

        public void Cleanup()
        {
            RTHandles.Release(exposureTexture);
            RTHandles.Release(m_TempTarget1024);
            RTHandles.Release(m_TempTarget32);
            RTHandles.Release(m_TempTarget1);
            CoreUtils.Destroy(m_ExposureCurveTexture);

            exposureTexture = null;
            m_TempTarget1024 = null;
            m_TempTarget32 = null;
            m_TempTarget1 = null;
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
        }

        #region Exposure

        void CheckAverageLuminanceTargets()
        {
            if (m_TempTarget1 != null)
                return;
            
            m_TempTarget1024 = RTHandles.Alloc(
                1024, 1024, colorFormat: RenderTextureFormat.RHalf,
                sRGB: false, enableRandomWrite: true, name: "Average Luminance Temp 1024"
            );
            m_TempTarget32 = RTHandles.Alloc(
                32, 32, colorFormat: RenderTextureFormat.RHalf,
                sRGB: false, enableRandomWrite: true, name: "Average Luminance Temp 32"
            );
            m_TempTarget1 = RTHandles.Alloc(
                1, 1, colorFormat: RenderTextureFormat.RHalf,
                sRGB: false, enableRandomWrite: true, name: "Average Luminance Temp 1"
            );
        }

        void PushExposureCurveData(CommandBuffer cmd, ComputeShader cs, int kernel)
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
            float min = 0f, max = 0f;

            // Fail safe in case the curve is deleted / has 0 point
            if (curve == null || curve.length == 0)
            {
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

            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ExposureCurveTexture, m_ExposureCurveTexture);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams, new Vector4(min, max));
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

                int kernel;
                RTHandle input;

                if (m_Settings.luminanceSource == LuminanceSource.LightingBuffer)
                {
                    kernel = cs.FindKernel("KAvgLumaPrePass_Lighting");
                    input = parameters.lightingBuffer;
                }
                else
                {
                    kernel = cs.FindKernel("KAvgLumaPrePass_Color");
                    input = parameters.colorBuffer;
                }

                // Pre-pass
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ExposureTexture, exposureTexture);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, input);
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
                    kernel = cs.FindKernel("KReduction_EvaluateAuto");
                    cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams, new Vector4(m_Settings.exposureCompensation, 0f, 0f, 0f));
                }
                else if (m_Settings.exposureMode == ExposureMode.CurveMapping)
                {
                    kernel = cs.FindKernel("KReduction_EvaluateCurve");
                    PushExposureCurveData(cmd, cs, kernel);
                }

                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, m_TempTarget32);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OuputTexture, exposureTexture);
                cmd.DispatchCompute(cs, kernel, 1, 1, 1);
            }
        }

        #endregion
    }
}
