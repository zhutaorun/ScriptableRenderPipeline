#ifndef LIGHTWEIGHT_PASS_LIT_INCLUDED
#define LIGHTWEIGHT_PASS_LIT_INCLUDED

#include "LWRP/ShaderLibrary/InputSurface.hlsl"
#include "LWRP/ShaderLibrary/Lighting.hlsl"

struct LightweightVertexInput
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
    float2 texcoord : TEXCOORD0;
    float2 lightmapUV : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct LightweightVertexOutput
{
    float2 uv                       : TEXCOORD0;
    DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);
    float3 posWS                    : TEXCOORD2;

#ifdef _NORMALMAP
    half4 normal                    : TEXCOORD3;    // xyz: normal, w: viewDir.x
    half4 tangent                   : TEXCOORD4;    // xyz: tangent, w: viewDir.y
    half4 binormal                  : TEXCOORD5;    // xyz: binormal, w: viewDir.z
#else
    half3 normal                    : TEXCOORD3;
    half3 viewDir                   : TEXCOORD6;
#endif

    half4 fogFactorAndVertexLight   : TEXCOORD7;    // x: fogFactor, yzw: vertex light

    float4 shadowCoord              : TEXCOORD8;

    float4 clipPos                  : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

struct LightweightVertexOutputSingleLight
{
    float2 uv                       : TEXCOORD0;
    DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);

    half3  normalWS                 : TEXCOORD2;
#ifdef _NORMALMAP
    half3 lightDirectionTS          : TEXCOORD3;
    half3 viewDirectionTS           : TEXCOORD4;
#else
    half3 viewDirectionWS           : TEXCOORD3;
#endif

    half4 fogFactorAndVertexLight   : TEXCOORD5; // x: fogFactor, yzw: vertex light
    float4 shadowCoord              : TEXCOORD6;
    float4 clipPos                  : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(LightweightVertexOutput IN, half3 normalTS, out InputData inputData)
{
    inputData.positionWS = IN.posWS.xyz;

#ifdef _NORMALMAP
    half3 viewDirection = half3(IN.normal.w, IN.tangent.w, IN.binormal.w);
    inputData.normal = TangentToWorldNormal(normalTS, IN.tangent.xyz, IN.binormal.xyz, IN.normal.xyz);
#else
    half3 viewDirection = IN.viewDir;
    #if !SHADER_HINT_NICE_QUALITY
        // World normal is already normalized in vertex. Small acceptable error to save ALU.
        inputData.normal = IN.normal;
    #else
        inputData.normal = normalize(IN.normal);
    #endif
#endif

#if SHADER_HINT_NICE_QUALITY
    inputData.viewDirection = SafeNormalize(viewDirection);
#else
    // View direction is already normalized in vertex. Small acceptable error to save ALU.
    inputData.viewDirection = viewDirection;
#endif

    inputData.shadowCoord = IN.shadowCoord;

    inputData.fogCoord = IN.fogFactorAndVertexLight.x;
    inputData.vertexLighting = IN.fogFactorAndVertexLight.yzw;
    inputData.bakedGI = SAMPLE_GI(IN.lightmapUV, IN.vertexSH, inputData.normal);
}

void InitializeInputDataSingleLight(LightweightVertexOutputSingleLight IN, half3 normalTS, out InputData inputData)
{
    // There's no distance attenuation or shadow fade. We don't use world position interpolator.
    inputData.positionWS = half3(0, 0, 0);

#ifdef _NORMALMAP
    half3 viewDirection = IN.viewDirectionTS;
    inputData.normal = normalTS;
#else
    half3 viewDirection = IN.viewDirectionWS;
    inputData.normal = normalize(IN.normalWS);
#endif

    inputData.viewDirection = SafeNormalize(viewDirection);

    inputData.shadowCoord = IN.shadowCoord;
    inputData.fogCoord = IN.fogFactorAndVertexLight.x;
    inputData.vertexLighting = IN.fogFactorAndVertexLight.yzw;
    inputData.bakedGI = SAMPLE_GI(IN.lightmapUV, IN.vertexSH, IN.normalWS);
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

// Vertex: Used in Standard (Physically Based) and Standard (Simple Lighting) shaders
LightweightVertexOutput LitPassVertex(LightweightVertexInput v)
{
    LightweightVertexOutput o = (LightweightVertexOutput)0;

    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);

    o.posWS = TransformObjectToWorld(v.vertex.xyz);
    o.clipPos = TransformWorldToHClip(o.posWS);

    half3 viewDir = GetCameraPositionWS() - o.posWS;
#if !SHADER_HINT_NICE_QUALITY
    // Normalize in vertex and avoid renormalizing it in frag to save ALU.
    viewDir = SafeNormalize(viewDir);
#endif

#ifdef _NORMALMAP
    o.normal.w = viewDir.x;
    o.tangent.w = viewDir.y;
    o.binormal.w = viewDir.z;
#else
    o.viewDir = viewDir;
#endif

    // initializes o.normal and if _NORMALMAP also o.tangent and o.binormal
    OUTPUT_NORMAL(v, o);

    // We either sample GI from lightmap or SH. lightmap UV and vertex SH coefficients
    // are packed in lightmapUVOrVertexSH to save interpolator.
    // The following funcions initialize
    OUTPUT_LIGHTMAP_UV(v.lightmapUV, unity_LightmapST, o.lightmapUV);
    OUTPUT_SH(o.normal.xyz, o.vertexSH);

    half3 vertexLight = VertexLighting(o.posWS, o.normal.xyz);
    half fogFactor = ComputeFogFactor(o.clipPos.z);
    o.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
    o.shadowCoord = ComputeShadowCoord(o.clipPos);

    return o;
}

// Vertex: Used in Standard (Simple Lighting - Single Directional) shader
LightweightVertexOutputSingleLight LitPassVertexSingleLight(LightweightVertexInput v)
{
    LightweightVertexOutputSingleLight o = (LightweightVertexOutputSingleLight)0;

    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);

    float3 posWS = TransformObjectToWorld(v.vertex.xyz);
    o.clipPos = TransformWorldToHClip(posWS);

    half3 viewDirection = normalize(GetCameraPositionWS() - posWS);

    o.normalWS = TransformObjectToWorldNormal(v.normal);
#ifdef _NORMALMAP
    half3 tangentWS = TransformObjectToWorldDir(v.tangent.xyz);
    half3x3 worldToTangent = CreateWorldToTangent(o.normalWS, tangentWS, v.tangent.w);
    o.lightDirectionTS = TransformWorldToTangent(_MainLightPosition.xyz, worldToTangent);
    o.viewDirectionTS = TransformWorldToTangent(viewDirection, worldToTangent);
#else

#if !SHADER_HINT_NICE_QUALITY
    // Normalize in vertex and avoid renormalizing it in frag to save ALU.
    viewDirection = SafeNormalize(viewDirection);
#endif

    o.viewDirectionWS = viewDirection;
#endif

    // We either sample GI from lightmap or SH. lightmap UV and vertex SH coefficients
    // are packed in lightmapUVOrVertexSH to save interpolator.
    // The following funcions initialize
    OUTPUT_LIGHTMAP_UV(v.lightmapUV, unity_LightmapST, o.lightmapUV);
    OUTPUT_SH(o.normalWS, o.vertexSH);

    half3 vertexLight = VertexLighting(posWS, o.normalWS);
    half fogFactor = ComputeFogFactor(o.clipPos.z);
    o.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
    o.shadowCoord = ComputeShadowCoord(o.clipPos);

    return o;
}

// Used in Standard (Physically Based) shader
// Physically based model. Lighting is computed in world space.
half4 LitPassFragment(LightweightVertexOutput IN) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(IN);

    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(IN.uv, surfaceData);

    InputData inputData;
    InitializeInputData(IN, surfaceData.normalTS, inputData);

    half4 color = LightweightFragmentPBR(inputData, surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.occlusion, surfaceData.emission, surfaceData.alpha);

    ApplyFog(color.rgb, inputData.fogCoord);
    return color;
}

// Used in Standard (Physically Based) shader
half4 LitPassFragmentNull(LightweightVertexOutput IN) : SV_Target
{
    LitPassFragment(IN);
    return 0;
}

// Used in Standard (Simple Lighting) shader
// BlinnPhong light model. Lighting is computed in world space.
half4 LitPassFragmentSimple(LightweightVertexOutput IN) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(IN);

    float2 uv = IN.uv;
    half4 diffuseAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
    half3 diffuse = diffuseAlpha.rgb * _Color.rgb;

    half alpha = diffuseAlpha.a * _Color.a;
    AlphaDiscard(alpha, _Cutoff);
#ifdef _ALPHAPREMULTIPLY_ON
    diffuse *= alpha;
#endif

#ifdef _NORMALMAP
    half3 normalTS = Normal(uv);
#else
    half3 normalTS = half3(0, 0, 1);
#endif

    half3 emission = Emission(uv);
    half4 specularGloss = SpecularGloss(uv, diffuseAlpha.a);
    half shininess = _Shininess * 128.0h;

    InputData inputData;
    InitializeInputData(IN, normalTS, inputData);

    return LightweightFragmentBlinnPhong(inputData, diffuse, specularGloss, shininess, emission, alpha);
};

// Used for Standard (Simple Lighting) shader
half4 LitPassFragmentSimpleNull(LightweightVertexOutput IN) : SV_Target
{
    half4 result = LitPassFragmentSimple(IN);
    return result.a;
}

// Used for Standard (Simple Lighting - Single Directional) shader
// BlinnPhong light model. Lighting is computed in world space if no normal map, tangent space otherwise.
// Optimized for a single directional light.
half4 LitPassFragmentSimpleSingleLight(LightweightVertexOutputSingleLight IN) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(IN);

    float2 uv = IN.uv;
    half4 diffuseAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
    half3 diffuse = diffuseAlpha.rgb * _Color.rgb;

    half alpha = diffuseAlpha.a * _Color.a;
    AlphaDiscard(alpha, _Cutoff);

#ifdef _ALPHAPREMULTIPLY_ON
    diffuse *= alpha;
#endif

#ifdef _NORMALMAP
    half3 normalTS = Normal(uv);
#else
    half3 normalTS = half3(0, 0, 1);
#endif

    half3 emission = Emission(uv);
    half4 specularGloss = SpecularGloss(uv, diffuseAlpha.a);
    half shininess = _Shininess * 128.0h;

    InputData inputData;
    InitializeInputDataSingleLight(IN, normalTS, inputData);

    Light mainLight = GetMainLight(inputData.positionWS);
    mainLight.attenuation = RealtimeShadowAttenuation(inputData.shadowCoord);
    MixRealtimeAndBakedGI(mainLight, inputData.normal, inputData.bakedGI, half4(0, 0, 0, 0));

#ifdef _NORMALMAP
    mainLight.direction = normalize(IN.lightDirectionTS);
#endif

    half3 attenuatedLightColor = mainLight.color * mainLight.attenuation;
    half3 diffuseColor = inputData.bakedGI + LightingLambert(attenuatedLightColor, mainLight.direction, inputData.normal);
    half3 specularColor = LightingSpecular(attenuatedLightColor, mainLight.direction, inputData.normal, inputData.viewDirection, specularGloss, shininess);

    half3 finalColor = diffuseColor * diffuse + emission;
    finalColor += inputData.vertexLighting * diffuse;

    #if defined(_SPECGLOSSMAP) || defined(_SPECULAR_COLOR)
    finalColor = specularColor;
    #endif

    ApplyFog(finalColor, inputData.fogCoord);
    return half4(finalColor, alpha);
};

#endif
