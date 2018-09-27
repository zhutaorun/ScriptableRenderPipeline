using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    internal class DefaultRendererSetup : IRendererSetup
    {
        private DepthOnlyPass m_DepthOnlyPass;
        private MainLightShadowCasterPass m_MainLightShadowCasterPass;
        private AdditionalLightsShadowCasterPass m_AdditionalLightsShadowCasterPass;
        private SetupForwardRenderingPass m_SetupForwardRenderingPass;
        private ScreenSpaceShadowResolvePass m_ScreenSpaceShadowResolvePass;
        private CreateLightweightRenderTexturesPass m_CreateLightweightRenderTexturesPass;
        private BeginXRRenderingPass m_BeginXrRenderingPass;
        private SetupLightweightConstanstPass m_SetupLightweightConstants;
        private RenderOpaqueForwardPass m_RenderOpaqueForwardPass;
        private OpaquePostProcessPass m_OpaquePostProcessPass;
        private DrawSkyboxPass m_DrawSkyboxPass;
        private CopyDepthPass m_CopyDepthPass;
        private CopyColorPass m_CopyColorPass;
        private RenderTransparentForwardPass m_RenderTransparentForwardPass;
        private TransparentPostProcessPass m_TransparentPostProcessPass;
        private FinalBlitPass m_FinalBlitPass;
        private EndXRRenderingPass m_EndXrRenderingPass;

#if UNITY_EDITOR
        private SceneViewDepthCopyPass m_SceneViewDepthCopyPass;
#endif


        private RenderTargetHandle ColorAttachment;
        private RenderTargetHandle DepthAttachment;
        private RenderTargetHandle DepthTexture;
        private RenderTargetHandle OpaqueColor;
        private RenderTargetHandle MainLightShadowmap;
        private RenderTargetHandle AdditionalLightsShadowmap;
        private RenderTargetHandle ScreenSpaceShadowmap;

        [NonSerialized]
        private bool m_Initialized = false;

        private void Init()
        {
            if (m_Initialized)
                return;

            m_DepthOnlyPass = new DepthOnlyPass();
            m_MainLightShadowCasterPass = new MainLightShadowCasterPass();
            m_AdditionalLightsShadowCasterPass = new AdditionalLightsShadowCasterPass();
            m_SetupForwardRenderingPass = new SetupForwardRenderingPass();
            m_ScreenSpaceShadowResolvePass = new ScreenSpaceShadowResolvePass();
            m_CreateLightweightRenderTexturesPass = new CreateLightweightRenderTexturesPass();
            m_BeginXrRenderingPass = new BeginXRRenderingPass();
            m_SetupLightweightConstants = new SetupLightweightConstanstPass();
            m_RenderOpaqueForwardPass = new RenderOpaqueForwardPass();
            m_OpaquePostProcessPass = new OpaquePostProcessPass();
            m_DrawSkyboxPass = new DrawSkyboxPass();
            m_CopyDepthPass = new CopyDepthPass();
            m_CopyColorPass = new CopyColorPass();
            m_RenderTransparentForwardPass = new RenderTransparentForwardPass();
            m_TransparentPostProcessPass = new TransparentPostProcessPass();
            m_FinalBlitPass = new FinalBlitPass();
            m_EndXrRenderingPass = new EndXRRenderingPass();

#if UNITY_EDITOR
            m_SceneViewDepthCopyPass = new SceneViewDepthCopyPass();
#endif

            // RenderTexture format depends on camera and pipeline (HDR, non HDR, etc)
            // Samples (MSAA) depend on camera and pipeline
            ColorAttachment.Init("_CameraColorTexture");
            DepthAttachment.Init("_CameraDepthAttachment");
            DepthTexture.Init("_CameraDepthTexture");
            OpaqueColor.Init("_CameraOpaqueTexture");
            MainLightShadowmap.Init("_MainLightShadowmapTexture");
            AdditionalLightsShadowmap.Init("_AdditionalLightsShadowmapTexture");
            ScreenSpaceShadowmap.Init("_ScreenSpaceShadowmapTexture");

            m_Initialized = true;
        }

        public static bool RequiresIntermediateColorTexture(ref CameraData cameraData, RenderTextureDescriptor baseDescriptor)
        {
            if (cameraData.isOffscreenRender)
                return false;

            bool isScaledRender = !Mathf.Approximately(cameraData.renderScale, 1.0f);
            bool isTargetTexture2DArray = baseDescriptor.dimension == TextureDimension.Tex2DArray;
            return cameraData.isSceneViewCamera || isScaledRender || cameraData.isHdrEnabled ||
                cameraData.postProcessEnabled || cameraData.requiresOpaqueTexture || isTargetTexture2DArray || !cameraData.isDefaultViewport;
        }

        public void Setup(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            Init();

            Camera camera = renderingData.cameraData.camera;

            renderer.SetupPerObjectLightIndices(ref renderingData.cullResults, ref renderingData.lightData);
            RenderTextureDescriptor baseDescriptor = ScriptableRenderer.CreateRenderTextureDescriptor(ref renderingData.cameraData);
            RenderTextureDescriptor shadowDescriptor = baseDescriptor;
            shadowDescriptor.dimension = TextureDimension.Tex2D;

            bool requiresDepthPrepass = renderingData.shadowData.requiresScreenSpaceShadowResolve || renderingData.cameraData.isSceneViewCamera ||
                (renderingData.cameraData.requiresDepthTexture && !CanCopyDepth(ref renderingData.cameraData));

            // For now VR requires a depth prepass until we figure out how to properly resolve texture2DMS in stereo
            requiresDepthPrepass |= renderingData.cameraData.isStereoEnabled;

            if (renderingData.shadowData.supportsMainLightShadows)
            {
                m_MainLightShadowCasterPass.Setup(MainLightShadowmap);
                renderer.EnqueuePass(m_MainLightShadowCasterPass);
            }

            if (renderingData.shadowData.supportsAdditionalLightShadows)
            {
                m_AdditionalLightsShadowCasterPass.Setup(AdditionalLightsShadowmap, renderer.maxVisibleAdditionalLights);
                renderer.EnqueuePass(m_AdditionalLightsShadowCasterPass);
            }

            renderer.EnqueuePass(m_SetupForwardRenderingPass);

            var afterDepthpasses = camera.GetComponents<IAfterDepthPrePass>();
            var afterOpaquePasses = camera.GetComponents<IAfterOpaquePass>();
            var afterOpaquePostProcessPasses = camera.GetComponents<IAfterOpaquePostProcess>();
            var afterSkyboxPasses = camera.GetComponents<IAfterSkyboxPass>();
            var afterTransparentPasses = camera.GetComponents<IAfterTransparentPass>();
            var afterRenderPasses = camera.GetComponents<IAfterRender>();

            if (requiresDepthPrepass)
            {
                m_DepthOnlyPass.Setup(baseDescriptor, DepthTexture, SampleCount.One);
                renderer.EnqueuePass(m_DepthOnlyPass);

                foreach (var pass in afterDepthpasses)
                    renderer.EnqueuePass(pass.GetPassToEnqueue(m_DepthOnlyPass.descriptor, DepthTexture));
            }

            if (renderingData.shadowData.supportsMainLightShadows &&
                renderingData.shadowData.requiresScreenSpaceShadowResolve)
            {
                m_ScreenSpaceShadowResolvePass.Setup(baseDescriptor, ScreenSpaceShadowmap);
                renderer.EnqueuePass(m_ScreenSpaceShadowResolvePass);
            }

            bool requiresRenderToTexture = RequiresIntermediateColorTexture(ref renderingData.cameraData, baseDescriptor)
                    || afterDepthpasses.Length != 0
                    || afterOpaquePasses.Length != 0
                    || afterOpaquePostProcessPasses.Length != 0
                    || afterSkyboxPasses.Length != 0
                    || afterTransparentPasses.Length != 0
                    || afterRenderPasses.Length != 0;

            RenderTargetHandle colorHandle = RenderTargetHandle.CameraTarget;
            RenderTargetHandle depthHandle = RenderTargetHandle.CameraTarget;

            if (requiresRenderToTexture)
            {
                colorHandle = ColorAttachment;
                depthHandle = DepthAttachment;

                var sampleCount = (SampleCount)renderingData.cameraData.msaaSamples;
                m_CreateLightweightRenderTexturesPass.Setup(baseDescriptor, colorHandle, depthHandle, sampleCount);
                renderer.EnqueuePass(m_CreateLightweightRenderTexturesPass);
            }

            if (renderingData.cameraData.isStereoEnabled)
                renderer.EnqueuePass(m_BeginXrRenderingPass);

            RendererConfiguration rendererConfiguration = ScriptableRenderer.GetRendererConfiguration(renderingData.lightData.additionalLightsCount);

            m_SetupLightweightConstants.Setup(renderer.maxVisibleAdditionalLights, renderer.perObjectLightIndices);
            renderer.EnqueuePass(m_SetupLightweightConstants);

            m_RenderOpaqueForwardPass.Setup(baseDescriptor, colorHandle, depthHandle, ScriptableRenderer.GetCameraClearFlag(camera), camera.backgroundColor, rendererConfiguration);
            renderer.EnqueuePass(m_RenderOpaqueForwardPass);
            foreach (var pass in afterOpaquePasses)
                renderer.EnqueuePass(pass.GetPassToEnqueue(baseDescriptor, colorHandle, depthHandle));

            if (renderingData.cameraData.postProcessEnabled &&
                renderingData.cameraData.postProcessLayer.HasOpaqueOnlyEffects(renderer.postProcessingContext))
            {
                m_OpaquePostProcessPass.Setup(baseDescriptor, colorHandle);
                renderer.EnqueuePass(m_OpaquePostProcessPass);

                foreach (var pass in afterOpaquePostProcessPasses)
                    renderer.EnqueuePass(pass.GetPassToEnqueue(baseDescriptor, colorHandle, depthHandle));
            }

            if (camera.clearFlags == CameraClearFlags.Skybox)
            {
                m_DrawSkyboxPass.Setup(colorHandle, depthHandle);
                renderer.EnqueuePass(m_DrawSkyboxPass);
            }

            foreach (var pass in afterSkyboxPasses)
                renderer.EnqueuePass(pass.GetPassToEnqueue(baseDescriptor, colorHandle, depthHandle));

            if (renderingData.cameraData.requiresDepthTexture && !requiresDepthPrepass)
            {
                m_CopyDepthPass.Setup(depthHandle, DepthTexture);
                renderer.EnqueuePass(m_CopyDepthPass);
            }

            if (renderingData.cameraData.requiresOpaqueTexture)
            {
                m_CopyColorPass.Setup(colorHandle, OpaqueColor);
                renderer.EnqueuePass(m_CopyColorPass);
            }

            m_RenderTransparentForwardPass.Setup(baseDescriptor, colorHandle, depthHandle, rendererConfiguration);
            renderer.EnqueuePass(m_RenderTransparentForwardPass);

            foreach (var pass in afterTransparentPasses)
                renderer.EnqueuePass(pass.GetPassToEnqueue(baseDescriptor, colorHandle, depthHandle));

            bool afterRenderExists = afterRenderPasses.Length != 0;

            // if we have additional filters
            // we need to stay in a RT
            if (afterRenderExists)
            {
                // perform post with src / dest the same
                if (!renderingData.cameraData.isStereoEnabled && renderingData.cameraData.postProcessEnabled)
                {
                    m_TransparentPostProcessPass.Setup(baseDescriptor, colorHandle, colorHandle.id, false);
                    renderer.EnqueuePass(m_TransparentPostProcessPass);
                }

                //execute after passes
                foreach (var pass in afterRenderPasses)
                    renderer.EnqueuePass(pass.GetPassToEnqueue(baseDescriptor, colorHandle, depthHandle));

                //now blit into the final target
                if (!renderingData.cameraData.isOffscreenRender && colorHandle != RenderTargetHandle.CameraTarget)
                {
                    m_FinalBlitPass.Setup(baseDescriptor, colorHandle);
                    renderer.EnqueuePass(m_FinalBlitPass);
                }
            }
            else
            {
                if (!renderingData.cameraData.isStereoEnabled && renderingData.cameraData.postProcessEnabled)
                {
                    m_TransparentPostProcessPass.Setup(baseDescriptor, colorHandle, BuiltinRenderTextureType.CameraTarget, (!renderingData.cameraData.isStereoEnabled && renderingData.cameraData.camera.targetTexture == null));
                    renderer.EnqueuePass(m_TransparentPostProcessPass);
                }
                else if (!renderingData.cameraData.isOffscreenRender && colorHandle != RenderTargetHandle.CameraTarget)
                {
                    m_FinalBlitPass.Setup(baseDescriptor, colorHandle);
                    renderer.EnqueuePass(m_FinalBlitPass);
                }
            }
            
            if (renderingData.cameraData.isStereoEnabled)
            {
                renderer.EnqueuePass(m_EndXrRenderingPass);
            }

#if UNITY_EDITOR
            if (renderingData.cameraData.isSceneViewCamera)
            {
                m_SceneViewDepthCopyPass.Setup(DepthTexture);
                renderer.EnqueuePass(m_SceneViewDepthCopyPass);
            }
#endif
        }

        bool CanCopyDepth(ref CameraData cameraData)
        {
            bool msaaEnabledForCamera = (int)cameraData.msaaSamples > 1;
            bool supportsTextureCopy = SystemInfo.copyTextureSupport != CopyTextureSupport.None;
            bool supportsDepthTarget = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth);
            bool supportsDepthCopy = !msaaEnabledForCamera && (supportsDepthTarget || supportsTextureCopy);

            // TODO:  We don't have support to highp Texture2DMS currently and this breaks depth precision.
            // currently disabling it until shader changes kick in.
            //bool msaaDepthResolve = msaaEnabledForCamera && SystemInfo.supportsMultisampledTextures != 0;
            bool msaaDepthResolve = false;
            return supportsDepthCopy || msaaDepthResolve;
        }
    }
}
