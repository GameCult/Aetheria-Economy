/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

Shader "UI/Isolines"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
    	_DetailTex ("Detail Texture", 2D) = "white" {}
//    	_TintTex ("Tint Texture", 2D) = "black" {}
//		_TintIntensity("Tint Intensity", Float) = 1
//		_TintExponent("Tint Exponent", Float) = 1
		[HDR] _Color ("Color", Color) = (.5,.5,.5,1)
		[HDR] _AngleColor ("Angle Color", Color) = (.5,.5,.5,1)
		[HDR] _DangerColor ("Danger Color", Color) = (.5,.5,.5,1)
		_StartDepth("Start Depth", Float) = 0
		_DepthRange("Height Range", Float) = 100
		_LineWidth("Line Width", Float) = .1
		_LineFade("Line Fade", Float) = .1
		_AngleWidth("Angle Width", Float) = .1
		_AngleFade("Angle Fade", Float) = .1
//		_Angle2Width("Angle 2 Width", Float) = .1
//		_Angle2Fade("Angle 2 Fade", Float) = .1
		_DangerSteepness("Danger Steepness", Float) = .1
		_Scale("Scale", Float) = 1

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    	[Toggle(SCREEN_SPACE)] _UseScreenSpace ("Screen Space Detail Texture", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
			CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
			#pragma shader_feature SCREEN_SPACE

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
            	#ifdef SCREEN_SPACE
				float4 screenPos : TEXCOORD1;
            	#endif
            	//float2 detailtexcoord : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _TintTex;
            sampler2D _DetailTex;
            float4 _DetailTex_ST;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _DetailTex_TexelSize;
            fixed4 _Color;
			fixed4 _AngleColor;
			fixed4 _DangerColor;
			float _TintIntensity;
			float _TintExponent;
			float _StartDepth;
			float _DepthRange;
			float _LineWidth;
			float _LineFade;
			float _AngleWidth;
			float _AngleFade;
			// float _Angle2Width;
			// float _Angle2Fade;
			float _DangerSteepness;
			float _Scale;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.vertex = UnityObjectToClipPos(v.vertex);

                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
            	#ifdef SCREEN_SPACE
				OUT.screenPos = ComputeScreenPos( OUT.vertex );
            	#endif

            	//OUT.detailtexcoord = TRANSFORM_TEX(OUT.worldPosition.xy, _MainTex);

                OUT.color = v.color * _Color;
                return OUT;
            }
        
            float2 calcGrad (float2 uv, float me)
            {
                float n = -tex2D(_DetailTex, float2(uv.x, uv.y + _DetailTex_TexelSize.y)).x;
                float e = -tex2D(_DetailTex, float2(uv.x + _DetailTex_TexelSize.x, uv.y)).x;
                return float2(e-me,n-me);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd);
            	
            	#ifdef SCREEN_SPACE
                float2 q = IN.screenPos.xy / IN.screenPos.w;
            	#else
                float2 q = IN.texcoord;
            	#endif
            	
				float h = -tex2D(_DetailTex, q).r;
            	//half4 c = tex2D(_TintTex, q);

				float4 col = float4(0,0,0,0);
				float blend = smoothstep(_StartDepth, _StartDepth + _DepthRange / 32, -h);
				
				// Calculate the direction in which the surface is facing
				float2 plan = calcGrad(q, h);
				
				float planmag = length(plan);
				
				float dangerblend = smoothstep(0,_DangerSteepness, pow(planmag / _Scale, 2));

            	half4 linecol = lerp(_Color,_DangerColor,dangerblend) * blend; 
				// Loop over isolines, computing a pseudo distance field for a number of height values
				float spacing = _DepthRange / 20;
				for (int ih = 1; ih <= 21; ih++) {
					float isoline = abs(h + _StartDepth + (ih*spacing));
					col += (1-smoothstep(_LineWidth, _LineWidth * _LineFade, isoline / planmag)) * linecol; // Isoline
				}
				
				float angle = atan2(plan.y,plan.x) / 3.1415926536 + 1;
				
				// float2 plannorm = normalize(plan);
				// float2 plan2uv = q + float2(-plannorm.y * _DetailTex_TexelSize.x * 2, plannorm.x * _DetailTex_TexelSize.y * 2);
				// float2 plan2 = calcGrad(plan2uv, -tex2D(_DetailTex, plan2uv).r);
				// float angle2 = atan2(plan2.y,plan2.x) / 3.1415926536 + 1;
				// float angmag = abs(angle-angle2);
				// angmag = min(angmag,0.025);
				half4 angcol = blend * (planmag / _Scale) * (lerp(_AngleColor, _DangerColor, dangerblend));
            	// + pow(c,_TintExponent) * _TintIntensity
				// Loop over angle isolines
				for (float ia = 0.5; ia < 13; ia++) {
				    float isoline = abs(angle - ia / 6.0);
					float l = 1-smoothstep(_AngleWidth, _AngleWidth * _AngleFade, isoline);
					l *= smoothstep(-_StartDepth - _DepthRange, -_StartDepth, h);
					col += l * angcol;
					// col += (1-smoothstep(_Angle2Width, _Angle2Width * _Angle2Fade, isoline / angmag)) * _AngleColor * blend; // Isoline
				}

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color * col;
            }
        ENDCG
        }
    }
}