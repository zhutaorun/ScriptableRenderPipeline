real4 ADD_FUNC_SUFFIX(ADD_NORMAL_FUNC_SUFFIX(SampleUVMappingNormal))(TEXTURE2D_ARGS(textureName, samplerName), UVMapping uvMapping, real scale, real param)
{
    if (uvMapping.mappingType == UV_MAPPING_TRIPLANAR)
    {
        real3 triplanarWeights = uvMapping.triplanarWeights;

#ifdef SURFACE_GRADIENT
        real2 derivXplane;
        real2 derivYPlane;
        real2 derivZPlane;
        derivXplane = derivYPlane = derivZPlane = real2(0.0, 0.0);
        real normalVariance = 0.0;

        if (triplanarWeights.x > 0.0)
        {
            real4 packedNormal = SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uvZY, param);
            normalVariance += triplanarWeights.x * packedNormal.z;
            derivXplane = triplanarWeights.x * UNPACK_DERIVATIVE_FUNC(packedNormal, scale);
        }
        if (triplanarWeights.y > 0.0)
        {
            real4 packedNormal = SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uvXZ, param);
            normalVariance += triplanarWeights.y * packedNormal.z;
            derivYPlane = triplanarWeights.y * UNPACK_DERIVATIVE_FUNC(packedNormal, scale);
        }
        if (triplanarWeights.z > 0.0)
        {
            real4 packedNormal = SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uvXY, param);
            normalVariance += triplanarWeights.z * packedNormal.z;
            derivZPlane = triplanarWeights.z * UNPACK_DERIVATIVE_FUNC(packedNormal, scale);
        }

        // Assume derivXplane, derivYPlane and derivZPlane sampled using (z,y), (z,x) and (x,y) respectively.
        // TODO: Check with morten convention! Do it follow ours ?
        real3 volumeGrad = real3(derivZPlane.x + derivYPlane.y, derivZPlane.y + derivXplane.y, derivXplane.x + derivYPlane.x);
        return real4(SurfaceGradientFromVolumeGradient(uvMapping.normalWS, volumeGrad), DecodeNormalMapVariance(normalVariance));
#else
        real3 val = real3(0.0, 0.0, 0.0);
        real normalVariance = 0.0;

        if (triplanarWeights.x > 0.0)
        {
            real4 packedNormal = SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uvZY, param);
            normalVariance += triplanarWeights.x * packedNormal.z;
            val += triplanarWeights.x * UNPACK_NORMAL_FUNC(packedNormal, scale);
        }
        if (triplanarWeights.y > 0.0)
        {
            real4 packedNormal = SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uvXZ, param);
            normalVariance += triplanarWeights.y * packedNormal.z;
            val += triplanarWeights.y * UNPACK_NORMAL_FUNC(packedNormal, scale);
        }
        if (triplanarWeights.z > 0.0)
        {
            real4 packedNormal = SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uvXY, param);
            normalVariance += triplanarWeights.z * packedNormal.z;
            val += triplanarWeights.z * UNPACK_NORMAL_FUNC(packedNormal, scale);
        }

        return real4(normalize(val), DecodeNormalMapVariance(normalVariance));
#endif
    }
#ifdef SURFACE_GRADIENT
    else if (uvMapping.mappingType == UV_MAPPING_PLANAR)
    {
        real4 packedNormal = SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uv, param);
        real normalVariance = packedNormal.z;
        // Note: Planar is on uv coordinate (and not uvXZ)
        real2 derivYPlane = UNPACK_DERIVATIVE_FUNC(packedNormal, scale);
        // See comment above
        real3 volumeGrad = real3(derivYPlane.y, 0.0, derivYPlane.x);
        return real4(SurfaceGradientFromVolumeGradient(uvMapping.normalWS, volumeGrad), DecodeNormalMapVariance(normalVariance));
    }
#endif
    else
    {
        real4 packedNormal = SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uv, param);
        real normalVariance = packedNormal.z;
#ifdef SURFACE_GRADIENT
        real2 deriv = UNPACK_DERIVATIVE_FUNC(packedNormal, scale);
        return real4(SurfaceGradientFromTBN(deriv, uvMapping.tangentWS, uvMapping.bitangentWS), DecodeNormalMapVariance(normalVariance));
#else
        return real4(UNPACK_NORMAL_FUNC(packedNormal, scale), DecodeNormalMapVariance(normalVariance));
#endif
    }
}
