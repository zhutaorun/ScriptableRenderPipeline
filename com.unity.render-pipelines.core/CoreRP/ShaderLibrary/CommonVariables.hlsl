#ifndef UNITY_COMMON_VARIABLES_INCLUDED
#define UNITY_COMMON_VARIABLES_INCLUDED

///////////////////////////////////////////////////////////////
//                     Global Textures                       //
///////////////////////////////////////////////////////////////

// Depth buffer
TEXTURE2D(_CameraDepthTexture);
SAMPLER(sampler_CameraDepthTexture);

// Main lightmap
TEXTURE2D(unity_Lightmap);
SAMPLER(samplerunity_Lightmap);
// Dual or directional lightmap (always used with unity_Lightmap, so can share sampler)
TEXTURE2D(unity_LightmapInd);

// We can have shadowMask only if we have lightmap, so no sampler
TEXTURE2D(unity_ShadowMask);

///////////////////////////////////////////////////////////////
//                       Projection                          //
///////////////////////////////////////////////////////////////

float4x4 OptimizeProjectionMatrix(float4x4 M)
{
    // Matrix format (x = non-constant value).
    // Orthographic Perspective  Combined(OR)
    // | x 0 0 x |  | x 0 x 0 |  | x 0 x x |
    // | 0 x 0 x |  | 0 x x 0 |  | 0 x x x |
    // | x x x x |  | x x x x |  | x x x x | <- oblique projection row
    // | 0 0 0 1 |  | 0 0 x 0 |  | 0 0 x x |
    // Notice that some values are always 0.
    // We can avoid loading and doing math with constants.
    M._21_41 = 0;
    M._12_42 = 0;
    return M;
}

#endif // UNITY_COMMON_VARIABLES_INCLUDED
