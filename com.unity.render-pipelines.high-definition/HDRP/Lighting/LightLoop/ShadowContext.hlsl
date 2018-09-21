#ifndef LIGHTLOOP_SHADOW_CONTEXT_HLSL
#define LIGHTLOOP_SHADOW_CONTEXT_HLSL

#define SHADOWCONTEXT_MAX_TEX2DARRAY   4
#define SHADOWCONTEXT_MAX_TEXCUBEARRAY 0
#define SHADOWCONTEXT_MAX_SAMPLER      3
#define SHADOWCONTEXT_MAX_COMPSAMPLER  1
#define SHADOW_OPTIMIZE_REGISTER_USAGE 1

#include "CoreRP/ShaderLibrary/Shadow/Shadow.hlsl"

#if SHADOWCONTEXT_MAX_TEX2DARRAY == 4
TEXTURE2D_ARRAY(_ShadowmapExp_VSM_0);
SAMPLER(sampler_ShadowmapExp_VSM_0);

TEXTURE2D_ARRAY(_ShadowmapExp_VSM_1);
SAMPLER(sampler_ShadowmapExp_VSM_1);

TEXTURE2D_ARRAY(_ShadowmapExp_VSM_2);
SAMPLER(sampler_ShadowmapExp_VSM_2);
#endif

#ifdef SHADOW_USE_PREFILTERED_SHADOWS
TEXTURE2D_ARRAY(_Shadowmap_EVSM);
    // Reuse 's_trilinear_clamp_sampler' to avoid SGPR explosion.
#endif

TEXTURE2D_ARRAY(_ShadowmapExp_PCF);
SAMPLER_CMP(sampler_ShadowmapExp_PCF);

StructuredBuffer<ShadowData>    _ShadowDatasExp;
StructuredBuffer<int4>          _ShadowPayloads;

// Currently we only use the PCF atlas.
// Keeping all other bindings for reference and for future PC dynamic shadow configuration as it's harmless anyway.
ShadowContext InitShadowContext()
{
    ShadowContext sc;
    sc.shadowDatas     = _ShadowDatasExp;
    sc.payloads        = _ShadowPayloads;
    sc.tex2DArray[0]   = _ShadowmapExp_PCF;
    sc.compSamplers[0] = sampler_ShadowmapExp_PCF;
#if SHADOWCONTEXT_MAX_TEX2DARRAY == 4
#ifdef SHADOW_USE_PREFILTERED_SHADOWS
    sc.tex2DArray[1]   = _Shadowmap_EVSM;
#else
    sc.tex2DArray[1]   = _ShadowmapExp_VSM_0;
#endif
    sc.tex2DArray[2]   = _ShadowmapExp_VSM_1;
    sc.tex2DArray[3]   = _ShadowmapExp_VSM_2;
#endif
#if SHADOWCONTEXT_MAX_SAMPLER == 3
#ifdef SHADOW_USE_PREFILTERED_SHADOWS
    sc.samplers[0]     = s_trilinear_clamp_sampler;
#else
    sc.samplers[0]     = sampler_ShadowmapExp_VSM_0;
#endif
    sc.samplers[1]     = sampler_ShadowmapExp_VSM_1;
    sc.samplers[2]     = sampler_ShadowmapExp_VSM_2;
#endif
    return sc;
}

#endif // LIGHTLOOP_SHADOW_CONTEXT_HLSL
