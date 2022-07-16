// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Aetheria/Volume Jupiter"
{
	Properties
	{
		_ColorRamp ("Color Ramp", 2D) = "white" {}
		_Offset ("Offset", CUBE) = "gray" {}
		_Albedo ("Albedo", CUBE) = "black" {}
		_RayStepSize("Ray Step Size", Float) = .01
		_FirstOffsetDistance("First Offset Distance", Float) = .01
		_FirstOffsetDepthExponent("First Offset Depth Exponent", Float) = 1
		_SecondOffsetDistance("Second Offset Distance", Float) = .01
		_SecondOffsetDepthExponent("Second Offset Depth Exponent", Float) = 1
//		_Striations ("Striations", Float) = 10
//		_StriationOffset ("Striation Offset", Float) = 0
		_LightingDirection("Lighting Direction", Vector) = (1,0,0,0)
		_LightingPower("Lighting Power", Float) = 1
		_LightingWrap("Lighting Wrap", Float) = 1
		_LightingAmbient("Lighting Ambient", Float) = .1
		_DensityDepthExponent("Density Depth Exponent", Float) = 1
		_DensityAlbedoExponent("Density Albedo Exponent", Float) = 1
		_NoiseFrequency("Noise Frequency", Float) = 1
		_NoiseAmplitude("Noise Amplitude", Float) = 1
		_NoiseSpeed("Noise Speed", Float) = 1
        _Emission("Emission", Float) = 1
        _Alpha("Alpha Multiplier", Float) = 1
        _Glossiness("Gloss", Range(0,1)) = 1
        _Metallic("Metallic", Range(0,1)) = 1
        //_EdgeTransparency("Edge Transparency", Float) = 1
	}
	
	SubShader
	{
        Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM

		#pragma surface surf Lambert vertex:vert noambient 
		#pragma target 3.0
        #include "UnityPBSLighting.cginc"
        #include "Assets/Shaders/Dither Functions.cginc"
        #include "Assets/Shaders/Volumetric.cginc"

		sampler2D _ColorRamp;
		samplerCUBE _Offset;
		samplerCUBE _Albedo;
 
		struct Input {
			float4 screenPos;
			float3 worldNormal;
			float3 viewDir;
			float3 ambientColor;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		float _RayStepSize;
		float _FirstOffsetDistance;
		float _SecondOffsetDistance;
		float _FirstOffsetDepthExponent;
		float _SecondOffsetDepthExponent;
		float _DensityDepthExponent;
		float _DensityAlbedoExponent;
		float _Alpha;
		float _Emission;

		float4x4 _AlbedoRotation;
		float4x4 _FirstOffsetDomainRotation;
		float4x4 _FirstOffsetRotation;
		float4x4 _SecondOffsetDomainRotation;
		float4x4 _SecondOffsetRotation;

		float4 _LightingDirection;
		half _LightingPower;
		half _LightingWrap;
		half _LightingAmbient;
		// half _Striations;
		// half _StriationOffset;
		half _NoiseFrequency;
		half _NoiseAmplitude;
		half _NoiseSpeed;

		void vert (inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input,o);
            float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
            float3 worldNormal = UnityObjectToWorldNormal(v.normal);
            o.ambientColor = VolumeSampleColorSimple(worldPos, worldNormal).rgb * 10;
		}
 
        // #define UINT_MAXF 4294967290.0f
        //
        // uint rngState;
        // int randomOffset;
        //
        // float Random(uint seed)
        // {
        //     rngState = seed + randomOffset; //also add the random offset so we globally randomize the seeds each frame
        //     //wang hash the seed first to get rid of correlation.
        //     rngState = (rngState ^ 61) ^ (rngState >> 16);
        //     rngState *= 9;
        //     rngState = rngState ^ (rngState >> 4);
        //     rngState *= 0x27d4eb2d;
        //     rngState = rngState ^ (rngState >> 15);
        //     
        //
        //     return (float)rngState / UINT_MAXF;
        // }

		// float tri(in float x){return abs(frac(x)-.5);}
		// float3 tri3(in float3 p){return float3( tri(p.z+tri(p.y*1.)), tri(p.z+tri(p.x*1.)), tri(p.y+tri(p.x*1.)));}
		//
		// float triNoise3d(in float3 p, in float spd)
		// {
		//     float z=1.4;
		// 	float rz = 0.;
		//     float3 bp = p;
		// 	for (float i=0.; i<=2.; i++ )
		// 	{
		//         float3 dg = tri3(bp*2.);
		//         p += (dg+_Time.y*spd);
		//
		//         bp *= 1.8;
		// 		z *= 1.5;
		// 		p *= 1.2;
		//         
		//         rz+= (tri(p.z+tri(p.x+tri(p.y))))/z;
		//         bp += 0.14;
		// 	}
		// 	return rz;
		// }
		// void modifyColor (Input IN, SurfaceOutput o, inout fixed4 color)
		// {
		// 	color *= _ColorTint;
		// }
		
		void surf (Input IN, inout SurfaceOutput o)
		{
			float rim = saturate(dot(IN.viewDir,IN.worldNormal));
        	float2 screenPos = IN.screenPos.xy / IN.screenPos.w;
        	
			float3 rayPos = IN.worldNormal;
			float3 rayStep = normalize(IN.viewDir)*_RayStepSize*(2-rim);
			float dither = tex2D(_DitheringTex, (screenPos + .5) * _DitheringCoords.xy + _DitheringCoords.zw).r * 2;
			rayPos += rayStep * dither;
        	
			float3 accum = 0;
        	float alphaAccum = 0;
			for(int i=0;i<32;i++){
				float depth = 1 - length(rayPos);
				float turbulence = 1.25 - abs(rayPos.y) * .5;

				float3 firstSamplePosition = mul((float3x3)_FirstOffsetDomainRotation, rayPos);
				float3 offset =  mul((float3x3)_FirstOffsetRotation,
									 normalize(texCUBElod(_Offset, float4(firstSamplePosition, i/16)).rgb - float3(.5,.5,.5))) *
									 _FirstOffsetDistance * (pow(length(rayPos),_FirstOffsetDepthExponent)) * turbulence;

				float3 secondSamplePosition = mul((float3x3)_SecondOffsetDomainRotation, rayPos + offset);
				float3 offset2 = mul((float3x3)_SecondOffsetRotation, 
									 normalize(texCUBElod(_Offset, float4(secondSamplePosition, i/8)).rgb - float3(.5,.5,.5))) * 
									 _SecondOffsetDistance * (pow(length(rayPos),_SecondOffsetDepthExponent)) * turbulence;

				// float gradientPos = Random(((rayPos + offset).y+1)*_Striations+_StriationOffset);
				float albedo = texCUBElod (_Albedo, float4(mul((float3x3)_AlbedoRotation,normalize(rayPos + offset2)) ,i/8)).x;
				//float noise = triNoise3d(firstSamplePosition * _NoiseFrequency, _NoiseSpeed) * _NoiseAmplitude;

				float density = pow(max(depth,0),_DensityDepthExponent) * pow(albedo,_DensityAlbedoExponent);// * max(1 - noise,0.01);
				alphaAccum += density;
				accum += tex2D(_ColorRamp, secondSamplePosition.y+.5f) * density / depth;
				            
				rayPos += rayStep;
			}
        	ditherClip(screenPos, alphaAccum * _Alpha);

        	o.Albedo = accum * _Emission;
			o.Emission = accum * IN.ambientColor * _LightingAmbient;
//            float4 envSample = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, IN.worldNormal);
//            float3 spec = DecodeHDR(envSample, unity_SpecCube0_HDR).rgb;
			//o.Emission = lerp(envSample.rgb,accum*_Emission,saturate(length(accum)*_AlphaPower));
			//o.Emission = accum*_Emission;// + spec*_Emission
			o.Alpha = 1;//;
		}
		ENDCG
	}
	FallBack "Standard"
}
