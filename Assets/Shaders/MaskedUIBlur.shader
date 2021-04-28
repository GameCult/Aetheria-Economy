Shader "Custom/MaskedUIBlur" {
    Properties {
        _Size ("Blur", Range(0, 30)) = 1
        [HideInInspector] _MainTex ("Masking Texture", 2D) = "white" {}
        _AdditiveColor ("Additive Tint color", Color) = (0, 0, 0, 0)
        _MultiplyColor ("Multiply Tint color", Color) = (1, 1, 1, 1)
    }

    Category {

        // We must be transparent, so other objects are drawn before this one.
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Opaque" }


        SubShader
        {
            /*
            ZTest Off
            Blend SrcAlpha OneMinusSrcAlpha
            */

            Cull Off
            Lighting Off
            ZWrite Off
            ZTest [unity_GUIZTestMode]
            Blend SrcAlpha OneMinusSrcAlpha

            Pass
            {          
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma fragmentoption ARB_precision_hint_fastest
                #include "UnityCG.cginc"

                struct appdata_t {
                    float4 vertex   : POSITION;
                    float4 color    : COLOR;
                    float2 texcoord : TEXCOORD0;
                };

                struct v2f {
                    float4 vertex : POSITION;
                    fixed4 color  : COLOR;
                    float4 uvgrab : TEXCOORD0;
                    float2 uvmain : TEXCOORD1;
                };

                sampler2D _MainTex;
                float4 _MainTex_ST;
                float4 _MultiplyColor;

                v2f vert (appdata_t v) {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);

                    // #if UNITY_UV_STARTS_AT_TOP
                    // float scale = -1.0;
                    // #else
                    // float scale = 1.0;
                    // #endif

                    o.uvgrab.xy = (float2(o.vertex.x, o.vertex.y) + o.vertex.w) * 0.5;
                    o.uvgrab.zw = o.vertex.zw;

                    o.uvmain = TRANSFORM_TEX(v.texcoord, _MainTex);
                    o.color = v.color * _MultiplyColor;

                    return o;
                }

                sampler2D _CameraBlur;
                float4 _CameraBlur_TexelSize;
                float _Size;
                float4 _AdditiveColor;

                half4 frag( v2f i ) : COLOR
                {
                    half4 sum = tex2D( _CameraBlur, UNITY_PROJ_COORD(float4(i.uvgrab.x, i.uvgrab.y, i.uvgrab.z, i.uvgrab.w)).xy);

                    half4 result = half4(sum.r * i.color.r + _AdditiveColor.r, 
                                        sum.g * i.color.g + _AdditiveColor.g, 
                                        sum.b * i.color.b + _AdditiveColor.b, 
                                        tex2D(_MainTex, i.uvmain).a * i.color.a);
                    return result;
                }
                ENDCG
            }
        }
    }
}