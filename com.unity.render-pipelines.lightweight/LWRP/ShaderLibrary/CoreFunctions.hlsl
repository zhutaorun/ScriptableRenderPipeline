#ifndef UNITY_SHADER_VARIABLES_FUNCTIONS_INCLUDED
#define UNITY_SHADER_VARIABLES_FUNCTIONS_INCLUDED

#include "CoreRP/ShaderLibrary/CommonTransformation.hlsl"

float3 GetCameraPositionWS()
{
    return _WorldSpaceCameraPos;
}

#endif // UNITY_SHADER_VARIABLES_FUNCTIONS_INCLUDED
