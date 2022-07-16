/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

Shader "Aetheria/Shield"
{
    Properties
    {
        [HDR]_Color ("Color", Color) = (1,1,1,1)
		_Offset ("Offset", CUBE) = "gray" {}
		_Albedo ("Albedo", CUBE) = "black" {}
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _TextureGamma ("Texture Gamma", Float) = 2
        _AlbedoGamma ("Albedo Gamma", Float) = 2
        _Gradient ("Gradient", 2D) = "white" {}
        _GradientMul ("Gradient Multiplier", Float) = 1
        _Impact ("Impact Direction", Vector) = (0,0,1)
        _Radius ("Impact Radius", Float) = .1
        _Blend ("Impact Blend", Float) = 2
        _NoiseSpeed ("Noise Speed", Float) = 2
        _NoiseScale ("Noise Scale", Float) = .1
        _NoiseAmplitude ("Noise Amplitude", Float) = .1
        _OffsetSize ("Offset Scale", Float) = .1
        _Alpha ("Alpha", Float) = 1
    	_DitherSampleOffset ("Dither Offset", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        Cull Off

        CGPROGRAM
        #include "Dither Functions.cginc"
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _Gradient;
		samplerCUBE _Offset;
		samplerCUBE _Albedo;

        struct appdata_particles {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            float2 texcoord : TEXCOORD0;
        };

        struct Input {
            float2 uv_MainTex;
            float4 screenPos;
            float3 objectPos;
        };

		float tri(in float x){return abs(frac(x)-.5);}
		float3 tri3(in float3 p){return float3( tri(p.z+tri(p.y*1.)), tri(p.z+tri(p.x*1.)), tri(p.y+tri(p.x*1.)));}

		float volumeNoise(in float3 p, in float spd)
		{
		    float z=1.4;
			float rz = 0.;
		    float3 bp = p;
			for (float i=0.; i<=2.; i++ )
			{
		        float3 dg = tri3(bp*2.);
		        p += (dg+_Time.y*spd);

		        bp *= 1.8;
				z *= 1.5;
				p *= 1.2;
		        
		        rz+= (tri(p.z+tri(p.x+tri(p.y))))/z;
		        bp += 0.14;
			}
			return rz;
		}

        void vert(inout appdata_particles v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input,o);
            o.uv_MainTex = v.texcoord.xy;
            o.objectPos = v.vertex.xyz;
        }

        fixed4 _Color;
        float3 _Impact;
        float _Radius;
        float _Blend;
        float _NoiseSpeed;
        float _NoiseScale;
        float _NoiseAmplitude;
        float _OffsetSize;
        float _TextureGamma;
        float _AlbedoGamma;
        float _Alpha;
        float _DitherSampleOffset;
        float _GradientMul;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            float a = pow(tex2D (_MainTex, IN.uv_MainTex).a, _TextureGamma);// * _Color * IN.color;

        	float3 pos = normalize(IN.objectPos);
			float noise = volumeNoise(pos*_NoiseScale, _NoiseSpeed) * _NoiseAmplitude;
			float3 offset = normalize(texCUBElod(_Offset, float4(pos + noise, 0)).rgb - float3(.5,.5,.5));
			float albedo = pow(texCUBElod (_Albedo, float4(pos + offset * _OffsetSize, 0)).x, _AlbedoGamma);
            
            float dist = dot(pos,normalize(_Impact));
            a *= albedo;
        	float cl = a;
        	a *= _Alpha * smoothstep(1 - _Radius, 1 - _Radius * _Blend, dist);
        	
            // Apply screen space dithering
		    float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
            ditherClip(screenUV + _DitherSampleOffset, a);

        	fixed3 c = tex2D (_Gradient, cl*_GradientMul) * _Color;

            o.Emission = c.rgb;
            
            // Metallic and smoothness come from slider variables
            o.Metallic = 0;
            o.Smoothness = 0;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
