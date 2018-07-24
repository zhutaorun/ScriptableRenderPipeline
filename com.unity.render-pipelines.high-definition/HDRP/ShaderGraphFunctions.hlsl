#ifndef UNITY_GRAPHFUNCTIONS_HD_INCLUDED
#define UNITY_GRAPHFUNCTIONS_HD_INCLUDED

#define SHADERGRAPH_SAMPLE_SCENE_DEPTH(uv) shadergraph_HDSampleSceneDepth(uv);
#define SHADERGRAPH_SAMPLE_BAKED_GI(positionWS, normalWS, uvStaticLightmap, uvDynamicLightmap) shadergraph_HDSampleBakedGI(positionWS, normalWS, uvStaticLightmap, uvDynamicLightmap)
#define SHADERGRAPH_SAMPLE_SHADOWMASK(positionWS, uvStaticLightmap) shadergraph_HDSampleShadowmask(positionWS, uvStaticLightmap)

float shadergraph_HDSampleSceneDepth(float2 uv)
{
#if defined(REQUIRE_DEPTH_TEXTURE)
    float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv);
	return Linear01Depth(rawDepth, _ZBufferParams);
#endif
    return 0;
}

float3 shadergraph_HDSampleBakedGI(float3 positionWS, float3 normalWS, float2 uvStaticLightmap, float2 uvDynamicLightmap)
{
    float3 positionRWS = GetCameraRelativePositionWS(positionWS);
    return SampleBakedGI(positionRWS, normalWS, uvStaticLightmap, uvDynamicLightmap);
}

float4 shadergraph_HDSampleShadowmask(float3 positionWS float2 uvStaticLightmap)
{
    float3 positionRWS = GetCameraRelativePositionWS(positionWS);
    return SampleShadowMask(positionRWS, uvStaticLightmap);
}

// Always include Shader Graph version
// Always include last to avoid double macros
#include "ShaderGraphLibrary/Functions.hlsl" 

#endif // UNITY_GRAPHFUNCTIONS_HD_INCLUDED
