#ifndef SUBSURFACE_SCATTERING
#define SUBSURFACE_SCATTERING

#include "Lighting.hlsl"

TEXTURE2D(_PreintegratedDiffuseScatteringTex);
SAMPLER(sampler_PreintegratedDiffuseScatteringTex);

TEXTURE2D(_PreintegratedShadowScatteringTex);
SAMPLER(sampler_PreintegratedShadowScatteringTex);

// This function can be precomputed for efficiency
float3 T(float s) {
  return float3(0.233, 0.455, 0.649) * exp(s / 0.0064) +
         float3(0.1,   0.336, 0.344) * exp(s / 0.0484) +
         float3(0.118, 0.198, 0.0)   * exp(s / 0.187)  +
         float3(0.113, 0.007, 0.007) * exp(s / 0.567)  +
         float3(0.358, 0.004, 0.0)   * exp(s / 1.99)   +
         float3(0.078, 0.0,   0.0)   * exp(s / 7.41);
}

float4 _ShadowBiasForTransmission[4];
half3 Transmittance(float3 positionWS, float3 normalWS, float3 albedo, Light light)
{
    //TODO: Normal bias here?
    positionWS = positionWS - 0.025 * normalWS;

    //Calculate the distance traveled through the media.
    half cascadeIndex = ComputeCascadeIndex(positionWS);
    float4 shadowCoord = mul(_WorldToShadow[cascadeIndex], float4(positionWS, 1.0));

    //NOTE: Need to counteract the bias done during shadowmap construction, to properly calculate thickness
    shadowCoord.z += _ShadowBiasForTransmission[cascadeIndex].x;
    
    half d1 = SAMPLE_TEXTURE2D(_ShadowMap, sampler_ShadowMap, shadowCoord.xyz);
    half d2 = shadowCoord.z;

    float translucency = 0.001;
    float scale = 8.25 * (1.0 - translucency) / 0.005;
    float s = abs(d1 - d2) * scale;

    float irradiance = max(0.3 + dot(-normalWS, light.direction), 0.0);

    return T(-s * s) * irradiance * light.color;
}


half3 ScatterShadow(float shadow, float width)
{
    return SAMPLE_TEXTURE2D(_PreintegratedShadowScatteringTex, sampler_PreintegratedShadowScatteringTex, float2(shadow, width));
}

/*
half3 DiffuseScattering(BRDFData brdfData, half3 normalHighWS, half3 normalLowWS, half3 lightDirectionWS)
{
    float c = 1.0 - float3(0.5, 0.3, 0.22); //TODO: BRDFData?
    float3 rN = lerp(normalHighWS, normalLowWS, c.r);
    float3 gN = lerp(normalHighWS, normalLowWS, c.g);
    float3 bN = lerp(normalHighWS, normalLowWS, c.b);

    float3 NdotL = float3(dot(rN, lightDirectionWS),
                          dot(gN, lightDirectionWS),
                          dot(bN, lightDirectionWS));
    NdotL = 0.5 * NdotL + 0.5; //Scale to 0..1 for lookup.

    float3 scatteredDiffuse;
    scatteredDiffuse.r = SAMPLE_TEXTURE2D(_PreintegratedDiffuseScatteringTex, sampler_PreintegratedDiffuseScatteringTex, float2(NdotL.r, curvature)).r;
    scatteredDiffuse.g = SAMPLE_TEXTURE2D(_PreintegratedDiffuseScatteringTex, sampler_PreintegratedDiffuseScatteringTex, float2(NdotL.g, curvature)).g;
    scatteredDiffuse.b = SAMPLE_TEXTURE2D(_PreintegratedDiffuseScatteringTex, sampler_PreintegratedDiffuseScatteringTex, float2(NdotL.b, curvature)).b;
    return scatteredDiffuse;
}
*/

half3 DiffuseScattering()
{
    return half3(1, 0, 0);
}

half3 SubsurfaceScatteringTest(InputData input, float curvature, half3 normalLowWS)
{
    Light light = GetMainLight(input.positionWS);

    float MainNdotL = saturate(dot(input.normalWS, light.direction));

    float3 c = 1 - pow(float3(0.4, 0.25, 0.2), 2.2);

    //TODO: Only when there are normal maps.
    //TODO: The second mipped normal needs to be clamped, to prevent aliasing at further distances.
    float3 rN = lerp(input.normalWS, normalLowWS, c.r);
    float3 gN = lerp(input.normalWS, normalLowWS, c.g);
    float3 bN = lerp(input.normalWS, normalLowWS, c.b);
    
    float3 NdotL = float3(dot(rN, light.direction), dot(gN, light.direction), dot(bN, light.direction)); 

    //Fetch the preintegrated diffuse scattering.
    float3 lookup = NdotL * 0.5 + 0.5;
    
    float3 diffuse;
    diffuse.r = SAMPLE_TEXTURE2D(_PreintegratedDiffuseScatteringTex, sampler_PreintegratedDiffuseScatteringTex, float2(lookup.r, curvature)).r;
    diffuse.g = SAMPLE_TEXTURE2D(_PreintegratedDiffuseScatteringTex, sampler_PreintegratedDiffuseScatteringTex, float2(lookup.g, curvature)).g;
    diffuse.b = SAMPLE_TEXTURE2D(_PreintegratedDiffuseScatteringTex, sampler_PreintegratedDiffuseScatteringTex, float2(lookup.b, curvature)).b;

#ifdef _SHADOWS_ENABLED
    //half3 shadow = RealtimeShadowAttenuation(input.shadowCoord);

    //NOTE: At the moment, we are looking up into the preintegrated diffuse scattering with our penumbra, but really should be using a second LUT based on our penumbra.
    half3 shadow = SAMPLE_TEXTURE2D(_PreintegratedDiffuseScatteringTex, sampler_PreintegratedDiffuseScatteringTex, float2(RealtimeShadowAttenuation(input.shadowCoord), 0.5 * MainNdotL + 0.5));
#else
    half3 shadow = 1;
#endif
    return (diffuse * light.color * shadow);
}
*/

#endif