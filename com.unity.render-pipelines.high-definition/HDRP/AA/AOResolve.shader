Shader "Hidden/HDRenderPipeline/AOResolve"
{
    HLSLINCLUDE
        #pragma target 4.5
        #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
        #include "CoreRP/ShaderLibrary/Common.hlsl"
        #include "../ShaderVariables.hlsl"
        #pragma enable_d3d11_debug_symbols

        // Target multisampling texture
        Texture2D<float4> _DepthValuesTexture;
        Texture2D<float2> _MultiAOTexture;

        struct Attributes
        {
            uint vertexID : SV_VertexID;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 texcoord   : TEXCOORD0;
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
            return output;
        }

        float Frag(Varyings input) : SV_Target
        {
            // Generate the matching pixel coordinates
            int2 msTex = int2(input.texcoord.x * _ScreenSize.x, input.texcoord.y * _ScreenSize.y);
            // Read the multiple depth values
            float4 depthValues = _DepthValuesTexture.Load(int3(msTex, 0));
            // Compute the lerp value between the depthgs
            float lerpVal = (depthValues.z - depthValues.y) / (depthValues.x - depthValues.y);
            float2 aoValues = _MultiAOTexture.Load(int3(msTex, 0));
            return (depthValues.x - depthValues.y < 0.001f) ? aoValues.x : lerp(aoValues.x, aoValues.y, pow(lerpVal, 2.2));
        }
    ENDHLSL
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            ZWrite On ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag
            ENDHLSL
        }
    }
    Fallback Off
}
