#if SHADERPASS != SHADERPASS_GBUFFER
#error SHADERPASS_is_not_correctly_define
#endif

#include "VertMesh.hlsl"

PackedVaryingsType Vert(AttributesMesh inputMesh)
{
    VaryingsType varyingsType;
    varyingsType.vmesh = VertMesh(inputMesh);
    return PackVaryingsType(varyingsType);
}

#ifdef TESSELLATION_ON

PackedVaryingsToPS VertTesselation(VaryingsToDS input)
{
    VaryingsToPS output;
    output.vmesh = VertMeshTesselation(input.vmesh);
    return PackVaryingsToPS(output);
}

#include "TessellationShare.hlsl"

#endif // TESSELLATION_ON

half3 LerpWhiteTo(half3 b, half t)
{
    half oneMinusT = 1 - t;
    return half3(oneMinusT, oneMinusT, oneMinusT) + b * t;
}


void Frag(  PackedVaryingsToPS packedInput,
            OUTPUT_GBUFFER(outGBuffer)
            OUTPUT_GBUFFER_VELOCITY(outVelocityBuffer)    
            #ifdef _DEPTHOFFSET_ON
            , out float outputDepth : SV_Depth
            #endif
            )
{
    FragInputs input = UnpackVaryingsMeshToFragInputs(packedInput.vmesh);

    // input.unPositionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.unPositionSS.xy, _ScreenSize.zw);
    UpdatePositionInput(input.unPositionSS.z, input.unPositionSS.w, input.positionWS, posInput);
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionWS);

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

#ifdef _OCCLUSIONMAP   
	UVMapping occlusionTexcoord;
	ZERO_INITIALIZE(UVMapping, occlusionTexcoord);
	occlusionTexcoord.uv = input.texCoord0;
    surfaceData.ambientOcclusion = SAMPLE_UVMAPPING_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, occlusionTexcoord).g;
#else   
    surfaceData.ambientOcclusion = 1.0f;
#endif

#ifdef _DETAIL_MAP_LEGACY
	UVMapping detailTexcoord;
	ZERO_INITIALIZE(UVMapping, detailTexcoord);
	detailTexcoord.uv = input.texCoord0 * _DetailMap_ST.xy + _DetailMap_ST.zw;
    float3 detailAlbedo = saturate(SAMPLE_UVMAPPING_TEXTURE2D(_DetailMapLegacy, sampler_DetailMapLegacy, detailTexcoord).rgb * 2.0f);
	float detailMask =  1.0f;
	#ifdef _DETAIL_MASK_MAP_LEGACY
		detailMask = SAMPLE_UVMAPPING_TEXTURE2D(_DetailMaskMapLegacy, sampler_DetailMaskMapLegacy, detailTexcoord).a;
	#endif
	surfaceData.baseColor *= LerpWhiteTo (detailAlbedo, detailMask);
#endif    

    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(surfaceData);

    PreLightData preLightData = GetPreLightData(V, posInput, bsdfData);

    float3 bakeDiffuseLighting = GetBakedDiffuseLigthing(surfaceData, builtinData, bsdfData, preLightData);

    ENCODE_INTO_GBUFFER(surfaceData, bakeDiffuseLighting, outGBuffer);
    ENCODE_VELOCITY_INTO_GBUFFER(builtinData.velocity, outVelocityBuffer);

#ifdef _DEPTHOFFSET_ON
    outputDepth = posInput.depthRaw;
#endif
}
