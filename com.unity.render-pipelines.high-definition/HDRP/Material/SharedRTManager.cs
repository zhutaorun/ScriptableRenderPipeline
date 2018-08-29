using UnityEngine.Rendering;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class SharedRTManager
    {
        // The render target used when we do not support MSAA
        RTHandleSystem.RTHandle m_NormalRT = null;
        RTHandleSystem.RTHandle m_CameraDepthStencilBuffer = null;
        RTHandleSystem.RTHandle m_CameraDepthBufferCopy;
        RTHandleSystem.RTHandle m_CameraStencilBufferCopy;

        // The two render targets that should be used when we render in MSAA
        RTHandleSystem.RTHandle m_NormalMSAART = null;
        RTHandleSystem.RTHandle m_DepthAsColorMSAART = null;
        RTHandleSystem.RTHandle m_CameraDepthStencilMSAABuffer;
        RTHandleSystem.RTHandle m_CameraDepthValuesBuffer = null; // Depth values buffer (Max/Min/Average)

        // MSAA resolve materials
        Material m_DepthResolveMaterial  = null;
        Material m_ColorResolveMaterial = null;

        // Flag that defines if we are using a local texture or external
        bool m_ExternalBuffer = false;
        bool m_MSAASupported = false;
        bool m_CopyRequired = false;

        // Arrays of RTIDs that are used to set render targets (when MSAA and when not MSAA)
        protected RenderTargetIdentifier[] m_RTIDs = new RenderTargetIdentifier[1];
        protected RenderTargetIdentifier[] m_MSAARTIDs = new RenderTargetIdentifier[2];

        public SharedRTManager()
        {
        }

        public void InitSharedBuffers(GBufferManager gbufferManager, RenderPipelineSettings settings, RenderPipelineResources resources, bool copyBuffer)
        {
            // Set the flags
            m_MSAASupported = settings.supportMSAA;
            m_ExternalBuffer = !settings.supportOnlyForward;
            m_CopyRequired = copyBuffer;

            // Create the depth/ stencil buffer
            m_CameraDepthStencilBuffer = RTHandles.Alloc(Vector2.one, depthBufferBits: DepthBits.Depth24, colorFormat: RenderTextureFormat.Depth, filterMode: FilterMode.Point, name: "CameraDepthStencil");

            if (m_CopyRequired)
            {
                m_CameraDepthBufferCopy = RTHandles.Alloc(Vector2.one, depthBufferBits: DepthBits.Depth24, colorFormat: RenderTextureFormat.Depth, filterMode: FilterMode.Point, name: "CameraDepthStencilCopy");
            }

            // Technically we won't need this buffer in some cases, but nothing that we can determine at init time.
            m_CameraStencilBufferCopy = RTHandles.Alloc(Vector2.one, depthBufferBits: DepthBits.None, colorFormat: RenderTextureFormat.R8, sRGB: false, filterMode: FilterMode.Point, name: "CameraStencilCopy"); // DXGI_FORMAT_R8_UINT is not supported by Unity

            if (m_MSAASupported)
            {
                // Let's create the MSAA textures
                m_CameraDepthStencilMSAABuffer = RTHandles.Alloc(Vector2.one, depthBufferBits: DepthBits.Depth24, colorFormat: RenderTextureFormat.Depth, filterMode: FilterMode.Point, bindTextureMS: true, enableMSAA: true, name: "CameraDepthStencilMSAA");
                m_CameraDepthValuesBuffer = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.ARGBFloat, sRGB: false, name: "DepthValuesBuffer");

                // Create the required resolve materials
                m_DepthResolveMaterial = CoreUtils.CreateEngineMaterial(resources.depthValues);
                m_ColorResolveMaterial = CoreUtils.CreateEngineMaterial(resources.colorResolve);
            }

            // If we are in the forward only mode 
            if (!m_ExternalBuffer)
            {
                // In case of full forward we must allocate the render target for normal buffer (or reuse one already existing)
                // TODO: Provide a way to reuse a render target
                m_NormalRT = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.ARGB32, sRGB: false, name: "NormalBuffer");

                // Is MSAA supported?
                if (m_MSAASupported)
                {
                    // Allocate the two render textures we need in the MSAA case
                    m_NormalMSAART = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.ARGB32, sRGB: false, enableMSAA: true, bindTextureMS: true, name: "NormalBufferMSAA");
                    m_DepthAsColorMSAART = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.RFloat, sRGB: false, bindTextureMS: true, enableMSAA: true, name: "DepthAsColorMSAA");
                }
            }
            else
            {
                // When not forward only we should are using the normal buffer of the gbuffer
                // In case of deferred, we must be in sync with NormalBuffer.hlsl and lit.hlsl files and setup the correct buffers
                m_NormalRT = gbufferManager.GetNormalBuffer(0); // Normal + Roughness
            }
        }

        public RenderTargetIdentifier[] GetPrepassBuffersRTI(bool activeMSAA = false)
        {
            if(activeMSAA)
            {
                Debug.Assert(m_MSAASupported);
                m_MSAARTIDs[0] = m_NormalMSAART.nameID;
                m_MSAARTIDs[1] = m_DepthAsColorMSAART.nameID;
                return m_MSAARTIDs;
            }
            else
            {
                m_RTIDs[0] = m_NormalRT.nameID;
                return m_RTIDs;
            }
        }

        public RTHandleSystem.RTHandle GetNormalBuffer(bool isMSAA)
        {
            if(isMSAA)
            {
                Debug.Assert(m_MSAASupported);
                return m_NormalMSAART;
            }
            else
            {
                return m_NormalRT;
            }
        }

        public RTHandleSystem.RTHandle GetDepthStencilBuffer(bool isMSAA = false)
        {
            if (isMSAA)
            {
                Debug.Assert(m_MSAASupported);
                return m_CameraDepthStencilMSAABuffer;
            }
            else
            {
                return m_CameraDepthStencilBuffer;
            }
        }

        public RTHandleSystem.RTHandle GetDepthTexture()
        {
            if(m_CopyRequired)
            {
                return m_CameraDepthBufferCopy;
            }
            else
            {
                return m_CameraDepthStencilBuffer;
            }
        }

        public RTHandleSystem.RTHandle GetDepthValuesTexture()
        {
            Debug.Assert(m_MSAASupported);
            return m_CameraDepthValuesBuffer;
        }

        public RTHandleSystem.RTHandle GetStencilBufferCopy()
        {
            return m_CameraStencilBufferCopy;
        }

        public RTHandleSystem.RTHandle GetDepthAsColorBufferMSAA()
        {
            Debug.Assert(m_MSAASupported);
            return m_DepthAsColorMSAART;
        }

        public void Build(HDRenderPipelineAsset hdAsset)
        {
        }

        public void Cleanup()
        {
            if (!m_ExternalBuffer)
            {
                RTHandles.Release(m_NormalRT);
                if (m_MSAASupported)
                {
                    RTHandles.Release(m_NormalMSAART);
                    RTHandles.Release(m_DepthAsColorMSAART);
                }
            }

            RTHandles.Release(m_CameraDepthStencilBuffer);
            RTHandles.Release(m_CameraDepthBufferCopy);
            RTHandles.Release(m_CameraStencilBufferCopy);

            if (m_MSAASupported)
            {
                RTHandles.Release(m_CameraDepthStencilMSAABuffer);
                RTHandles.Release(m_CameraDepthValuesBuffer);
            }
        }

        public static int SampleCountToPassIndex(MSAASamples samples)
        {
            switch (samples)
            {
                case MSAASamples.None:
                    return 0;
                case MSAASamples.MSAA2x:
                    return 1;
                case MSAASamples.MSAA4x:
                    return 2;
                case MSAASamples.MSAA8x:
                    return 3;
            };
            return 0;
        }

        public void BindNormalBuffer(CommandBuffer cmd, bool isMSAA)
        {
            // NormalBuffer can be access in forward shader, so need to set global texture
            cmd.SetGlobalTexture(HDShaderIDs._NormalBufferTexture[0], GetNormalBuffer(isMSAA));
        }

        public void ResolveSharedRT(CommandBuffer cmd, HDCamera hdCamera, FrameSettings frameSettings)
        {
            if (frameSettings.enableMSAA)
            {
                Debug.Assert(m_MSAASupported);
                using (new ProfilingSample(cmd, "ComputeDepthValues", CustomSamplerId.VolumeUpdate.GetSampler()))
                {
                    // Grab the RTIs and set the output render targets
                    m_MSAARTIDs[0] = m_CameraDepthValuesBuffer.nameID;
                    m_MSAARTIDs[1] = m_NormalRT.nameID;
                    HDUtils.SetRenderTarget(cmd, hdCamera, m_MSAARTIDs, m_CameraDepthStencilBuffer);

                    // Set the input textures
                    Shader.SetGlobalTexture(HDShaderIDs._NormalTextureMS, m_NormalMSAART);
                    Shader.SetGlobalTexture(HDShaderIDs._DepthTextureMS, m_DepthAsColorMSAART);

                    // Resolve the depth and normal buffers
                    cmd.DrawProcedural(Matrix4x4.identity, m_DepthResolveMaterial, SampleCountToPassIndex(frameSettings.msaaSampleCount), MeshTopology.Triangles, 3, 1);
                }
            }
        }
        public void ResolveMSAAIntoNonMSAA(CommandBuffer cmd, HDCamera hdCamera, FrameSettings frameSettings, RTHandleSystem.RTHandle msaaTarget, RTHandleSystem.RTHandle simpleTarget)
        {
            Debug.Assert(m_MSAASupported);
            if (frameSettings.enableMSAA)
            {
                Debug.Assert(m_MSAASupported);
                using (new ProfilingSample(cmd, "ResolveColor", CustomSamplerId.VolumeUpdate.GetSampler()))
                {
                    // Grab the RTIs and set the output render targets
                    HDUtils.SetRenderTarget(cmd, hdCamera, simpleTarget);

                    MaterialPropertyBlock block =  new MaterialPropertyBlock();

                    // Set the input textures
                    block.SetTexture(HDShaderIDs._ColorTextureMS, msaaTarget);

                    // Resolve the depth and normal buffers
                    cmd.DrawProcedural(Matrix4x4.identity, m_ColorResolveMaterial, SampleCountToPassIndex(frameSettings.msaaSampleCount), MeshTopology.Triangles, 3, 1, block);
                }
            }
        }
    }
}
