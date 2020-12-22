/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

Shader "Unlit/GalaxyMap"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_CloudColor ("CloudColor", Color) = (1,1,1,1)
		_GlowColor ("GlowColor", Color) = (1,1,1,1)
		Arms("Arms", Float) = 4
		Twist("Twist", Float) = 180
		TwistPower("TwistPower", Float) = 1
		SpokeOffset("SpokeOffset", Float) = 1
		SpokeScale("SpokeScale", Float) = 1
		CoreBoost("CoreBoost", Float) = 1
		CoreBoostOffset("CoreBoostOffset", Float) = 0
		CoreBoostPower("CoreBoostPower", Float) = 1
		EdgeReduction("EdgeReduction", Float) = 1
		NoisePosition("NoisePosition", Float) = 0.5
		NoiseAmplitude("NoiseAmplitude", Float) = 0.5
		NoiseOffset("NoiseOffset", Float) = 0.5
		NoiseGain("NoiseGain", Float) = 1
		NoiseLacunarity("NoiseLacunarity", Float) = 2
		NoiseFrequency("NoiseFrequency", Float) = 1
		GlowOffset("GlowOffset", Float) = 0
		GlowAmount("GlowAmount", Float) = 0
		GlowPower("GlowPower", Float) = 0
		StarBoost("StarBoost", Float) = 0
		GalaxyAmplitude("GalaxyAmplitude", Float) = 1
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
			
			#include "UnityCG.cginc"
			#include "Assets/Scripts/NIH/GPU Noise/SimplexNoise2D.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float2 uv2 : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
		    fixed4 _CloudColor;
		    fixed4 _GlowColor;
			float Arms;
			float Twist;
			float TwistPower;
	        float SpokeOffset;
	        float SpokeScale;
			
            float CoreBoost;
	        float CoreBoostOffset;
	        float CoreBoostPower;
	        float EdgeReduction;
            float NoisePosition;
            float NoiseAmplitude;
            float NoiseOffset;
	        float NoiseGain;
	        float NoiseLacunarity;
	        //int NoiseOctaves = 7;
	        float NoiseFrequency;
	        float GlowOffset;
	        float GlowAmount;
	        float GlowPower;
	        float StarBoost;
	        float GalaxyAmplitude;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.uv2 = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
            // fractal sum, range -1.0 - 1.0
            float fBm(float2 p, int octaves)
            {
                float freq = NoiseFrequency, amp = .5;
                float sum = 0;	
                for(int i = 0; i < octaves; i++) 
                {
                    sum += snoise(p * freq) * amp;
                    freq *= NoiseLacunarity;
                    amp *= NoiseGain;
                }
                return (sum + NoiseOffset)*NoiseAmplitude;
            }
			
			fixed4 frag (v2f i) : SV_Target
			{
			    float2 offset = -(.5).xx+i.uv;
			    float circle = (.5-length(offset))*2;
			    clip(circle);
			    float angle = pow(length(offset)*2,TwistPower) * Twist;
			    float2 twist = float2(offset.x*cos(angle) - offset.y*sin(angle), offset.x*sin(angle) + offset.y*cos(angle));
			    float atan = atan2(twist.y,twist.x);
			    float spokes = (sin(atan*Arms) + SpokeOffset) * SpokeScale;
			    float noise = fBm(i.uv + NoisePosition.xx, 8);
			    float shape = lerp(spokes - EdgeReduction * length(offset), 1, pow(circle, CoreBoostPower) * CoreBoost + CoreBoostOffset);
			    float gal = max(shape - noise * clamp(circle,0,1), 0) * GalaxyAmplitude;
			    float glow = (pow(circle,GlowPower) + GlowOffset) * GlowAmount;
			    float stars = tex2D(_MainTex, i.uv2) * clamp(shape*StarBoost, 0, 1);
				fixed4 col = fixed4(gal*_CloudColor.rgb + glow*_GlowColor.rgb + stars.xxx, 1);
				return col; 
			}
			ENDCG
		}
	}
}
