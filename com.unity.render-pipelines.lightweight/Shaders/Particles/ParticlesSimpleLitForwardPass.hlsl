#ifndef LIGHTWEIGHT_PARTICLES_FORWARD_SIMPLE_LIT_PASS_INCLUDED
#define LIGHTWEIGHT_PARTICLES_FORWARD_SIMPLE_LIT_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Particles.hlsl"

struct AttributesParticle
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    half4 color : COLOR;
#if defined(_REQUIRE_UV2) && !defined(UNITY_PARTICLE_INSTANCING_ENABLED)
    float4 texcoords : TEXCOORD0;
    float texcoordBlend : TEXCOORD1;
#else
    float2 texcoords : TEXCOORD0;
#endif
    float4 tangent : TANGENT;
};

struct VaryingsParticle
{
    half4 color                     : COLOR;
    float2 texcoord                 : TEXCOORD0;
    
    float4 positionWS               : TEXCOORD1;

#ifdef _NORMALMAP
    half4 normalWS                  : TEXCOORD2;    // xyz: normal, w: viewDir.x
    half4 tangentWS                 : TEXCOORD3;    // xyz: tangent, w: viewDir.y
    half4 bitangentWS               : TEXCOORD4;    // xyz: bitangent, w: viewDir.z
#else
    half3 normalWS                  : TEXCOORD2;
    half3 viewDirWS                 : TEXCOORD3;
#endif

#if defined(_REQUIRE_UV2)
    float3 texcoord2AndBlend        : TEXCOORD5;
#endif
#if defined(SOFTPARTICLES_ON) || defined(_FADING_ON)
    float4 projectedPosition        : TEXCOORD6;
#endif

#if defined(_MAIN_LIGHT_SHADOWS) && !defined(_RECEIVE_SHADOWS_OFF)
    float4 shadowCoord              : TEXCOORD7;
#endif

    float3 vertexSH                 : TEXCOORD8; // SH
    float4 clipPos                  : SV_POSITION;
};

void InitializeInputData(VaryingsParticle input, half3 normalTS, out InputData output)
{
    output = (InputData)0;

    output.positionWS = input.positionWS.xyz;

#ifdef _NORMALMAP
    half3 viewDirWS = half3(input.normalWS.w, input.tangentWS.w, input.bitangentWS.w);
    output.normalWS = TransformTangentToWorld(normalTS,
        half3x3(input.tangentWS.xyz, input.bitangentWS.xyz, input.normalWS.xyz));
#else
    half3 viewDirWS = input.viewDirWS;
    output.normalWS = input.normalWS;
#endif

    output.normalWS = NormalizeNormalPerPixel(output.normalWS);
    
#if SHADER_HINT_NICE_QUALITY
    viewDirWS = SafeNormalize(viewDirWS);
#endif

    output.viewDirectionWS = viewDirWS;
    
#if defined(_MAIN_LIGHT_SHADOWS) && !defined(_RECEIVE_SHADOWS_OFF)
    output.shadowCoord = input.shadowCoord;
#else
    output.shadowCoord = float4(0, 0, 0, 0);
#endif

    output.fogCoord = (half)input.positionWS.w;
    output.vertexLighting = half3(0.0h, 0.0h, 0.0h);
    output.bakedGI = SampleSHPixel(input.vertexSH, output.normalWS);
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

VaryingsParticle ParticlesLitVertex(AttributesParticle input)
{
    VaryingsParticle output;

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.vertex.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normal, input.tangent);
    half3 viewDirWS = GetCameraPositionWS() - vertexInput.positionWS;
#if !SHADER_HINT_NICE_QUALITY
    viewDirWS = SafeNormalize(viewDirWS);
#endif

#ifdef _NORMALMAP
    output.normalWS = half4(normalInput.normalWS, viewDirWS.x);
    output.tangentWS = half4(normalInput.tangentWS, viewDirWS.y);
    output.bitangentWS = half4(normalInput.bitangentWS, viewDirWS.z);
#else
    output.normalWS = normalInput.normalWS;
    output.viewDirWS = viewDirWS;
#endif

    OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

    output.positionWS.xyz = vertexInput.positionWS.xyz;
    output.positionWS.w = ComputeFogFactor(vertexInput.positionCS.z);
    output.clipPos = vertexInput.positionCS;
    output.color = input.color;

    // TODO: Instancing
    // vertColor(output.color);
    
    output.texcoord = input.texcoords.xy;
#ifdef _REQUIRE_UV2
    output.texcoord2AndBlend.xy = input.texcoords.zw;
    output.texcoord2AndBlend.z = input.texcoordBlend;
#endif

#if defined(SOFTPARTICLES_ON) || defined(_FADING_ON)
    output.projectedPosition = ComputeScreenPos(vertexInput.positionCS);
#endif

#if defined(_MAIN_LIGHT_SHADOWS) && !defined(_RECEIVE_SHADOWS_OFF)
    output.shadowCoord = GetShadowCoord(vertexInput);
#endif

    return output;
}

half4 ParticlesLitFragment(VaryingsParticle input) : SV_Target
{
    float2 uv = input.texcoord;
    float3 blendUv = float3(0, 0, 0);
#if defined(_REQUIRE_UV2)
    blendUv = input.texcoord2AndBlend;
#endif

    float4 projectedPosition = float4(0,0,0,0);
#if defined(SOFTPARTICLES_ON) || defined(_FADING_ON)
    projectedPosition = input.projectedPosition;
#endif

    half3 normalTS = SampleNormalTS(uv, blendUv, TEXTURE2D_PARAM(_BumpMap, sampler_BumpMap));
    half4 albedo = SampleAlbedo(uv, blendUv, _BaseColor, input.color, projectedPosition, TEXTURE2D_PARAM(_BaseMap, sampler_BaseMap));
    half3 diffuse = AlphaModulate(albedo.rgb, albedo.a);
    half alpha = AlphaBlendAndTest(albedo.a, _Cutoff);
#if defined(_EMISSION)
    half3 emission = BlendTexture(TEXTURE2D_PARAM(_EmissionMap, sampler_EmissionMap), uv, blendUv) * _EmissionColor.rgb;
#else
    half3 emission = half3(0, 0, 0);
#endif
    half4 specularGloss = SampleSpecularGloss(uv, blendUv, albedo.a, _SpecColor, TEXTURE2D_PARAM(_SpecGlossMap, sampler_SpecGlossMap));
    half shininess = 0.5;
    
#if defined(_DISTORTION_ON)
    diffuse = Distortion(half4(diffuse, alpha), normalTS, _DistortionStrengthScaled, _DistortionBlend, projectedPosition);
#endif

    InputData inputData;
    InitializeInputData(input, normalTS, inputData);

    half4 color = LightweightFragmentBlinnPhong(inputData, diffuse, specularGloss, shininess, emission, alpha);

    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    return color;
}

#endif // LIGHTWEIGHT_PARTICLES_FORWARD_SIMPLE_LIT_PASS_INCLUDED
