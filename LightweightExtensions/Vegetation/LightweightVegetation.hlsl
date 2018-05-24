#include "LWRP/ShaderLibrary/Lighting.hlsl"

#define _VEGETATION
#define UNITY_USE_SHCOEFFS_ARRAYS 1

//float3 _VolumeDirection;
//float _VolumeStrength;
//float _VolumeTurbulence;

UNITY_INSTANCING_BUFFER_START(Props)
UNITY_DEFINE_INSTANCED_PROP(float3, _Position)
UNITY_DEFINE_INSTANCED_PROP(half3, _VolumeDirection)
UNITY_DEFINE_INSTANCED_PROP(half, _VolumeStrength)
UNITY_DEFINE_INSTANCED_PROP(half, _VolumeTurbulence)
UNITY_INSTANCING_BUFFER_END(Props)

half4 SmoothCurve( half4 x ) 
{
    return x * x *( 3.0 - 2.0 * x );
}

half4 TriangleWave( half4 x ) 
{
    return abs( frac( x + 0.5 ) * 2.0 - 1.0 );
}

half4 SmoothTriangleWave( half4 x ) 
{
    return SmoothCurve( TriangleWave( x ) );
}

float3 VegetationDeformation(float3 position, float3 origin, float3 normal, half leafStiffness, half branchStiffness, half phaseOffset)
{
    // ------------------------------
    // Main Bending

    // Move these to Material
    float trunkStiffness = 0.5; // 0 - 1

    float fBendScale = (1 - trunkStiffness) * 0.1; // main bend opacity
    float fLength = length(position); // distance to origin
    float2 turbulence = float2(sin(_Time.y + origin.x) * 0.1, sin(_Time.y + origin.z) * 0.1); // Turbulence 0 - 0.1
    float2 vWind = UNITY_ACCESS_INSTANCED_PROP(Props, _VolumeDirection).xz * lerp(0.1, turbulence, UNITY_ACCESS_INSTANCED_PROP(Props, _VolumeTurbulence)); // wind direction

    // Bend factor - Wind variation is done on the CPU.
    float fBF = position.y * fBendScale * UNITY_ACCESS_INSTANCED_PROP(Props, _VolumeStrength);

    // Smooth bending factor and increase its nearby height limit.
    fBF += 1.0;
    fBF *= fBF;
    fBF = fBF * fBF - fBF;

    // Displace position
    float3 vNewPos = position;
    vNewPos.xz += vWind.xy * fBF;

    // Rescale
    position = normalize(vNewPos.xyz) * fLength;

    // ------------------------------
    // Detail blending

    float fSpeed = 0.25; // leaf occil
    float fDetailFreq = 0.3; // detail leaf occil
    float fEdgeAtten = leafStiffness; // leaf stiffness (red)
    float fDetailAmp = 0.1; // leaf edge amplitude of movement
    float fBranchAtten = 1 - branchStiffness; // branch stiffness (blue)
    float fBranchAmp = 5.5; // branch amplitude of movement
    float fBranchPhase = phaseOffset * 3.3; // leaf phase (green)

    // Phases (object, vertex, branch)
    float fObjPhase = dot(origin, 1);
    fBranchPhase += fObjPhase;
    float fVtxPhase = dot(position, fBranchPhase + fBranchPhase);

    // x is used for edges; y is used for branches
    float2 vWavesIn = _Time.y + float2(fVtxPhase, fBranchPhase );

    // 1.975, 0.793, 0.375, 0.193 are good frequencies
    float4 vWaves = (frac( vWavesIn.xxyy * float4(1.975, 0.793, 0.375, 0.193) ) * 2.0 - 1.0 ) * fSpeed * fDetailFreq;
    vWaves = SmoothTriangleWave( vWaves );
    float2 vWavesSum = vWaves.xz + vWaves.yw;

    // Edge (xy) and branch bending (z)
    return position + vWavesSum.xyx * float3(fEdgeAtten * fDetailAmp * normal.x, fBranchAtten * fBranchAmp, fEdgeAtten * fDetailAmp * normal.z);
}