//-----------------------------------------------------------------------------
// EvaluateBSDF_Env - Reference
// ----------------------------------------------------------------------------

float3 IntegrateSpecularCottonWoolIBLRef(LightLoopContext lightLoopContext,
                                  float3 V, PreLightData preLightData, EnvLightData lightData, BSDFData bsdfData,
                                  uint sampleCount = 4096)
{
    float3x3 localToWorld = GetLocalFrame(bsdfData.normalWS);
    float    NdotV        = ClampNdotV(dot(bsdfData.normalWS, V));
    float3 acc   = float3(0.0, 0.0, 0.0);
   
    // Add some jittering on Hammersley2d
    float2 randNum  = InitRandom(V.xy * 0.5 + 0.5);

    for (uint i = 0; i < sampleCount; ++i)
    {
        float2 u    = Hammersley2d(i, sampleCount);
        u           = frac(u + randNum);

        float3 localL = SampleHemisphereUniform(u.x, u.y);
        float3 L = mul(localL, localToWorld);
        float NdotL = saturate(dot(bsdfData.normalWS, L));

        if (NdotL > 0.0)
        {
            float LdotV, NdotH, LdotH, NdotV, invLenLV;
            GetBSDFAngle(V, L, NdotL, preLightData.NdotV, LdotV, NdotH, LdotH, NdotV, invLenLV);

            // Incident Light intensity
            float4 val = SampleEnv(lightLoopContext, lightData.envIndex, L, 0);

            // BRDF Data
            float3 F = F_Schlick(bsdfData.specularColor, LdotH);
            float D = D_Charlie(NdotH, bsdfData.roughnessT);
            float Vis = V_Charlie(NdotL, NdotV, bsdfData.roughnessT);

            // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
            acc += F * D * Vis * val.rgb * NdotL;
        }
    }
    return acc * TWO_PI / sampleCount;
}

float3 IntegrateSpecularSilkIBLRef(LightLoopContext lightLoopContext,
                                  float3 V, PreLightData preLightData, EnvLightData lightData, BSDFData bsdfData,
                                  uint sampleCount = 2048)
{
    // Given that it may be anisotropic we need to compute the oriented basis
    float3x3 localToWorld = float3x3(bsdfData.tangentWS, bsdfData.bitangentWS, bsdfData.normalWS);
    float    NdotV        = ClampNdotV(dot(bsdfData.normalWS, V));
    float3 acc   = float3(0.0, 0.0, 0.0);
   
    // Add some jittering on Hammersley2d
    float2 randNum  = InitRandom(V.xy * 0.5 + 0.5);

    for (uint i = 0; i < sampleCount; ++i)
    {
        float2 u    = Hammersley2d(i, sampleCount);
        u           = frac(u + randNum);

        float3 localL = SampleHemisphereUniform(u.x, u.y);
        float3 L = mul(localL, localToWorld);
        float NdotL = saturate(dot(bsdfData.normalWS, L));

        if (NdotL > 0.0)
        {

            float LdotV, NdotH, LdotH, NdotV, invLenLV;
            GetBSDFAngle(V, L, NdotL, preLightData.NdotV, LdotV, NdotH, LdotH, NdotV, invLenLV);

            float4 val = SampleEnv(lightLoopContext, lightData.envIndex, L, 0);

            // For silk we just use a tinted anisotropy
            float3 H = (L + V) * invLenLV;

            // For anisotropy we must not saturate these values
            float TdotH = dot(bsdfData.tangentWS, H);
            float TdotL = dot(bsdfData.tangentWS, L);
            float BdotH = dot(bsdfData.bitangentWS, H);
            float BdotL = dot(bsdfData.bitangentWS, L);

            // TODO: Do comparison between this correct version and the one from isotropic and see if there is any visual difference
            float DV = DV_SmithJointGGXAniso(   TdotH, BdotH, NdotH, NdotV, TdotL, BdotL, NdotL,
                                                bsdfData.roughnessT, bsdfData.roughnessB, preLightData.partLambdaV);
            float3 F = F_Schlick(bsdfData.specularColor, LdotH);

            // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
            acc += F * DV * val.rgb * NdotL;
        }
    }
    return acc * TWO_PI / sampleCount;
}