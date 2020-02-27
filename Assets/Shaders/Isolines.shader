Shader "Unlit/Isolines"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (.5,.5,.5,1)
		_AngleColor ("Angle Color", Color) = (.5,.5,.5,1)
		_StartHeight("Start Height", Float) = 0
		_HeightRange("Height Range", Float) = 100
		_LineWidth("Line Width", Float) = .1
		_LineFade("Line Fade", Float) = .1
		_AngleWidth("Angle Width", Float) = .1
		_AngleFade("Angle Fade", Float) = .1
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100
        Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma only_renderers d3d11
			#pragma target 5.0
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
			float4 _Color;
			float4 _AngleColor;
			float _StartHeight;
			float _HeightRange;
			float _LineWidth;
			float _LineFade;
			float _AngleWidth;
			float _AngleFade;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
            float2 calcGrad (float2 uv, float me)
            {
                float n = -tex2D(_MainTex, float2(uv.x, uv.y + _MainTex_TexelSize.y)).x;
                float e = -tex2D(_MainTex, float2(uv.x + _MainTex_TexelSize.x, uv.y)).x;
                return float2(e-me,n-me);
            }
			
			fixed4 frag (v2f IN) : SV_Target
			{
				// sample the texture
				half h = -tex2D(_MainTex, IN.uv).r;
				float4 col = float4(0,0,0,0);
				
				// Loop over isolines, computing a pseudo distance field for a number of height values
				float spacing = _HeightRange / 20;
				for (int i = 0; i < 20; i++) {
					float isoline = abs(h - _StartHeight + (i*spacing));
					float2 isograd = float2(ddx_fine(isoline), ddy_fine(isoline));
					float isomag = length(isograd);
					col += (1-smoothstep(_LineWidth * _ScreenParams.y, _LineWidth * _ScreenParams.y * _LineFade, isoline / (isomag))) * _Color; // Isoline
				}
				
				// Calculate the direction in which the surface is facing
				float2 plan = normalize(calcGrad(IN.uv, h));
				
				// 
				float angle = atan2(plan.y,plan.x) / 3.1415926536 + 1;
				
				// Loop over isoangles
				for (float i = 0.5; i < 13; i++) {
				    float isoline = abs(angle - i / 6.0);// ;
					float2 isograd = float2(ddx_fine(isoline), ddy_fine(isoline));
					float isomag = length(isograd);
					float blend = smoothstep(-2,-10,h)*smoothstep(0.04,0.083333,angle)*smoothstep(1.96,1.916667,angle);
					col += (1-smoothstep(_AngleWidth * _ScreenParams.y, _AngleWidth * _ScreenParams.y * _AngleFade, isoline / (isomag))) * _AngleColor * blend; // Isoline
				}
                
				return col;
			}
			ENDCG
		}
	}
}
