using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    internal static class HDProbeRendererUtilities
    {
        const GraphicsFormat k_CubemapFormat = GraphicsFormat.R32G32B32A32_SFloat;

        internal static Texture CreateRenderTarget(HDProbe probe)
        {
            var standard = probe as HDAdditionalReflectionData;
            var planar = probe as PlanarReflectionProbe;
            if (standard != null)
            {
                var c = standard.probeCaptureProperties;
                var cubemapSize = (int)((HDRenderPipelineAsset)GraphicsSettings.renderPipelineAsset)
                        .renderPipelineSettings
                        .lightLoopSettings
                        .reflectionCubemapSize;
                var rt = new RenderTexture(cubemapSize, cubemapSize, 1, k_CubemapFormat)
                {
                    dimension = TextureDimension.Cube,
                    enableRandomWrite = true,
                    autoGenerateMips = false,
                    useMipMap = false,
                    name  = "Render Target For " + probe.name
                };
                rt.Create();
                return rt;
            }

            if (planar != null)
            {
                var c = planar.probeCaptureProperties;
                var textureSize = (int)((HDRenderPipelineAsset)GraphicsSettings.renderPipelineAsset)
                    .renderPipelineSettings
                    .lightLoopSettings
                    .planarReflectionTextureSize;
                var rt = new RenderTexture(textureSize, textureSize, 1, k_CubemapFormat)
                {
                    dimension = TextureDimension.Tex2D,
                    enableRandomWrite = true,
                    autoGenerateMips = false,
                    useMipMap = false,
                    name = "Render Target For " + probe.name
                };
                rt.Create();
                return rt;
            }
                
            throw new ArgumentException();
        }
    }
}
