#ifndef LIGHTWEIGHT_PASS_DEPTH_ONLY_INCLUDED
#define LIGHTWEIGHT_PASS_DEPTH_ONLY_INCLUDED

#include "LWRP/ShaderLibrary/Core.hlsl"

struct VertexInput
{
    float4 position     : POSITION;
    float2 texcoord     : TEXCOORD0;
#ifdef _VEGETATION
    float3 normal : NORMAL;
    float4 color : COLOR;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float2 uv           : TEXCOORD0;
    float4 clipPos      : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

VertexOutput DepthOnlyVertex(VertexInput v)
{
    VertexOutput o = (VertexOutput)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

#ifdef _VOLUME_WIND
#ifdef _VEGETATION
    float3 objectOrigin = UNITY_ACCESS_INSTANCED_PROP(Props, _Position);
    v.position.xyz = VegetationDeformation(v.position.xyz, objectOrigin, v.normal, v.color.x, v.color.z, v.color.y);
#endif
#endif

    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
    o.clipPos = TransformObjectToHClip(v.position.xyz);
    return o;
}

half4 DepthOnlyFragment(VertexOutput IN) : SV_TARGET
{
    Alpha(SampleAlbedoAlpha(IN.uv, TEXTURE2D_PARAM(_MainTex, sampler_MainTex)).a, _Color, _Cutoff);
    return 0;
}
#endif
