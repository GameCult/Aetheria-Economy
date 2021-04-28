Shader "Brushes/Cubic Pulse Brush" {
Properties {
	_Depth ("Depth", Float) = 0.5
}

Category {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
	Blend One One
	ColorMask RGB
	Cull Off Lighting Off ZWrite Off
	
	SubShader {
		Pass {
		
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"

			float _Depth;
			
			struct appdata_t {
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};
			
			float4 _MainTex_ST;

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.color = v.color;
				o.texcoord = v.texcoord;
				return o;
			}

			float cubicPulse( float c, float w, float x )
			{
				x = abs(x - c);
				if( x>w ) return 0.0f;
				x /= w;
				return 1.0f - x*x*(3.0f-2.0f*x);
			}

			fixed4 frag (v2f i) : SV_Target
			{
				return _Depth * cubicPulse(0,.5,length(i.texcoord-float2(.5,.5)));
			}
			ENDCG 
		}
	}	
}
}
