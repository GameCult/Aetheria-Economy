Shader "Brushes/Wormhole Brush" {
Properties {
	_Depth ("Depth", Float) = 0.5
	_Power("Envelope Power", Float) = 1.25
	_Frequency("Frequency", Float) = 6.2832
	_FrequencyDist("Distance Frequency", Float) = 6.2832
	_Speed("Speed", Float) = 0
	_SinePower("Sine Power", Float) = 1.25
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
			float _Power;
			float _Frequency;
			float _FrequencyDist;
			float _Speed;
			float _SinePower;
			
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

			float almostIdentity(float x, float m, float n)
			{
				if (x>m) return x;

				const float a = 2.0f*n - m;
				const float b = 2.0f*m - 3.0f*n;
				const float t = x / m;

				return (a*t + b)*t*t + n;
			}

			float powerPulse( float x, float power )
			{
				x = saturate(abs(x));
				return pow((x + 1.0f) * (1.0f - x), power);
			}

			fixed4 frag (v2f i) : SV_Target
			{
			    float2 uv2 = i.texcoord - float2(.5,.5);
				float dist = length(uv2);
				float ang = atan2(uv2.y,uv2.x);
				return _Depth * powerPulse(dist * 2,_Power) * (cos(ang*_Frequency+pow(dist, _SinePower)*_FrequencyDist+_Time.x*_Speed)+1);
			}
			ENDCG 
		}
	}	
}
}
