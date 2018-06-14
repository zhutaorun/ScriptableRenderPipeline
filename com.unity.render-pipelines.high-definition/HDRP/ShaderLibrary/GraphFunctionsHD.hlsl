#ifndef UNITY_GRAPHFUNCTIONS_HD_INCLUDED
#define UNITY_GRAPHFUNCTIONS_HD_INCLUDED

#define SHADERGRAPH_SAMPLE_SCENE_DEPTH(uv) shadergraph_HDSampleSceneDepth(uv);
#define SHADERGRAPH_SAMPLE_BAKED_GI(normal) shadergraph_HDSampleBakedGI(normal);

float shadergraph_HDSampleSceneDepth(float2 uv)
{
#if defined(REQUIRE_DEPTH_TEXTURE) || defined(REQUIRE_DEPTH_TEXTURE_FLOAT)
    float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv);
	return Linear01Depth(rawDepth, _ZBufferParams);
#endif
    return 0;
}

float shadergraph_HDSampleBakedGI(float3 normal)
{
    return 0;
    //return SampleBakedGI(float3 positionWS, float3 normalWS, float2 uvStaticLightmap, float2 uvDynamicLightmap);
}

// Always include CoreRP version
// Always include last to avoid double macros
#include "CoreRP/ShaderLibrary/GraphFunctions.hlsl" 

#endif // UNITY_GRAPHFUNCTIONS_HD_INCLUDED