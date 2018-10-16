using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public partial class Hair : RenderPipelineMaterial
    {
        [GenerateHLSL(PackingRules.Exact)]
        public enum MaterialFeatureFlags
        {
            HairKajiyaKay               = 1 << 0,
            HairSubsurfaceScattering    = 1 << 1,
            HairTransmission            = 1 << 2
        };

        //-----------------------------------------------------------------------------
        // SurfaceData
        //-----------------------------------------------------------------------------

        // Main structure that store the user data (i.e user input of master node in material graph)
        [GenerateHLSL(PackingRules.Exact, false, true, 1400)]
        public struct SurfaceData
        {
            [SurfaceDataAttributes("MaterialFeatures")]
            public uint materialFeatures;

            [SurfaceDataAttributes("Ambient Occlusion")]
            public float ambientOcclusion;

            [SurfaceDataAttributes("Specular Occlusion")]
            public float specularOcclusion;

            [SurfaceDataAttributes("Diffuse Color", false, true)]
            public Vector3 diffuseColor;

            [SurfaceDataAttributes(new string[] {"Normal", "Normal View Space"}, true)]
            public Vector3 normalWS;

            [SurfaceDataAttributes(new string[] { "Geometric Normal", "Geometric Normal View Space" }, true)]
            public Vector3 geomNormalWS;

            [SurfaceDataAttributes("Smoothness")]
            public float perceptualSmoothness;

            // SSS
            [SurfaceDataAttributes("Diffusion Profile")]
            public uint diffusionProfile;
            [SurfaceDataAttributes("Subsurface Mask")]
            public float subsurfaceMask;

            // Transmission
            // + Diffusion Profile
            [SurfaceDataAttributes("Thickness")]
            public float thickness;

            // Anisotropic
            [SurfaceDataAttributes("Tangent", true)]
            public Vector3 tangentWS;

            // Kajiya kay
            [SurfaceDataAttributes("Secondary Smoothness")]
            public float secondaryPerceptualSmoothness;

            // Specular Color
            [SurfaceDataAttributes("Specular Tint", false, true)]
            public Vector3 specularTint;

            [SurfaceDataAttributes("Secondary Specular Tint", false, true)]
            public Vector3 secondarySpecularTint;

            [SurfaceDataAttributes("Specular Shift")]
            public float specularShift;

            [SurfaceDataAttributes("Secondary Specular Shift")]
            public float secondarySpecularShift;
        };

        //-----------------------------------------------------------------------------
        // BSDFData
        //-----------------------------------------------------------------------------

        [GenerateHLSL(PackingRules.Exact, false, true, 1450)]
        public struct BSDFData
        {
            public uint materialFeatures;

            public float ambientOcclusion;
            public float specularOcclusion;

            [SurfaceDataAttributes("", false, true)]
            public Vector3 diffuseColor;
            public Vector3 fresnel0;

            [SurfaceDataAttributes(new string[] { "Normal WS", "Normal View Space" }, true)]
            public Vector3 normalWS;

            public Vector3 geomNormalWS;

            public float perceptualRoughness;

            // SSS
            public uint diffusionProfile;
            public float subsurfaceMask;

            // Transmission
            // + Diffusion Profile
            public float thickness;
            public bool useThickObjectMode; // Read from the diffusion profile
            public Vector3 transmittance;   // Precomputation of transmittance

            // Anisotropic
            [SurfaceDataAttributes("", true)]
            public Vector3 tangentWS;
            [SurfaceDataAttributes("", true)]
            public Vector3 bitangentWS;

            // Kajiya kay
            public float secondaryPerceptualSmoothness;
            public Vector3 secondarySpecularTint;
            public float specularExponent;
            public float secondarySpecularExponent;
            public float specularShift;
            public float secondarySpecularShift;
        };

        //-----------------------------------------------------------------------------
        // GBuffer management
        //-----------------------------------------------------------------------------

        public override bool IsDefferedMaterial() { return true; }

        protected void GetGBufferOptions(HDRenderPipelineAsset asset, out int gBufferCount, out bool supportShadowMask, out bool supportLightLayers)
        {
            // Caution: This must be in sync with GBUFFERMATERIAL_COUNT definition in 
            supportShadowMask = asset.renderPipelineSettings.supportShadowMask;
            supportLightLayers = asset.renderPipelineSettings.supportLightLayers;
            gBufferCount = 4 + (supportShadowMask ? 1 : 0) + (supportLightLayers ? 1 : 0);
        }

        // This must return the number of GBuffer to allocate
        public override int GetMaterialGBufferCount(HDRenderPipelineAsset asset)
        {
            int gBufferCount;
            bool unused0;
            bool unused1;
            GetGBufferOptions(asset, out gBufferCount, out unused0, out unused1);

            return gBufferCount;
        }

        public override void GetMaterialGBufferDescription(HDRenderPipelineAsset asset, out RenderTextureFormat[] RTFormat, out bool[] sRGBFlag, out GBufferUsage[] gBufferUsage, out bool[] enableWrite)
        {
            int gBufferCount;
            bool supportShadowMask;
            bool supportLightLayers;
            GetGBufferOptions(asset, out gBufferCount, out supportShadowMask, out supportLightLayers);

            RTFormat = new RenderTextureFormat[gBufferCount];
            sRGBFlag = new bool[gBufferCount];
            gBufferUsage = new GBufferUsage[gBufferCount];
            enableWrite = new bool[gBufferCount];

            RTFormat[0] = RenderTextureFormat.ARGB32; // Albedo sRGB / SSSBuffer
            sRGBFlag[0] = true;
            gBufferUsage[0] = GBufferUsage.SubsurfaceScattering;
            enableWrite[0] = false;
            RTFormat[1] = RenderTextureFormat.ARGB32; // Normal Buffer
            sRGBFlag[1] = false;
            gBufferUsage[1] = GBufferUsage.Normal;
            enableWrite[1] = true;                    // normal buffer is used as RWTexture to composite decals in forward
            RTFormat[2] = RenderTextureFormat.ARGB32; // Data
            sRGBFlag[2] = false;
            gBufferUsage[2] = GBufferUsage.None;
            enableWrite[2] = false;
            RTFormat[3] = Builtin.GetLightingBufferFormat();
            sRGBFlag[3] = Builtin.GetLightingBufferSRGBFlag();
            gBufferUsage[3] = GBufferUsage.None;
            enableWrite[3] = false;

            int index = 4;

            if (supportLightLayers)
            {
                RTFormat[index] = RenderTextureFormat.ARGB32;
                sRGBFlag[index] = false;
                gBufferUsage[index] = GBufferUsage.LightLayers;
                index++;
            }

            // All buffer above are fixed. However shadow mask buffer can be setup or not depends on light in view.
            // Thus it need to be the last one, so all indexes stay the same
            if (supportShadowMask)
            {
                RTFormat[index] = Builtin.GetShadowMaskBufferFormat();
                sRGBFlag[index] = Builtin.GetShadowMaskBufferSRGBFlag();
                gBufferUsage[index] = GBufferUsage.ShadowMask;
                index++;
            }
        }


        //-----------------------------------------------------------------------------
        // Init precomputed texture
        //-----------------------------------------------------------------------------

        public Hair() {}

        public override void Build(HDRenderPipelineAsset hdAsset)
        {
            PreIntegratedFGD.instance.Build(PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse);
            LTCAreaLight.instance.Build();
        }

        public override void Cleanup()
        {
            PreIntegratedFGD.instance.Cleanup(PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse);
            LTCAreaLight.instance.Cleanup();
        }

        public override void RenderInit(CommandBuffer cmd)
        {
            PreIntegratedFGD.instance.RenderInit(PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse, cmd);
        }

        public override void Bind()
        {
            PreIntegratedFGD.instance.Bind(PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse);
            LTCAreaLight.instance.Bind();
        }
    }
}
