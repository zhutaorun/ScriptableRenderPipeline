Shader "ShaderGraph/Tests/Default-Unlit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            Tags { "LightMode" = "TestPass" }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariables.hlsl"

            float4 _Color;

            struct MeshAttributes
            {
                float3 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct VaryingsM2F
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            VaryingsM2F vert(MeshAttributes mesh)
            {
                VaryingsM2F o;

                o.position = mul(UNITY_MATRIX_VP, mul(UNITY_MATRIX_M, float4(mesh.vertex, 1)));
                o.uv = mesh.texcoord.xy;

                return o;
            }

            float4 frag(VaryingsM2F v) : SV_Target
            {
                // ${PixelShaderBody}
                return _Color;
            }

            ENDHLSL
        }
    }
}