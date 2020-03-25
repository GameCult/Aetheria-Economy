Shader "Unlit/Background"
{
    Properties
    {
		_Color1 ("Color 1", Color) = (1,1,1,1)
		_Color2 ("Color 2", Color) = (1,1,1,1)
		_Color3 ("Color 2", Color) = (1,1,1,1)
		NoisePosition("NoisePosition", Float) = 0.5
		NoiseAmplitude("NoiseAmplitude", Float) = 0.5
		NoiseOffset("NoiseOffset", Float) = 0.5
		NoiseGain("NoiseGain", Float) = 1
		NoiseLacunarity("NoiseLacunarity", Float) = 2
		NoiseFrequency("NoiseFrequency", Float) = 1
		Speed("Speed", Float) = 1
		ScrollSpeed("ScrollSpeed", Float) = 1
		Distortion("Distortion", Float) = 1
		ParallaxDirection("ParallaxDirection", Vector) = (0,0,0,0)
		Parallax("Parallax", Float) = 1
		
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
	    Cull Off Lighting Off ZWrite Off
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
			#include "Assets/Scripts/NIH/GPU Noise/SimplexNoiseGrad2D.cginc"

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

            half4 _Color1;
            half4 _Color2;
            half4 _Color3;
            float NoisePosition;
            float NoiseAmplitude;
            float NoiseOffset;
	        float NoiseGain;
	        float NoiseLacunarity;
	        float NoiseFrequency;
	        float Speed;
	        float ScrollSpeed;
	        float Distortion;
	        float2 ParallaxDirection;
	        float Parallax;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = mul (unity_ObjectToWorld, v.vertex).xy;
                return o;
            }
            
            // fractal sum, range -1.0 - 1.0
            float fbm(float2 p)
            {
                float freq = NoiseFrequency, amp = .5;
                float parallax = Parallax;
                float sum = 0;	
                for(int i = 0; i < 4; i++) 
                {
                    sum += snoise(p * freq + Parallax * ParallaxDirection) * amp;
                    parallax /= NoiseLacunarity;
                    freq *= NoiseLacunarity;
                    amp *= NoiseGain;
                }
                return (sum + NoiseOffset)*NoiseAmplitude;
            }
            
            // fractal sum, range -1.0 - 1.0
            float2 fbmgrad(float2 p)
            {
                float freq = NoiseFrequency, amp = .5;
                float parallax = Parallax;
                float2 sum = 0;	
                for(int i = 0; i < 4; i++) 
                {
                    sum += snoise_grad(p * freq + Parallax * ParallaxDirection) * amp;
                    parallax /= NoiseLacunarity;
                    freq *= NoiseLacunarity;
                    amp *= NoiseGain;
                }
                return (sum + NoiseOffset)*NoiseAmplitude;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3x3 rot = float3x3(cos(_Time.x*Speed), sin(_Time.x*Speed), 0,
                                       -sin(_Time.x*Speed), cos(_Time.x*Speed), 0,
                                       0,0,1);
                float3x3 rot2 = float3x3(cos(_Time.x*Speed*2), sin(_Time.x*Speed*2), 0,
                                       -sin(_Time.x*Speed*2), cos(_Time.x*Speed*2), 0,
                                       0,0,1);
                float2 offset0 = i.uv - float2(0,_Time.x*ScrollSpeed*.5);
                float noise0 = fbm(offset0);
                float2 offset1 = mul(float3(fbmgrad(offset0),1), rot).xy;
                float2 offsetuv1 = i.uv + offset1 * Distortion + float2(_Time.x*ScrollSpeed,0);
                float noise1 = fbm(offsetuv1);
                float2 offset2 = mul(float3(fbmgrad(offsetuv1),1), rot2).xy;
                float2 offsetuv2 = i.uv + offset2 * Distortion * .5 + float2(0,_Time.x*ScrollSpeed*.5);
                float noise2 = abs(fbm(offsetuv1));
                return float4(noise1*_Color1.rgb+abs(noise0-noise1)*_Color2.rgb + abs(noise0-noise2) * _Color3.rgb, 1);
            }
            ENDCG
        }
    }
}
