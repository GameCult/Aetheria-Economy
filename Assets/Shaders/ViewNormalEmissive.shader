// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

Shader "Aetheria/View Dependent Emissive" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_BumpMap ("Bumpmap", 2D) = "bump" {}
		_BumpPower ("Bump Power", Range (0.01,5)) = 1
		_Rotation ("Normal Rotation", Float) = 0
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_MetallicGlossMap("Metallic", 2D) = "white" {}
		_EmissionColor ("Emission Color", Color) = (1,1,1,1)
		_Emission ("Emission Strength", Float) = 0
        _EmissionFresnel ("Emission Fresnel", Float) = 1.0
		_EmissionRoughnessFresnelPower("Emission Roughness Fresnel Power", Float) = 1.0
		[MaterialToggle] _ReverseRim("ReverseRim", Float) = 0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows
		//#pragma multi_compile REVERSE_RIM_ON REVERSE_RIM_OFF

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _BumpMap;
		sampler2D _MetallicGlossMap;

		struct Input {
			float2 uv_MainTex;
			float2 uv_BumpMap;
			float3 viewDir;
			float3 worldNormal; 
			INTERNAL_DATA
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;
		fixed4 _EmissionColor;
		float _BumpPower;
		float _Rotation;
		float _Emission;
        float _EmissionFresnel;
        float _EmissionRoughnessFresnelPower;
		half _ReverseRim;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;

            float sinX = sin ( _Rotation );
            float cosX = cos ( _Rotation );
            float sinY = sin ( _Rotation );
            float2x2 rotationMatrix = float2x2( cosX, -sinX, sinY, cosX);

			fixed3 normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
			normal.xy = mul ( normal.xy, rotationMatrix);
			normal.z = normal.z / _BumpPower;
			normal = normalize(normal);
			o.Normal = normal;

			// Metallic and smoothness come from a texture
			float2 metalSmooth = tex2D (_MetallicGlossMap, IN.uv_MainTex).ra;
			o.Metallic = metalSmooth.x;
			o.Smoothness = metalSmooth.y * _Glossiness;

			half rim = saturate(dot (normalize(IN.viewDir), o.Normal));
			if(_ReverseRim > .5)
				rim = 1 - rim;
			o.Emission = _Emission * _EmissionColor * pow(rim,_EmissionFresnel / pow(metalSmooth.x, _EmissionRoughnessFresnelPower));

			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
