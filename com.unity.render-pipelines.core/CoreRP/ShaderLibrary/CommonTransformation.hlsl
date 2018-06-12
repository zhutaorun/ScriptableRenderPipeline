#ifndef UNITY_COMMON_TRANSFORMATION_INCLUDED
#define UNITY_COMMON_TRANSFORMATION_INCLUDED

///////////////////////////////////////////////////////////////
//                  Built In Variables                       //
///////////////////////////////////////////////////////////////

real GetOddNegativeScale()
{
    return unity_WorldTransformParams.w;
}

///////////////////////////////////////////////////////////////
//                      Matrices                             //
///////////////////////////////////////////////////////////////

float4x4 GetObjectToWorldMatrix()
{
    return UNITY_MATRIX_M;
}

float4x4 GetWorldToObjectMatrix()
{
    return UNITY_MATRIX_I_M;
}

float4x4 GetWorldToViewMatrix()
{
    return UNITY_MATRIX_V;
}

// Transform to homogenous clip space
float4x4 GetWorldToHClipMatrix()
{
    return UNITY_MATRIX_VP;
}

// Transform to homogenous clip space
float4x4 GetViewToHClipMatrix()
{
    return UNITY_MATRIX_P;
}

///////////////////////////////////////////////////////////////
//                 Space Transformations                     //
///////////////////////////////////////////////////////////////

float3 TransformObjectToWorld(float3 positionOS)
{
    return mul(GetObjectToWorldMatrix(), float4(positionOS, 1.0)).xyz;
}

real3 TransformObjectToWorldDir(real3 dirOS)
{
    // Normalize to support uniform scaling
    return normalize(mul((real3x3)GetObjectToWorldMatrix(), dirOS));
}

real3 TransformObjectToTangent(real3 dirOS, real3x3 worldToTangent)
{
    return mul(worldToTangent, TransformObjectToWorldDir(dirOS));
}

// Transforms position from object space to homogenous space
float4 TransformObjectToHClip(float3 positionOS)
{
    // More efficient than computing M*VP matrix product
    return mul(GetWorldToHClipMatrix(), mul(GetObjectToWorldMatrix(), float4(positionOS, 1.0)));
}

// Transforms normal from object to world space
real3 TransformObjectToWorldNormal(real3 normalOS)
{
#ifdef UNITY_ASSUME_UNIFORM_SCALING
    return TransformObjectToWorldDir(normalOS);
#else
    // Normal need to be multiply by inverse transpose
    return normalize(mul(normalOS, (real3x3)GetWorldToObjectMatrix()));
#endif
}

float3 TransformWorldToObject(float3 positionWS)
{
    return mul(GetWorldToObjectMatrix(), float4(positionWS, 1.0)).xyz;
}

float3 TransformWorldToView(float3 positionWS)
{
    return mul(GetWorldToViewMatrix(), float4(positionWS, 1.0)).xyz;
}

real3 TransformWorldToTangent(real3 dirWS, real3x3 worldToTangent)
{
    return mul(worldToTangent, dirWS);
}

float3 TransformWorldToViewDir(float3 dirWS)
{
    return mul((float3x3)GetWorldToViewMatrix(), dirWS).xyz;
}

// Tranforms vector from world space to homogenous space
float3 TransformWorldToHClipDir(float3 directionWS)
{
    return mul((float3x3)GetWorldToHClipMatrix(), directionWS);
}

real3 TransformWorldToObjectDir(real3 dirWS)
{
    // Normalize to support uniform scaling
    return normalize(mul((real3x3)GetWorldToObjectMatrix(), dirWS));
}

// Tranforms position from world space to homogenous space
float4 TransformWorldToHClip(float3 positionWS)
{
    return mul(GetWorldToHClipMatrix(), float4(positionWS, 1.0));
}

real3x3 CreateWorldToTangent(real3 normal, real3 tangent, real flipSign)
{
    // For odd-negative scale transforms we need to flip the sign
    real sgn = flipSign * GetOddNegativeScale();
    real3 bitangent = cross(normal, tangent) * sgn;

    return real3x3(tangent, bitangent, normal);
}

real3 TransformTangentToObject(real3 dirTS, real3x3 worldToTangent)
{
    // Use transpose transformation to go from tangent to world as the matrix is orthogonal
    real3 normalWS = mul(dirTS, worldToTangent);
    return mul((real3x3)GetWorldToObjectMatrix(), normalWS);
}

real3 TransformTangentToWorld(real3 dirTS, real3x3 worldToTangent)
{
    // Use transpose transformation to go from tangent to world as the matrix is orthogonal
    return mul(dirTS, worldToTangent);
}

// Tranforms position from view space to homogenous space
float4 TransformWViewToHClip(float3 positionVS)
{
    return mul(GetViewToHClipMatrix(), float4(positionVS, 1.0));
}

// Tranforms vector from world space to homogenous space
float3 TransformViewToHClipDir(float3 directionVS)
{
    return mul((float3x3)GetViewToHClipMatrix(), directionVS);
}

#endif // UNITY_COMMON_TRANSFORMATION_INCLUDED
