// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Aetheria/Field"
{
	Properties
	{
		_Displacement("Max Displacement", Range(0, 1.0)) = 0.3
		_SubdivisionAmount("Subivision Amount", Range(1, 10)) = 1
//		_Color("Color", Color) = (1,1,1,1)
//		_Metallic("Metallic", Range(0, 1)) = 1
//		_Gloss("Gloss", Range(0, 1)) = 0.8
		_Ambient_Multiplier("Ambient Multiplier", Float) = 5
		
		_EmissionColor ("Emission Color", Color) = (1,1,1,1)
		_Emission ("Emission Strength", Float) = 0
        _EmissionFresnel ("Emission Fresnel", Float) = 1.0
		
		_Background ("Background", CUBE) = "" {}
        _BackgroundFresnel ("Background Fresnel", Float) = 1.0
		_BackgroundBlur ("Background Blur", Float) = 2
		
		_PushWaveMagnitude ("Push Magnitude", Float) = .5
		_PushWaveFrequency ("Push Frequency", Float) = 2
		_PushWaveRangeExponent ("Push Range Exponent", Float) = 2
		_PushWaveEnvelopeExponent ("Push Envelope Exponent", Float) = 2
		_PushWaveExponent ("Push Wave Exponent", Float) = 2
		_PushWaveRange ("Push Range", Range(0,3.141592654)) = 1
		_PushWaveOffset ("Push Time", Float) = .5
		_PushDirection ("Push Direction", Vector) = (0,0,1)
		_PushOpacity ("Push Opacity", Range(0,1)) = 1
		_PushOpacityRangeMin ("Push Opacity Range Start", Range(0,1)) = .5
		_PushOpacityRangeMax ("Push Opacity Range End", Range(0,1)) = .25
		
		_FresnelOpacity ("Fresnel Opacity", Range(0,1)) = 1
		_OpacityFresnel ("Opacity Fresnel", Float) = 1
		_DisplacementOpacity ("Displacement Opacity", Float) = 1
		_DeviationOpacityExponent ("Deviation Opacity Exponent", Float) = 8
	}

	SubShader
	{
		Pass
		{
			Tags {"LightMode" = "Deferred"}
			Cull Off

			CGPROGRAM
			#pragma vertex vertex_shader
			#pragma hull hull_shader
			#pragma domain domain_shader
			#pragma fragment pixel_shader
			#pragma multi_compile ___ UNITY_HDR_ON
			//#pragma exclude_renderers nomrt
			#pragma target 4.6
			#pragma glsl

			//#define UNITY_PASS_DEFERRED
			#include "UnityPBSLighting.cginc"
			#include "Dither Functions.cginc"
			
// #include "UnityShaderVariables.cginc"
// #include "UnityStandardCore.cginc"
			//#include "HLSLSupport.cginc"
			//#include "UnityShaderVariables.cginc"
			//#include "Lighting.cginc"
			//#include "AutoLight.cginc"

			//#pragma only_renderers d3d11

			uniform sampler2D _LightBuffer;

			float _SubdivisionAmount;
			float _Displacement;

			// half4 _Color;
			// float _Metallic;
			// float _Gloss;
			float _Ambient_Multiplier;
			
			samplerCUBE _Background;
			float _Emission;
			half4 _EmissionColor;
			float _EmissionFresnel;
			float _BackgroundBlur;
			float _BackgroundFresnel;
			float _FresnelOpacity;
			float _OpacityFresnel;

			float _PushWaveMagnitude;
			float _PushWaveFrequency;
			float _PushWaveRangeExponent;
			float _PushWaveExponent;
			float _PushWaveEnvelopeExponent;
			float _PushWaveRange;
			float _PushWaveOffset;
			float _PushOpacity;
			float _PushOpacityRangeMin;
			float _PushOpacityRangeMax;
			float _DisplacementOpacity;
			float _DeviationOpacityExponent;

			float _MeleeDisplacement;
			float _MeleeShape;
			float _MeleeFlattening;
			float3 _MeleeDirection;

			int _HitCount;
		    struct FieldHit
		    {
		        float3 Position;
		        float3 Direction;
		        float Magnitude;
		        float Time;
		    };
            StructuredBuffer<FieldHit> _Hits;
			
			uniform float3 _PushDirection;
			uniform float3 _InverseScale;
			uniform float4x4 _ReflRotate;

			samplerCUBE _Offset;
			
			float2 uv_MainTex;

			sampler2D _NormalTex;
			SamplerState sampler_NormalTex;

			sampler2D _DispTex;
			SamplerState sampler_DispTex;

			struct HS_CONSTANT_DATA_OUTPUT
			{
				float Edges[4] : SV_TessFactor;
				float Inside[2] : SV_InsideTessFactor;
			};
			
			struct appdata_t {
			    float4 vertex : POSITION;
			    float4 tangent : TANGENT;
			};

			// Apparently this uses VS_OUTPUT, but step is redundant.
			void vertex_shader(inout appdata_t v) {}

			HS_CONSTANT_DATA_OUTPUT constantsHS(InputPatch<appdata_t, 4> patch)
			{
				HS_CONSTANT_DATA_OUTPUT output;
				output.Edges[0] = output.Edges[1] = output.Edges[2] = output.Edges[3] = _SubdivisionAmount;
				output.Inside[0] = output.Inside[1] = _SubdivisionAmount;
				return output;
			}

			// I think this is what they mean by fixedfunction. The tessellation part is fixedfunction.
			[domain("quad")]
			[partitioning("integer")] // [partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[outputcontrolpoints(4)]
			[patchconstantfunc("constantsHS")]
			appdata_t hull_shader(InputPatch<appdata_t, 4> patch, uint id : SV_OutputControlPointID)//, uint pid : SV_PrimitiveID)
			{
				return patch[id];
			}
			
			// Declare GBuffers.
			struct structurePS
			{
				float4 albedo : SV_Target0;
				float4 specular : SV_Target1;
				float4 normal : SV_Target2;
				float4 emission : SV_Target3;
				//float depthSV : Depth;
			};

			// q = lerped position according to SV_DomainLocation.
			// p = patch position, in sequential order [0], [1], etc...
			// n = patch normal, in sequential order [0], [1], etc...
			float3 PhongOperator(float3 q, float3 p, float3 n)
			{
				return q - dot(q - p, n) * n;
			}
			
			float smootherstep(float edge0, float edge1, float x) {
				// Scale, and clamp x to 0..1 range
				x = saturate((x - edge0) / (edge1 - edge0));
				// Evaluate polynomial
				return x * x * x * (x * (x * 6 - 15) + 10);
			}
			
			struct v2f {
			    float4 vertex : POSITION;
			    float4 tangent : TANGENT;
			    float3 normal : NORMAL;
				float4 screenPos : TEXCOORD1;
				float3 viewDirection : TEXCOORD2;
			};
			
			float3 SampleDisplacement (float3 normal)
			{
				float3 disp = 0;
				float l = smoothstep(UNITY_PI, 0, acos(dot(_PushDirection,normal)));
				float pushwave = pow((sin(pow(l, _PushWaveRangeExponent)*UNITY_PI*_PushWaveFrequency - _PushWaveOffset * UNITY_TWO_PI)+1)/2, _PushWaveExponent);
				if(_PushWaveMagnitude<0) pushwave = 1-pushwave;
				disp += normal * pow(l, _PushWaveEnvelopeExponent) * pushwave * abs(_PushWaveMagnitude);
				float3 scaledNormal = normal/_InverseScale;
				for(int i=0; i<_HitCount; i++)
				{
					FieldHit hit = _Hits[i];
					float hitroot = sqrt(hit.Magnitude);
					float invsqtime = pow(1-hit.Time, 4);
					float3 toHit = hit.Position/_InverseScale-scaledNormal;
					float envelope = pow(smoothstep(hit.Magnitude + 4, 0, length(toHit)), 8 / (hit.Time));
					disp += lerp(hit.Direction, normal, hit.Time) * (envelope * pow(cos(envelope*hitroot*hit.Time*6)+1,2) * hitroot * invsqtime * _InverseScale.y);
				}
				if(_MeleeDisplacement>.01)
				{
					normal.y /= _MeleeFlattening;
					normal = normalize(normal);
					disp += _MeleeDirection * pow(saturate(1 - acos(dot(normal,_MeleeDirection))/(UNITY_PI/2)),_MeleeShape) * _MeleeDisplacement;
				}
				return disp;
				//return texCUBElod(_Offset, float4(normal, 0)).r;
			}

			// DOMAIN SHADER.
			// We process displacement in here apparently... Used to be a lot of things, a vertex struct, a pixel struct, this is the new one.
			// a = top Mid Position.
			// b = bottom Mid Position.
			[domain("quad")]
			v2f domain_shader(HS_CONSTANT_DATA_OUTPUT input, const OutputPatch<appdata_t, 4> patch, float2 UV : SV_DomainLocation)// : SV_POSITION
			{
				v2f output;

				// Bilinear interpolation of position.
				float3 a = lerp(patch[0].vertex, patch[1].vertex, UV.x);
				float3 b = lerp(patch[3].vertex, patch[2].vertex, UV.x);

				float3 newPosition = normalize(lerp(a, b, UV.y));

				// Bilinear interpolation of normals.
				float4 tangent_a = lerp(patch[0].tangent, patch[1].tangent, UV.x);
				float4 tangent_b = lerp(patch[3].tangent, patch[2].tangent, UV.x);
				
				// Output Data.
				output.normal = newPosition;
				output.tangent = lerp(tangent_a, tangent_b, UV.y);

				float3 displacement = SampleDisplacement(newPosition);
				
				// Displace vertices along surface normal vector.
				float3 newVertexPos = newPosition + _InverseScale * displacement; // normalize(newPosition * _InverseScale)

				// Output data.
				output.vertex = UnityObjectToClipPos(float4(newVertexPos, 1));
				output.screenPos = ComputeScreenPos(output.vertex);
				// float3 objSpaceCameraPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos.xyz, 1)).xyz;
				// output.viewDirection = normalize(objSpaceCameraPos-newVertexPos);
				output.viewDirection = normalize(WorldSpaceViewDir(float4(newVertexPos, 1)));

				return output;

			}
			
			inline float4 AngleAxis (float radians, float3 axis)
			{
		        radians *= 0.5;
		        axis = axis * sin(radians);
		        return float4(axis.x, axis.y, axis.z, cos(radians));
			}
			 
			inline float3 Rotate (float4 rot, float3 v)
			{
		        float3 a = rot.xyz * 2.0;
		        float3 r0 = rot.xyz * a;
		        float3 r1 = rot.xxy * a.yzz;
		        float3 r2 = a.xyz * rot.w;
		 
		        return float3(
		                dot(v, float3(1.0 - (r0.y + r0.z), r1.x - r2.z, r1.y + r2.y)),
		                dot(v, float3(r1.x + r2.z, 1.0 - (r0.x + r0.z), r1.z - r2.x)),
		                dot(v, float3(r1.y - r2.y, r1.z + r2.x, 1.0 - (r0.x + r0.y))));
			}
			 
			float3 CalculateNormal (float3 n, float4 t, float textureSize, out float height)
			{
		        float pixel = 3.14159265 / textureSize;
		        float3 binormal = cross(n, t.xyz) * (t.w * unity_WorldTransformParams.w);
		        float3 x1 = Rotate(AngleAxis(pixel, binormal), n);
		        float3 z1 = Rotate(AngleAxis(pixel, t.xyz), n);

				float3 nd = SampleDisplacement(n);
				height = length(nd);
		        n += nd * _InverseScale;
		        x1 += SampleDisplacement(x1) * _InverseScale;
		        z1 += SampleDisplacement(z1) * _InverseScale;
		 
		        float3 right = (x1 - n);
		        float3 forward = (z1 - n);
		        float3 normal = cross(right, forward);
		        normal = normalize(normal);
		 
		        if (dot(normal, n) <= 0.0) normal = -normal;
		        return normal;
			}

			structurePS pixel_shader(v2f input) : SV_Target
			{
				structurePS deferredStruct;
				deferredStruct.albedo.rgb = 0;
				deferredStruct.albedo.a = 0;
				deferredStruct.specular = 0;

				input.normal = normalize(input.normal);
				float height;
				float3 normal = CalculateNormal(input.normal, input.tangent, 4096.0, height);

				normal = UnityObjectToWorldNormal(normal);
				float deviation = 1 - pow(abs(dot(UnityObjectToWorldNormal(input.normal), normal)), _DeviationOpacityExponent);
				normal = dot(input.viewDirection, normal) > 0 ? normal : -normal;
				float rim = dot(input.viewDirection, normal);
				deferredStruct.normal = float4(normal * 0.5 + 0.5, 1);

				deferredStruct.emission = float4(0, 0, 0, 1);
				
			    float3 dir = mul(_ReflRotate, refract(input.viewDirection,normal,_BackgroundFresnel));
				//float lerp = smoothstep(UNITY_PI, 0, pow(acos(dot(_PushDirection,input.normal)), 2));
				float2 screenUV = input.screenPos.xy / input.screenPos.w;
				float l = pow(smoothstep(UNITY_PI * _PushOpacityRangeMin, UNITY_PI * _PushOpacityRangeMax, acos(dot(_PushDirection,input.normal))), 4);
		        ditherClip(screenUV, abs(height) * _DisplacementOpacity + deviation + l * _PushOpacity + pow(1-rim, _OpacityFresnel) * _FresnelOpacity);
				deferredStruct.emission.rgb += texCUBElod(_Background, float4(dir, _BackgroundBlur)).rgb * _Emission * pow(rim,_EmissionFresnel) * _EmissionColor;

				#ifndef UNITY_HDR_ON
				deferredStruct.emission.rgb = exp2(-deferredStruct.emission.rgb);
				#endif

				return deferredStruct;
			}

			ENDCG
		}
	}
	Fallback "Diffuse"

}
