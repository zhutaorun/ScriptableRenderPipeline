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
                    dimension = TextureDimension.Cube
                };
                return rt;
            }
            if (planar != null)
                throw new NotImplementedException();
            throw new ArgumentException();
        }
    }
}
