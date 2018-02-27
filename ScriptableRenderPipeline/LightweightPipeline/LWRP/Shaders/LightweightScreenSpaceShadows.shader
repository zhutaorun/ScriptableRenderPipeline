Shader "Hidden/LightweightPipeline/ScreenSpaceShadows"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "LightweightPipeline" }

        HLSLINCLUDE

        //Keep compiler quiet about Shadows.hlsl. 
        #include "CoreRP/ShaderLibrary/Common.hlsl"
        #include "CoreRP/ShaderLibrary/EntityLighting.hlsl"
        #include "CoreRP/ShaderLibrary/ImageBasedLighting.hlsl"
        #include "LWRP/ShaderLibrary/Core.hlsl"
        #include "LWRP/ShaderLibrary/Shadows.hlsl"

#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
        TEXTURE2D_ARRAY(_CameraDepthTexture);
        SAMPLER(sampler_CameraDepthTexture);
#else
        TEXTURE2D(_CameraDepthTexture);
        SAMPLER(sampler_CameraDepthTexture);
#endif

        struct VertexInput
        {
            float4 vertex   : POSITION;
            float2 texcoord : TEXCOORD0;
        };

        struct Interpolators
        {
            half4  pos      : SV_POSITION;
            half4  texcoord : TEXCOORD0;
        };

        Interpolators Vertex(VertexInput i)
        {
            Interpolators o;

            o.pos = i.vertex.xyzw;

            float4 projPos = o.pos * 0.5;
            projPos.xy = projPos.xy + projPos.w;

            //o.texcoord.xy = i.texcoord;
            o.texcoord.xy = UnityStereoTransformScreenSpaceTex(i.texcoord.xy);
            o.texcoord.zw = projPos.xy;

            return o;
        }

        half Fragment(Interpolators i) : SV_Target
        {
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
            float deviceDepth = SAMPLE_TEXTURE2D_ARRAY(_CameraDepthTexture, sampler_CameraDepthTexture, i.texcoord.xy, unity_StereoEyeIndex).r;
#else
            float deviceDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.texcoord.xy);
#endif

#if UNITY_REVERSED_Z
            deviceDepth = 1 - deviceDepth;
#endif
            deviceDepth = 2 * deviceDepth - 1; //NOTE: Currently must massage depth before computing CS position. 

            float3 vpos = ComputeViewSpacePosition(i.texcoord.zw, deviceDepth, unity_CameraInvProjection);
            float3 wpos = mul(unity_CameraToWorld, float4(vpos, 1)).xyz;
            
            //Fetch shadow coordinates for cascade.
            float4 coords  = ComputeScreenSpaceShadowCoords(wpos);

            return SampleShadowmap(coords);
        }

        ENDHLSL

        Pass
        {           
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma enable_d3d11_debug_symbols
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _SHADOWS_CASCADE
            
            #pragma vertex   Vertex
            #pragma fragment Fragment
            ENDHLSL
        }
    }
}
