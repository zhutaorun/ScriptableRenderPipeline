using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    internal static class HDProbeRendererUtilities
    {
        const GraphicsFormat k_CubemapFormat = GraphicsFormat.R32G32B32A32_SFloat;

        internal static Texture CreateRenderTarget(HDProbe probe)
        {
            var standard = probe as HDReflectionProbe;
            var planar = probe as HDPlanarProbe;
            if (standard != null)
            {
                var c = standard.captureSettings;
                var rt = new RenderTexture(c.cubemapSize, c.cubemapSize, 1, k_CubemapFormat)
                {
                    dimension = UnityEngine.Rendering.TextureDimension.Cube
                };
                return rt;
            }
            if (planar != null)
                throw new NotImplementedException();
            throw new ArgumentException();
        }
    }
}
