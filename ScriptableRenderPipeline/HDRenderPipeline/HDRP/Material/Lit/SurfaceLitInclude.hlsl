#include "HDRP/Material/Lit/Lit.cs.hlsl"

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.Lit+SurfaceData
// PackingRules = Exact
/*
struct SurfaceData
{
uint materialFeatures;
float3 baseColor;
float specularOcclusion;
float3 normalWS;
float perceptualSmoothness;
float ambientOcclusion;
float metallic;
float coatMask;
float3 specularColor;
uint diffusionProfile;
float subsurfaceMask;
float thickness;
float3 tangentWS;
float anisotropy;
float iridescenceThickness;
float iridescenceMask;
float ior;
float3 transmittanceColor;
float atDistance;
float transmittanceMask;
};
*/

SurfaceData GetSurfaceLitData()
{
    SurfaceData s;

    ZERO_INITIALIZE(SurfaceData, s);

    //s.materialFeatures      = MATERIALFEATUREFLAGS_LIT_STANDARD;
    s.baseColor = float3(1, 0, 0);
    s.specularOcclusion = 0;
    s.normalWS = float3(0, 0, 1);
    s.perceptualSmoothness = 0.5;
    s.ambientOcclusion = 0;
    s.metallic = 0;
    s.coatMask = 0;
    s.specularColor = float3(0, 0, 0);
    s.diffusionProfile = 0;
    s.subsurfaceMask = 0;
    s.thickness = 0;
    s.tangentWS = float3(1, 0, 0);
    s.anisotropy = 0;
    s.iridescenceThickness = 0;
    s.iridescenceMask = 0;
    s.ior = 1;
    s.transmittanceColor = float3(0, 0, 0);
    s.atDistance = 0;
    s.transmittanceMask = 0;

    return s;
}

void ModSurfaceLitData(inout SurfaceData s)
{
    //s.materialFeatures      = MATERIALFEATUREFLAGS_LIT_STANDARD;
    s.baseColor = float3(1, 0, 0);

    //s.specularOcclusion = 0;
    //s.normalWS = float3(0, 0, 1);
    s.perceptualSmoothness = 0.5;
    //s.ambientOcclusion = 0;
    //s.metallic = 0;
    //s.coatMask = 0;
    //s.specularColor = float3(0, 0, 0);
    //s.diffusionProfile = 0;
    //s.subsurfaceMask = 0;
    //s.thickness = 0;
    //s.tangentWS = float3(1, 0, 0);
    //s.anisotropy = 0;
    //s.iridescenceThickness = 0;
    //s.iridescenceMask = 0;
    //s.ior = 1;
    //s.transmittanceColor = float3(0, 0, 0);
    //s.atDistance = 0;
    //s.transmittanceMask = 0;
}
