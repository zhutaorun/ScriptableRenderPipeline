Shader "Hidden/SimpleTonemapper"
{
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
            #pragma target 5.0
			
			#include "UnityCG.cginc"

			struct v2f
			{
				float4 vertex : SV_Position;
			};

            UNITY_DECLARE_FRAMEBUFFER_INPUT(0); 
             
			v2f vert (uint id : SV_VertexID)
			{
				v2f o;
				float2 ouv = float2((id << 1) & 2, id & 2);
				o.vertex = float4(ouv * float2(2, -2) + float2(-1, 1), 0, 1);
				return o;
			}
			
//			sampler2D _MainTex;

			half4 frag (v2f i) : SV_Target
			{
				float _ExposureAdjustment = 0.5;
				float4 exp = float4(1.0, 0.8, 0.7, 1.0) * _ExposureAdjustment;
//				half4 col = tex2D(_MainTex, i.uv);
                half4 col = UNITY_READ_FRAMEBUFFER_INPUT(0, i.vertex);
//                half4 col = _UnityFBInput0.Load(int3(i.vertex.xy, 0));
				return 1-exp2(-exp * col);
			}
			ENDCG
		}
	}
}
