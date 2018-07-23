#ifndef UNITY_GRAPHFUNCTIONS_LW_INCLUDED
#define UNITY_GRAPHFUNCTIONS_LW_INCLUDED

#define SHADERGRAPH_SAMPLE_SCENE_DEPTH(uv) shadergraph_LWSampleSceneDepth(uv);
#define SHADERGRAPH_SAMPLE_SCENE_COLOR(uv) shadergraph_LWSampleSceneColor(uv);
#define SHADERGRAPH_MAIN_LIGHT(attenuation, direction, color) shadergraph_LWMainLight(attenuation, direction, color);
#define SHADERGRAPH_ADDITIONAL_LIGHT(index, positionWS, attenuation, direction, color) shadergraph_LWAdditionalLight(index, positionWS, attenuation, direction, color);
#define SHADERGRAPH_BAKED_GI(positionWS, normalWS, uvStaticLightmap, uvDynamicLightmap) shadergraph_LWSampleBakedGI(positionWS, normalWS, uvStaticLightmap, uvDynamicLightmap)

#if defined(REQUIRE_DEPTH_TEXTURE)
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    TEXTURE2D_ARRAY(_CameraDepthTexture);
#else
    TEXTURE2D(_CameraDepthTexture);
#endif
    SAMPLER(sampler_CameraDepthTexture);
#endif // REQUIRE_DEPTH_TEXTURE

#if defined(REQUIRE_OPAQUE_TEXTURE)
    TEXTURE2D(_CameraOpaqueTexture);
    SAMPLER(sampler_CameraOpaqueTexture);
#endif // REQUIRE_OPAQUE_TEXTURE

float shadergraph_LWSampleSceneDepth(float2 uv)
{
#if defined(REQUIRE_DEPTH_TEXTURE)
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    float rawDepth = SAMPLE_TEXTURE2D_ARRAY(_CameraDepthTexture, sampler_CameraDepthTexture, i.texcoord.xy, unity_StereoEyeIndex).r;
#else
    float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv);
#endif
	return Linear01Depth(rawDepth, _ZBufferParams);
#endif // REQUIRE_DEPTH_TEXTURE
    return 0;
}

float3 shadergraph_LWSampleSceneColor(float2 uv)
{
#if defined(REQUIRE_OPAQUE_TEXTURE)
    return SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv);
#endif
    return 0;
}

void shadergraph_LWMainLight(out float attenutation, out float3 direction, out float3 color)
{
    Light light = GetMainLight();
    attenutation = light.attenuation;
    direction = light.direction;
    color = light.color;
}

void shadergraph_LWAdditionalLight(float index, float3 positionWS, out float attenutation, out float3 direction, out float3 color)
{
    Light light = GetLight(index, positionWS);
    attenutation = light.attenuation;
    direction = light.direction;
    color = light.color;
}

float3 shadergraph_LWSampleBakedGI(float3 positionWS, float3 normalWS, float2 uvStaticLightmap, float2 uvDynamicLightmap)
{
#ifdef LIGHTMAP_ON
    return SampleLightmap(uvStaticLightmap, normalWS)
#else
    return SampleSH(normalWS)
#endif
}

// Always include Shader Graph version
// Always include last to avoid double macros
#include "ShaderGraphLibrary/Functions.hlsl" 

#endif // UNITY_GRAPHFUNCTIONS_LW_INCLUDED
