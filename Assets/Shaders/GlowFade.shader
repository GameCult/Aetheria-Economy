Shader "Aetheria/GlowFade"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        
        _Fade ("Fade", Range(0.0, 1.0)) = 0.0
        _FadeSharpness ("Fade Sharpness", Float) = 1.0
        _FadeTex ("Fade Texture", 2D) = "white" {}
        _FadeEdge ("Fade Edge", Range(0.0, 1.0)) = 1.0
        [HDR] _EdgeColor ("Edge Color", Color) = (1,1,1,1)
        
        _Glossiness("Smoothness", Range(0.0, 1.0)) = 1.0
        [Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _MetallicGlossMap("Metallic", 2D) = "white" {}
 
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1.0

        _OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        Cull Off

        CGPROGRAM
        #include "Dither Functions.cginc"
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _MetallicGlossMap;
        sampler2D _OcclusionMap;
        sampler2D _FadeTex;
        sampler2D _BumpMap;

        struct Input
        {
            float2 uv_MainTex;
			float2 uv_FadeTex;
            float4 screenPos;
        };

        half _Fade;
        half _FadeSharpness;
        half _FadeEdge;
        half _Glossiness;
        half _Metallic;
        half _BumpScale;
        half _OcclusionStrength;
        fixed4 _Color;
        half3 _EdgeColor;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            half f = tex2D (_FadeTex, IN.uv_FadeTex).r + (1-_Fade);
            half3 e = _EdgeColor * smoothstep(1 - _FadeEdge, 1, f);
            o.Emission = e;
            // Apply screen space dithering
		    float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
            ditherClip(screenUV, _FadeSharpness - f * _FadeSharpness);
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex)) * _BumpScale;
            fixed4 m = tex2D (_MetallicGlossMap, IN.uv_MainTex);
            o.Metallic = _Metallic * m.r;
            o.Smoothness = _Glossiness * m.a;
            fixed4 oc = tex2D (_OcclusionMap, IN.uv_MainTex);
            o.Occlusion = oc.r * _OcclusionStrength;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
