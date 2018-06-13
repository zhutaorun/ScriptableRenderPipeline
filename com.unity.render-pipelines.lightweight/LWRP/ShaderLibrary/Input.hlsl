#ifndef LIGHTWEIGHT_INPUT_INCLUDED
#define LIGHTWEIGHT_INPUT_INCLUDED

#define MAX_VISIBLE_LIGHTS 16

// Must match check of use compute buffer in LightweightPipeline.cs
// GLES check here because of WebGL 1.0 support
// TODO: check performance of using StructuredBuffer on mobile as well
#if defined(SHADER_API_MOBILE) || defined(SHADER_API_GLES) || defined(SHADER_API_GLCORE)
#define USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA 0
#else
#define USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA 1
#endif

struct InputData
{
    float3  positionWS;
    half3   normalWS;
    half3   viewDirectionWS;
    float4  shadowCoord;
    half    fogCoord;
    half3   vertexLighting;
    half3   bakedGI;
};

///////////////////////////////////////////////////////////////////////////////
//                      Constant Buffers                                     //
///////////////////////////////////////////////////////////////////////////////

CBUFFER_START(_PerFrame)
half4 _GlossyEnvironmentColor;
half4 _SubtractiveShadowColor;
CBUFFER_END

CBUFFER_START(_PerCamera)
float4 _MainLightPosition;
half4 _MainLightColor;
float4x4 _WorldToLight;

half4 _AdditionalLightCount;
float4 _AdditionalLightPosition[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightColor[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightDistanceAttenuation[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightSpotDir[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightSpotAttenuation[MAX_VISIBLE_LIGHTS];

float4 _ScaledScreenParams;
CBUFFER_END

#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
StructuredBuffer<int> _LightIndexBuffer;
#endif

#include "CoreRP/ShaderLibrary/CommonMatrices.hlsl"
#include "InputBuiltin.hlsl"
#include "CoreRP/ShaderLibrary/UnityInstancing.hlsl"
#include "CoreFunctions.hlsl"

#endif
