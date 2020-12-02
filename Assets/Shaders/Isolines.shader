Shader "Unlit/Isolines"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		[HDR] _Color ("Color", Color) = (.5,.5,.5,1)
		[HDR] _AngleColor ("Angle Color", Color) = (.5,.5,.5,1)
		[HDR] _DangerColor ("Danger Color", Color) = (.5,.5,.5,1)
		_StartDepth("Start Depth", Float) = 0
		_DepthRange("Height Range", Float) = 100
		_LineWidth("Line Width", Float) = .1
		_LineFade("Line Fade", Float) = .1
		_AngleWidth("Angle Width", Float) = .1
		_AngleFade("Angle Fade", Float) = .1
		_DangerSteepness("Danger Steepness", Float) = .1
		//_ClipDistance("Clip Distance", Float) = 10
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
				float2 uvw : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
			float4 _Color;
			float4 _AngleColor;
			float4 _DangerColor;
			float _StartDepth;
			float _DepthRange;
			float _LineWidth;
			float _LineFade;
			float _AngleWidth;
			float _AngleFade;
			float _DangerSteepness;
			//float _ClipDistance;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uvw = mul (unity_ObjectToWorld, v.vertex).xy;
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
			    //clip(1-length(IN.uvw)/_ClipDistance);
				// sample the texture
				half h = -tex2D(_MainTex, IN.uv).r;
				float4 col = float4(0,0,0,0);
				float blend = smoothstep(_StartDepth, _StartDepth + _DepthRange / 32, -h);
				
				// Calculate the direction in which the surface is facing
				float2 plan = calcGrad(IN.uv, h);
				
				float planmag = length(plan);
				
				float dangerblend = smoothstep(0,_DangerSteepness, planmag * _ScreenParams.y / unity_OrthoParams.y);
				
				// Loop over isolines, computing a pseudo distance field for a number of height values
				float spacing = _DepthRange / 20;
				for (int ih = 1; ih <= 21; ih++) {
					float isoline = abs(h + _StartDepth + (ih*spacing));
					//float2 isograd = float2(ddx(isoline), ddy(isoline));
					//float isomag = length(isograd);
					col += (1-smoothstep(_LineWidth, _LineWidth * _LineFade, isoline / planmag * _ScreenParams.y)) * lerp(_Color,_DangerColor,dangerblend) * blend; // Isoline
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
					col += (1-smoothstep(_AngleWidth, _AngleWidth * _AngleFade, isoline / angmag * _ScreenParams.y)) * lerp(_AngleColor,_DangerColor,dangerblend) * blend; // Isoline
				}
                
                //return float4(1,0,0,1);
				return col;
			}
			ENDCG
		}
	}
}
