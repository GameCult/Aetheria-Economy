Shader "Aetheria/Influence"
{
    Properties
    {
        _MainTex ("Influence Map", 2D) = "white" {}
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
    }
    SubShader
    {
	    Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
	    Cull Off Lighting Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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

            fixed4 frag (v2f i) : SV_Target
            {
            	// Sample influence texture
                float influence = tex2D(_MainTex, i.uv).r;

            	// Calculate gradient and rate of change of influence
				//float2 grad = calcGrad(i.uv, influence);
				//float diff = length(grad);
				float2 grad = float2(ddx(influence), ddy(influence));
				float diff = length(grad);

            	// Tilt determines the vector along which we measure the uv coordinate
            	float2 patternDirection = float2(sin(_FillTilt), cos(_FillTilt));

            	// Repeat a sine wave along the pattern direction and smoothstep it to interpolate between the two pattern colors
            	fixed4 pattern = lerp(_Color1, _Color2, smoothstep(-_PatternBlend, _PatternBlend, _PatternOffset + sin(dot(patternDirection, i.uv * _FillTiling))));

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
            	
                return col;
            }
            ENDCG
        }
    }
}
