Shader "Hidden/HDRenderPipeline/DepthResolve"
{
    HLSLINCLUDE
        #pragma target 4.5
        #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
        #include "CoreRP/ShaderLibrary/Common.hlsl"
        #include "../ShaderVariables.hlsl"
        #pragma enable_d3d11_debug_symbols

        // Target multisampling texture
        Texture2DMS<float> _DepthTextureMS;

        // Different resolving approaches
        #define RESOLVE_MAX
        // #define RESOLVE_MIN
        // #define RESOLVE_AVERAGE

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
            output.texcoord   = GetFullScreenTriangleTexCoord(input.vertexID);
            return output;
        }

        float Frag1X(Varyings input) : SV_Depth
        {
            int2 msTex = int2(input.texcoord.x * _ScreenSize.x, input.texcoord.y * _ScreenSize.y);
            return _DepthTextureMS.Load(msTex, 0).x;
        }

        float Frag2X(Varyings input) : SV_Depth
        {
            return 0.0f;
        }

        float Frag4X(Varyings input) : SV_Depth
        {
            return 0.0f;
        }

        float Frag8X(Varyings input) : SV_Depth
        {
            int2 msTex = int2(input.texcoord.x * _ScreenSize.x, input.texcoord.y * _ScreenSize.y);
            float depth = 0.0f;
            for(int sampleIdx = 0; sampleIdx < 8; ++sampleIdx)
            {
                depth = max(_DepthTextureMS.Load(msTex, sampleIdx).x, depth);
            }
            return depth;
        }
    ENDHLSL
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        // 0: MSAA 1x
        Pass
        {
            ZWrite On ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag1X
            ENDHLSL
        }

        // 1: MSAA 2x
        Pass
        {
            ZWrite On ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag2X
            ENDHLSL
        }

        // 2: MSAA 4X
        Pass
        {
            ZWrite On ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag4X
            ENDHLSL
        }

        // 3: MSAA 8X
        Pass
        {
            ZWrite On ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag8X
            ENDHLSL
        }
    }
    Fallback Off
}