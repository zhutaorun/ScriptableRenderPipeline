#ifndef UNITY_GRAPHFUNCTIONS_LW_INCLUDED
#define UNITY_GRAPHFUNCTIONS_LW_INCLUDED

#define SHADERGRAPH_SAMPLE_SCENE_DEPTH(uv) shadergraph_LWSampleSceneDepth(uv);
#define SHADERGRAPH_SAMPLE_SCENE_COLOR(uv) shadergraph_LWSampleSceneColor(uv);

#if defined(REQUIRE_DEPTH_TEXTURE)
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    TEXTURE2D_ARRAY(_CameraDepthTexture);
#else
    TEXTURE2D(_CameraDepthTexture);
    SAMPLER(sampler_CameraDepthTexture);
#endif
#elif defined(REQUIRE_DEPTH_TEXTURE_FLOAT)
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    TEXTURE2D_ARRAY_FLOAT(_CameraDepthTexture);
#else
    TEXTURE2D_FLOAT(_CameraDepthTexture);
    SAMPLER(sampler_CameraDepthTexture);
#endif // REQUIRE_DEPTH_TEXTURE
#endif

#if defined(REQUIRE_OPAQUE_TEXTURE)
    TEXTURE2D(_CameraOpaqueTexture);
    SAMPLER(sampler_CameraOpaqueTexture);
#endif

float shadergraph_LWSampleSceneDepth(float2 uv)
{
#if defined(REQUIRE_DEPTH_TEXTURE) || defined(REQUIRE_DEPTH_TEXTURE_FLOAT)
    float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv);
	return Linear01Depth(rawDepth, _ZBufferParams);
#endif
    return 0;
}

float3 shadergraph_LWSampleSceneColor(float2 uv)
{
#if defined(REQUIRE_OPAQUE_TEXTURE)
    return SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv);
#endif
    return 0;
}

// Always include Shader Graph version
// Always include last to avoid double macros
#include "ShaderGraphLibrary/Functions.hlsl" 

#endif // UNITY_GRAPHFUNCTIONS_LW_INCLUDED
