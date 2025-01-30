// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Aetheria/Field"
{
	Properties
	{
		_Displacement("Max Displacement", Range(0, 1.0)) = 0.3
		_SubdivisionAmount("Subivision Amount", Range(1, 12)) = 1
		_Color("Color", Color) = (1,1,1,1)
		
		_Emission ("Emission Strength", Float) = 0
        _EmissionFresnel ("Emission Fresnel", Float) = 1.0
		
		_Background ("Background", CUBE) = "" {}
        _BackgroundFresnel ("Background Fresnel", Float) = 1.0
		_BackgroundBlur ("Background Blur", Float) = 2
		_BlurFresnel ("Blur Fresnel", Float) = 2
		
		_FresnelOpacity ("Fresnel Opacity", Range(0,1)) = 1
		_OpacityFresnel ("Opacity Fresnel", Float) = 1
		_DisplacementOpacity ("Displacement Opacity", Float) = 1
		_DeviationOpacityExponent ("Deviation Opacity Exponent", Float) = 8
		
		_WaveMagnitude ("Wave Magnitude", Float) = .5
		_WaveFrequency ("Wave Frequency", Float) = 2
		_WaveOffset ("Wave Time", Float) = .5
		_WaveExponent ("Wave Exponent", Float) = 2
		
		_Push ("Push", Range(0,1)) = 1
		_PushDirection ("Push Direction", Vector) = (0,0,1)
		_PushVerticalExponent ("Push Vertical Exponent", Float) = 1
		_PushEnvelopeExponent ("Push Envelope Exponent", Float) = 2
		_PushFrequencyExponent ("Push Frequency Exponent", Float) = 2
//		_PushOpacity ("Push Opacity", Range(0,1)) = 1
//		_PushOpacityRangeMin ("Push Opacity Range Start", Range(0,1)) = .5
//		_PushOpacityRangeMax ("Push Opacity Range End", Range(0,1)) = .25
		
		_TwistFront ("Twist Front", Range(-1,1)) = 1
		_TwistRear ("Twist Back", Range(-1,1)) = 1
		_TwistVerticalExponent ("Twist Vertical Exponent", Float) = 1
		_TwistEnvelopeExponent ("Twist Envelope Exponent", Range(1,12)) = 1
		_TwistFrequencyExponent ("Twist Frequency Exponent", Float) = 2
		
		_MeleeDisplacement ("Melee Displacement", Float) = 8
		_MeleeShape ("Melee Shape", Float) = 8
		_MeleeFlattening ("Melee Flattening", Float) = 8
		_MeleeDirection ("Melee Direction", Vector) = (0,0,-1)
		
		_TendrilSize ("Tendril Size", Float) = 1
		_TendrilExponent ("Tendril Exponent", Float) = 1
		_TendrilRadius ("Tendril Radius", Float) = .1
		_TendrilInfluence ("Tendril Influence", Float) = .1
		
		_InverseScale ("Inverse Scale", Vector) = (1,1,1)
		
		_CellSpeed("Cell Speed", Float) = 1
		_CellTiling("Cell Tiling", Float) = 1
		_CellExponent("Cell Exponent", Float) = 1
//		_BulbRadius ("Bulb Radius", Float) = .5
//		_BulbRange ("Bulf Range", Float) = .1
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
			#pragma multi_compile A B C D E F G H
			//#pragma exclude_renderers nomrt
			#pragma target 4.6
			#pragma glsl

			//#define UNITY_PASS_DEFERRED
			#include "UnityPBSLighting.cginc"
			#include "Dither Functions.cginc"

			//#pragma only_renderers d3d11

			float _SubdivisionAmount;
			float _Displacement;

			half4 _Color;
			
			samplerCUBE _Background;
			float _Emission;
			float _EmissionFresnel;
			float _BackgroundBlur;
			float _BlurFresnel;
			float _BackgroundFresnel;
			float _FresnelOpacity;
			float _OpacityFresnel;
			float _DeviationOpacityExponent;
			float _DisplacementOpacity;

			float _WaveMagnitude;
			float _WaveOffset;
			float _WaveFrequency;
			float _WaveExponent;
			
			float _Push;
			float3 _PushDirection;
			float _PushVerticalExponent;
			float _PushEnvelopeExponent;
			float _PushFrequencyExponent;
			// float _PushOpacity;
			// float _PushOpacityRangeMin;
			// float _PushOpacityRangeMax;

			float _TwistVerticalExponent;
			float _TwistEnvelopeExponent;
			float _TwistFrequencyExponent;
			float _TwistFront;
			float _TwistRear;

			float _MeleeDisplacement;
			float _MeleeShape;
			float _MeleeFlattening;
			float3 _MeleeDirection;

			float3 _TendrilBase;
			float3 _TendrilBend;
			float3 _TendrilTarget;
			float _TendrilSize;
			float _TendrilRadius;
			float _TendrilExponent;
			float _TendrilInfluence;
			// float _BulbRadius;
			// float _BulbRange;
			
			float _CellSpeed;
			float _CellExponent;
			float _CellTiling;

			int _HitCount;
		    struct FieldHit
		    {
		        float3 Position;
		        float3 Direction;
		        float Magnitude;
		        float Time;
		    };
            StructuredBuffer<FieldHit> _Hits;
			
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

			inline float3 spline(float3 p0, float3 p1, float3 p2, float t)
		    {
		        t = saturate(t);
		        float oneMinusT = 1 - t;
		        return
		            oneMinusT * oneMinusT * p0 +
		            2 * oneMinusT * t * p1 +
		            t * t * p2;
		    }

			inline float bulb(float x)
			{
				return sqrt(0.25-pow(x-.5, 2));
			}

			inline float unlerp(float from, float to, float value){
				return saturate((value - from) / (to - from));
			}
			
			inline float almostUnitIdentity( float x )
			{
			    return x*x*x*(2.0-x);
			}
			
			struct v2f {
			    float4 vertex : POSITION;
			    float4 tangent : TANGENT;
			    float3 normal : NORMAL;
				float4 screenPos : TEXCOORD1;
				float3 viewDirection : TEXCOORD2;
			};

			static const float EPSILON = .001;
			
			float3 SampleDisplacement (float3 normal)
			{
				float3 disp = 0;

				float vertical = acos(dot(normal,float3(0,1,0)))/UNITY_PI;
				float angle = atan2(normal.z,normal.x) / UNITY_PI;
				
				if(_Push > EPSILON)
				{
					float waveRange = acos(dot(_PushDirection,normal)) / UNITY_PI;
					float pushwave = pow((sin(pow(waveRange, _PushFrequencyExponent)*_WaveFrequency-_WaveOffset)+1)/2, _WaveExponent);
					if(_WaveMagnitude<0) pushwave = 1-pushwave;
					//if(_CellExponent > 0) pushwave *= pow(cells(float2(waveRange * _CellTiling, vertical * _CellTiling)), _CellExponent);
					float verticalEnvelope = pow( 4.0*vertical*(1.0-vertical), abs(_PushVerticalExponent) );
					if(_PushVerticalExponent < 0) verticalEnvelope = 1-verticalEnvelope;
					disp += normal * (
						pow(waveRange, _PushEnvelopeExponent) *
						verticalEnvelope *
						pushwave *
						abs(_WaveMagnitude) *
						_Push);
				}

				float twist = angle < 0 ? _TwistFront : _TwistRear;
				if(abs(twist) > EPSILON)
				{
					angle = frac(angle);
					if(twist < 0) angle = 1 - angle;
					float verticalEnvelope = pow( 4.0*vertical*(1.0-vertical), _TwistVerticalExponent );
					float twistEnvelope = (1-angle) *
						pow(angle, _TwistEnvelopeExponent) *
						pow(1/_TwistEnvelopeExponent+1, _TwistEnvelopeExponent) *
						(_TwistEnvelopeExponent+1);
					float twistwave = pow((sin(pow(angle, _TwistFrequencyExponent)*_WaveFrequency - _WaveOffset)+1)/2, _WaveExponent);
					if(_WaveMagnitude<0) twistwave = 1-twistwave;
					disp += normal * (
						verticalEnvelope *
						twistEnvelope *
						twistwave *
						abs(_WaveMagnitude) *
						abs(twist));
				}
				
				float3 scaledNormal = normal/_InverseScale;
				for(int i=0; i<_HitCount; i++)
				{
					FieldHit hit = _Hits[i];
					float3 toHit = hit.Position-scaledNormal;
					float envelope = pow(smoothstep(hit.Magnitude + 4, 0, length(toHit)), 8 / hit.Time);
					if(envelope < EPSILON) continue;
					float hitroot = sqrt(hit.Magnitude);
					float invsqtime = pow(1-hit.Time, 2);
					disp += lerp(normal, hit.Direction, invsqtime) * (envelope * pow(cos(envelope*hitroot*hit.Time*6)+1,2) * hitroot * invsqtime * _InverseScale.y);
				}
				
				if(_MeleeDisplacement > EPSILON)
				{
					normal.y *= _MeleeFlattening;
					normal = normalize(normal);
					disp += _MeleeDirection * (pow(saturate(1 - acos(dot(normal,_MeleeDirection))/(UNITY_PI/2)),_MeleeShape) * _MeleeDisplacement);
				}

				if(_TendrilInfluence > EPSILON)
				{
					float3 toBase = _TendrilBase-scaledNormal;
					float3 toBend = _TendrilBend-scaledNormal;
					float3 toTarget = _TendrilTarget-scaledNormal;
					float tendrilLerp = 1 - saturate(length(toBase)/_TendrilSize);
					float3 radial = normalize(-toBase) * _TendrilRadius;
					// if(_BulbRadius > 0)
					// {
					// 	float bulbLerp = smoothstep(1-_BulbRange * _BulbRadius, 1, tendrilLerp);
					// 	radial = normalize(-toBase) * (_TendrilRadius + bulb(bulbLerp) * _BulbRadius);
					// 	toTarget += normalize(toTarget) * _BulbRadius*.25;
					// 	toTarget += normalize(toTarget-toBend) * _BulbRadius*.25;
					// }
					float3 tendril = spline(toBase, toBend, toTarget, tendrilLerp) + radial;
					disp = lerp(disp, tendril, almostUnitIdentity(tendrilLerp) * _TendrilInfluence);
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
		        ditherClip(screenUV, abs(height) * _DisplacementOpacity + deviation + pow(1-rim, _OpacityFresnel) * _FresnelOpacity);
				deferredStruct.emission.rgb += texCUBElod(_Background, float4(dir, _BackgroundBlur*pow(rim, _BlurFresnel))).rgb * _Emission * pow(rim,_EmissionFresnel) * _Color;

				#ifndef UNITY_HDR_ON
				deferredStruct.emission.rgb = exp2(-deferredStruct.emission.rgb);
				#endif

				return deferredStruct;
			}

			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "CellBrushEditor"

}
