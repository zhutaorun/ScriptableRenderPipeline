using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace ShaderGraph.Tests
{
    public class TestPipeline : RenderPipeline
    {
        private const string k_cameraTag = "TestSRP - Render Camera";
        private readonly ShaderPassName m_shaderPassName = new ShaderPassName("TestPass");
        private CullResults m_cullResults = new CullResults();

        public TestPipeline()
        {
            Shader.globalRenderPipeline = "TestPipeline";
            SetRenderFeatures();
        }

        public override void Dispose()
        {
            Shader.globalRenderPipeline = "";
            SupportedRenderingFeatures.active = new SupportedRenderingFeatures();
        }

        private static void SetRenderFeatures()
        {
            #if UNITY_EDITOR
            SupportedRenderingFeatures.active = new SupportedRenderingFeatures
            {
                reflectionProbeSupportFlags = SupportedRenderingFeatures.ReflectionProbeSupportFlags.None,
                defaultMixedLightingMode = SupportedRenderingFeatures.LightmapMixedBakeMode.None,
                supportedMixedLightingModes = SupportedRenderingFeatures.LightmapMixedBakeMode.None,
                supportedLightmapBakeTypes = LightmapBakeType.Baked,
                supportedLightmapsModes = LightmapsMode.NonDirectional,
                rendererSupportsLightProbeProxyVolumes = false,
                rendererSupportsMotionVectors = false,
                rendererSupportsReceiveShadows = false,
                rendererSupportsReflectionProbes = false
            };
            #endif
        }

        public override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
        {
            if (cameras == null || cameras.Length == 0)
            {
                Debug.LogWarning("The camera list passed to the render pipeline is either null or empty.");
                return;
            }

            base.Render(renderContext, cameras);

            SetPerFramShaderConstants();

            int numCams = cameras.Length;
            for(int i = 0; i < numCams; ++i)
            {
                Camera camera = cameras[i];
                
                BeginCameraRendering(camera);
                
                RenderCamera(renderContext, camera);
            }
        }

        public void RenderCamera(ScriptableRenderContext renderContext, Camera camera)
        {
            renderContext.SetupCameraProperties(camera, false);
            SetPerCameraShaderConstants(camera);

            ScriptableCullingParameters cullingParameters;
            if(!CullResults.GetCullingParameters(camera, false, out cullingParameters))
            {
                return;
            }

            cullingParameters.shadowDistance = 0.0f;
            CullResults.Cull(ref cullingParameters, renderContext, ref m_cullResults);

            // clear color and depth buffers
            CommandBuffer cmd = CommandBufferPool.Get(k_cameraTag);
            cmd.ClearRenderTarget(true, true, Color.black);
            renderContext.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            // draw opaque renderers
            DrawRendererSettings drawSettings = new DrawRendererSettings(camera, m_shaderPassName);
            FilterRenderersSettings filterSettings = new FilterRenderersSettings();
            drawSettings.sorting.flags = SortFlags.CommonOpaque;
            filterSettings.renderQueueRange = RenderQueueRange.opaque;
            renderContext.DrawRenderers(m_cullResults.visibleRenderers, ref drawSettings, filterSettings);
            renderContext.Submit();
        }

        private static void SetPerFramShaderConstants()
        {

        }

        private static void SetPerCameraShaderConstants(Camera camera)
        {
            Matrix4x4 proj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
            Matrix4x4 view = camera.worldToCameraMatrix;
            Matrix4x4 viewProj = proj * view;
            Shader.SetGlobalMatrix("unity_MatrixVP", viewProj);
        }
    }
}