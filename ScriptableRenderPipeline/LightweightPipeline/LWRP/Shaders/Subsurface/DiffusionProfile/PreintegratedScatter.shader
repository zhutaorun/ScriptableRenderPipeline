Shader "Hidden/LightweightPipeline/PreintegratedScatter"
{
    SubShader
    {
        Pass
        {
            Cull   Off
            ZTest  Always
            ZWrite Off
            Blend  Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal

            #pragma vertex Vert
            #pragma fragment Frag

            //-------------------------------------------------------------------------------------
            // Include
            //-------------------------------------------------------------------------------------

            #include "CoreRP/ShaderLibrary/Common.hlsl"
            #define USE_LEGACY_UNITY_MATRIX_VARIABLES
            #include "HDRP/ShaderVariables.hlsl"
            
            #include "LWRP/DiffusionProfile/DiffusionProfileSettings.cs.hlsl"

            //-------------------------------------------------------------------------------------
            // Inputs & outputs
            //-------------------------------------------------------------------------------------

            float4 _StdDev1, _StdDev2;
            float _LerpWeight; // See 'SubsurfaceScatteringParameters'

            //-------------------------------------------------------------------------------------
            // Implementation
            //-------------------------------------------------------------------------------------

            struct Attributes
            {
                float3 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct Varyings
            {
                float4 vertex   : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.vertex   = TransformWorldToHClip(input.vertex);
                output.texcoord = input.texcoord.xy;
                return output;
            }

            float3 DiffusionProfile(float r, float3 var1, float3 var2)
            {
                return lerp(exp(-r * r / (2 * var1)) / (TWO_PI * var1),
                            exp(-r * r / (2 * var2)) / (TWO_PI * var2), _LerpWeight);
            }

            //Pre-integrate the diffuse scattering, indexed by Curvate and NdotL.
            float4 Frag(Varyings input) : SV_Target
            {
                float NdotL     = 2.0 * input.texcoord.x - 1.0;
                float Curvature = 1.0 / input.texcoord.y;

                float Theta = acos(NdotL);
                float3 W = float3(0, 0, 0);
                float3 L = float3(0, 0, 0);

                float x = -(PI / 2.0);
                while(x <= (PI / 2.0))
                {
                    float Diffuse = saturate(cos(Theta + x));

                    float  R = abs(2.0 * Curvature * sin(x * 0.5));
                    float3 Variance1 = _StdDev1.rgb * _StdDev1.rgb;
                    float3 Variance2 = _StdDev2.rgb * _StdDev2.rgb;

                    float3 Weights = DiffusionProfile(R, Variance1, Variance2);

                    W += Weights;
                    L += Diffuse * Weights;

                    x += PI / 180.0;
                }

                return float4(sqrt(L / W), 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
