Shader "Unlit/Isolines"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (.5,.5,.5,1)
		_AngleColor ("Angle Color", Color) = (.5,.5,.5,1)
		_DangerColor ("Danger Color", Color) = (.5,.5,.5,1)
		_StartHeight("Start Height", Float) = 0
		_HeightRange("Height Range", Float) = 100
		_LineWidth("Line Width", Float) = .1
		_LineFade("Line Fade", Float) = .1
		_AngleWidth("Angle Width", Float) = .1
		_AngleFade("Angle Fade", Float) = .1
		_DangerSteepness("Danger Steepness", Float) = .1
	}
	SubShader
	{
	    Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
		LOD 100
        Blend One One
	    Cull Off Lighting Off ZWrite Off

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
			float4 _DangerColor;
			float _StartHeight;
			float _HeightRange;
			float _LineWidth;
			float _LineFade;
			float _AngleWidth;
			float _AngleFade;
			float _DangerSteepness;
			
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
				float blend = smoothstep(_StartHeight, _StartHeight + _HeightRange / 32, -h);
				
				// Calculate the direction in which the surface is facing
				float2 plan = calcGrad(IN.uv, h);
				
				float planmag = length(plan);
				
				float dangerblend = smoothstep(0,_DangerSteepness * 600, planmag * _ScreenParams.y / unity_OrthoParams.y);
				
				// Loop over isolines, computing a pseudo distance field for a number of height values
				float spacing = _HeightRange / 20;
				for (int ih = 0; ih < 20; ih++) {
					float isoline = abs(h - _StartHeight + (ih*spacing));
					//float2 isograd = float2(ddx(isoline), ddy(isoline));
					//float isomag = length(isograd);
					col += (1-smoothstep(_LineWidth * _ScreenParams.y, _LineWidth * _ScreenParams.y * _LineFade, isoline / planmag)) * lerp(_Color,_DangerColor,dangerblend) * blend; // Isoline
				}
				
				float angle = atan2(plan.y,plan.x) / 3.1415926536 + 1;
				
				float2 plannorm = normalize(plan);
				float2 plan2uv = IN.uv + float2(-plannorm.y * _MainTex_TexelSize.x, plannorm.x * _MainTex_TexelSize.y);
				float2 plan2 = calcGrad(plan2uv, -tex2D(_MainTex, plan2uv).r);
				float angle2 = atan2(plan2.y,plan2.x) / 3.1415926536 + 1;
				float angmag = abs(angle-angle2);
				angmag = min(angmag,0.025);
				//angmag = clamp(angmag,0.005,0.05);
				
				// Loop over isoangles
				for (float ia = 0.5; ia < 13; ia++) {
				    float isoline = abs(angle - ia / 6.0);// ;
					//float2 isograd = float2(ddx_fine(isoline), ddy_fine(isoline));
					//float isomag = min(length(isograd),0.025); // isomag is huge over atan2 boundary so clamp it to avoid false positives
					col += (1-smoothstep(_AngleWidth * _ScreenParams.y, _AngleWidth * _ScreenParams.y * _AngleFade, isoline / (angmag))) * lerp(_AngleColor,_DangerColor,dangerblend) * blend; // Isoline
				}
                
                //return float4(1,0,0,1);
				return col;
			}
			ENDCG
		}
	}
}
