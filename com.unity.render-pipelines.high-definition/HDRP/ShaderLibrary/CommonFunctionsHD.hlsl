#ifndef UNITY_COMMON_FUNCTIONS_HD_INCLUDED
#define UNITY_COMMON_FUNCTIONS_HD_INCLUDED

#include "CoreRP/ShaderLibrary/CommonFunctions.hlsl"

// This method should be used for rendering any full screen quad that uses an auto-scaling Render Targets (see RTHandle/HDCamera)
// It will account for the fact that the textures it samples are not necesarry using the full space of the render texture but only a partial viewport.
float2 GetNormalizedFullScreenTriangleTexCoord(uint vertexID)
{
    return GetFullScreenTriangleTexCoord(vertexID) * _ScreenToTargetScale.xy;
}

// The size of the render target can be larger than the size of the viewport.
// This function returns the fraction of the render target covered by the viewport:
// ViewportScale = ViewportResolution / RenderTargetResolution.
// Do not assume that their size is the same, or that sampling outside of the viewport returns 0.
float2 GetViewportScaleCurrentFrame()
{
    return _ScreenToTargetScale.xy;
}

float2 GetViewportScalePreviousFrame()
{
    return _ScreenToTargetScale.zw;
}

// This function always return the camera relative position in WS either the CameraRelative mode is enabled or not
float3 GetAbsolutePositionWS(float3 positionWS)
{
#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
    positionWS += GetCameraPositionWS();
#endif
    return positionWS;
}

float3 GetCameraRelativePositionWS(float3 positionWS)
{
#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
    positionWS -= GetCameraPositionWS();
#endif
    return positionWS;
}

float3 GetPrimaryCameraPosition()
{
#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
    return float3(0, 0, 0);
#else
    return GetCameraPositionWS();
#endif
}

// Could be e.g. the position of a primary camera or a shadow-casting light.
float3 GetCurrentViewPosition()
{
#if defined(SHADERPASS) && (SHADERPASS != SHADERPASS_SHADOWS)
    return GetPrimaryCameraPosition();
#else
    // This is a generic solution.
    // However, for the primary camera, using '_WorldSpaceCameraPos' is better for cache locality,
    // and in case we enable camera-relative rendering, we can statically set the position is 0.
    float4x4 inverseViewMat = GetViewToWorldMatrix();
    return inverseViewMat._14_24_34;
#endif
}

// Returns the forward (central) direction of the current view in the world space.
float3 GetViewForwardDir()
{
    float4x4 viewMat = GetWorldToViewMatrix();
    return -viewMat[2].xyz;
}

// Returns 'true' if the current view performs a perspective projection.
bool IsPerspectiveProjection()
{
#if defined(SHADERPASS) && (SHADERPASS != SHADERPASS_SHADOWS)
    return (unity_OrthoParams.w == 0);
#else
    // TODO: set 'unity_OrthoParams' during the shadow pass.
    float4x4 clipMat = GetViewToHClipMatrix();
    return clipMat[3][3] == 0;
#endif
}

// Computes the world space view direction (pointing towards the viewer).
float3 GetWorldSpaceViewDir(float3 positionWS)
{
    if (IsPerspectiveProjection())
    {
        // Perspective
        return GetCurrentViewPosition() - positionWS;
    }
    else
    {
        // Orthographic
        return -GetViewForwardDir();
    }
}

float3 GetWorldSpaceNormalizeViewDir(float3 positionWS)
{
    return normalize(GetWorldSpaceViewDir(positionWS));
}

#endif // UNITY_COMMON_FUNCTIONS_HD_INCLUDED
