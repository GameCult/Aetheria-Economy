/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

Shader "UI/Influence"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
    	_DetailTex ("Influence Texture", 2D) = "white" {}
		_Color1 ("Primary Color", Color) = (1,1,1,1)
		_Color2 ("Secondary Color", Color) = (1,1,1,1)
		_Threshold("Border Threshold", Float) = 0.05
		_FillTiling("Fill Tiling", Float) = 50
		_FillBorderBlend("Fill Border Blend", Float) = .05
		_FillBlend("Fill Blend", Float) = .05
		_PatternBlend("Pattern Blend", Float) = .05
		_PatternOffset("Pattern Offset", Float) = .05
		_FillAlpha("Fill Alpha", Float) = .5
		_FillTilt("Fill Tilt", Range(0,3.14)) = 1
		_Stroke("Stroke Weight", Float) = 2
		_StrokeBlend("Stroke Blend", Float) = .05
		_StrokeTransitionBlend("Stroke Transition Blend", Float) = .05

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

            //#pragma multi_compile_local _ UNITY_UI_CLIP_RECT
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
            fixed4 _Color1;
            fixed4 _Color2;
            half _FillTiling;
            half _FillBlend;
            half _FillBorderBlend;
            half _FillAlpha;
            half _FillTilt;
            half _PatternBlend;
            half _PatternOffset;
            half _Threshold;
            half _Stroke;
            half _StrokeBlend;
            half _StrokeTransitionBlend;

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

                OUT.color = v.color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd);
            	
            	#ifdef SCREEN_SPACE
                float2 q = IN.screenPos.xy / IN.screenPos.w;
            	#else
                float2 q = IN.texcoord;
            	#endif

            	// Sample influence texture
                float influence = tex2D(_DetailTex, q).r;

            	// Calculate gradient and rate of change of influence
				float2 grad = float2(ddx(influence), ddy(influence));
				float diff = length(grad);

            	// Tilt determines the vector along which we measure the uv coordinate
            	float2 patternDirection = float2(sin(_FillTilt), cos(_FillTilt));

            	// Repeat a sine wave along the pattern direction and smoothstep it to interpolate between the two pattern colors
            	fixed4 pattern = lerp(_Color1, _Color2, smoothstep(-_PatternBlend, _PatternBlend, _PatternOffset + sin(dot(patternDirection, q * _FillTiling))));

            	float4 col = pattern *
            		smoothstep(_Threshold, _Threshold + _FillBorderBlend, influence) * 
            		smoothstep(_Threshold + _FillBlend, _Threshold, influence) *
            		_FillAlpha;

            	// Draw a line at the threshold
				float isoline = abs(influence - _Threshold) / diff;
            	col = smoothstep(0,_Threshold, influence) *
            		lerp(col,
            			lerp(_Color1, _Color2, smoothstep(_StrokeTransitionBlend*diff, -_StrokeTransitionBlend*diff, influence - _Threshold)),
            			smoothstep(_Stroke * (1 + _StrokeBlend), _Stroke, isoline));

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color * col;
            }
        ENDCG
        }
    }
}