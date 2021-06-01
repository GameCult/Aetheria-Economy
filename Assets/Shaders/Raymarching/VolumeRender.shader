Shader "VolSample/Volume Render" {
	Properties {
		_MainTex( "", 2D ) = "white" {}
		_StepExponent("Step Exponent", float) = 1
		_Surface("Surface", 2D) = "black" {}
		_Patch("Patch", 2D) = "black" {}
		_Displacement("Displacement", 2D) = "black" {}
		_TintTexture("Tint Texture", 2D ) = "white" {}
		[HDR]_Tint("Tint Color", Color) = (1,1,1,1)
		//_GridTransform("Grid Transform", Vector) = (0,0,0,0)
		_GridFillDensity("Fill Density", float) = 1
        _GridFloorDensity("Floor Density", float) = 1
        _GridPatchDensity("Patch Density", float) = 1
        _GridFloorOffset("Floor Offset", float) = 1
        _GridFloorBlend("Floor Blend", float) = 1
        _GridPatchBlend("Patch Blend", float) = 1
		_Gamma("Gamma", float) = .5
		_TintExponent("Tint Exponent", float) = .5
		_DepthCeiling("Depth Ceiling", float) = 1000
		_DepthBlend("Depth Blend", float) = 100
		_NoiseStrength("Noise Strength", float) = 1
		_NoiseFrequency("Noise Frequency", float) = 1
		_NoiseExponent("Noise Exponent", float) = 2
		_NoiseSpeed("Noise Speed", float) = 1
		_TurbulenceScale("Turbulence Scale", float) = 1
		_TurbulenceAmplitude("Turbulence Amplitude", float) = 1
		_FlowSpeed("Flow Speed", float) = 1
		_SafetyDistance("Safety Distance", float) = 20
		_Scattering("Scattering", float) = 1
		_ScatteringMinDist("ScatteringMinDist", float) = 1
		_ScatteringDistExponent("ScatteringDistExponent", float) = 1
		_ScatteringDensityExponent("ScatteringDensityExponent", float) = 1
	}
	
	CGINCLUDE;

	uniform sampler2D _MainTex;
	uniform sampler2D _CameraDepthTexture;

	#include "UnityCG.cginc"
	#include "Assets/Shaders/Volumetric.cginc"
	
	// the number of volume samples to take
	#define SAMPLE_COUNT 96

	// Shared shader code for pixel view rays, given screen pos and camera frame vectors.
	// Camera vectors are passed in as this shader is run from a post proc camera, so the unity built-in values are not useful.
	uniform float4x4 _CamProj;
	uniform float4x4 _CamInvProj;
	uniform float3 _CamPos;
	uniform float3 _CamForward;
	uniform float3 _CamRight;
	uniform float  _HalfFov;
	uniform int _FrameNumber;
	
	uniform float  _StepExponent;
	
    uniform half _Gamma,
				_DepthCeiling,
				_DepthBlend,
				_Scattering,
				_ScatteringDensityExponent,
				_ScatteringDistExponent,
				_ScatteringMinDist;
	
	// Dithering
	sampler2D _DitheringTex;
	float4 _DitheringCoords;
	
	inline float nrand(float2 ScreenUVs)
	{
		return frac(sin(ScreenUVs.x * 12.9898 + ScreenUVs.y * 78.233) * 43758.5453);
	}
	
	void RaymarchStep( in float3 pos, in float stepSize, in float weight, inout float4 sum, in float scatter, inout float scatterSum)
	{
		float4 col = VolumeSampleColor( pos );
		col.a *= stepSize * weight;
		sum.rgb += col.rgb * col.a * (1.0 - sum.a);
		sum.a += col.a;
		scatterSum += pow(col.a, _ScatteringDensityExponent) * scatter;
	}
	
	float4 RayMarch( in float3 origin, in float3 direction, in float zbuf, in float2 screenUV, out float scatterSum )
	{
		scatterSum = 0;
		float blue = tex2D(_DitheringTex, screenUV * _DitheringCoords.xy + _DitheringCoords.zw).r;
        //float rand = frac(blue + _FrameNumber * 1.61803398875);
		//return float4(rand, rand, rand, 1);
        //half rand = frac(nrand(screenUV) + _FrameNumber * 1.61803398875);
		//half rand = nrand(screenUV + frac(_Time.x));
		half rand = blue;
		float4 sum = (float4)0.;
		scatterSum = 0.;

		// setup sampling

		float rayDist = 0;
		for( int i = 0; i < SAMPLE_COUNT; i++ )
		{
			float prevRayDist = rayDist;
			rayDist = pow((i+rand)/SAMPLE_COUNT,_StepExponent) * _DepthCeiling + 1;
			float distToSurf = zbuf - rayDist;
			if( distToSurf <= 0.001 || sum.a > .99) break;

			float step = rayDist - prevRayDist;
			//float wt = (distToSurf >= step) ? 1. : distToSurf / step;

			RaymarchStep( origin + rayDist * direction, step, 1-rayDist/_DepthCeiling, sum, _Scattering/pow(max(rayDist,_ScatteringMinDist),_ScatteringDistExponent), scatterSum);
		}

		sum.rgb = pow( sum.rgb, 1 / _Gamma );
		return sum;
	}

	struct v2f
	{
		float4 pos : SV_POSITION;
		float4 screenPos : TEXCOORD1;
	};

	v2f vert( appdata_base v )
	{
		v2f o;
		o.pos = UnityObjectToClipPos( v.vertex );
		o.screenPos = ComputeScreenPos( o.pos );
		return o;
	}

	void computeCamera( in float2 q, out float3 rayOrigin, out float3 rd )
	{
		rayOrigin = _WorldSpaceCameraPos;
		
		float2 p = 2.0*(q - 0.5);
		float4 d = mul(_CamProj, float4(p, 0, 1));
		d.xyz /= d.w;
		//rd = (d - rayOrigin);
		rd = normalize(d - rayOrigin);
		rayOrigin = d;
		//rayOrigin += _ProjectionParams.y * rd;
	}
	
	inline float LinearEyeDepthToOutDepth(float z)
	{
	    return (1 - _ZBufferParams.w * z) / (_ZBufferParams.z * z);
	}
	
	float3 DepthToWorld(float2 uv, float depth) {
		float z = (1-depth) * 2.0 - 1.0;

		float4 clipSpacePosition = float4(uv * 2.0 - 1.0, z, 1.0);

		float4 viewSpacePosition = mul(_CamInvProj,clipSpacePosition);
		viewSpacePosition /= viewSpacePosition.w;

		float4 worldSpacePosition = mul(unity_ObjectToWorld,viewSpacePosition);

		return worldSpacePosition.xyz;
	}

    // MRT shader
    struct FragmentOutput
    {
        half4 dest0 : COLOR0;
        half4 dest1 : COLOR1;
    };
	
	FragmentOutput frag( v2f i ) : SV_Target //out float outDepth : SV_Depth
	{
        FragmentOutput o;
		
		float2 q = i.screenPos.xy / i.screenPos.w;

		// camera
		float3 rayOrigin, rayDirection;
		computeCamera( q, rayOrigin, rayDirection );

		// z buffer / scene depth for this pixel
		float4 screenPos = UNITY_PROJ_COORD( i.screenPos );
		float2 screenUV = screenPos.xy / screenPos.w;
		
		float depthSample = tex2Dproj( _CameraDepthTexture, screenPos ).r;
		//float depthValue = LinearEyeDepth(depthSample);

		float3 worldDepth = DepthToWorld(screenPos, depthSample);
		float depthValue = length(worldDepth-rayOrigin);
		
		// march through volume
		float scatter;
		float4 clouds = RayMarch( rayOrigin, rayDirection, depthValue, screenUV, scatter );
		o.dest1 = .5 + scatter*.5;//smoothstep(1,.99,clouds.a)*pow(clouds.a,1)*.5;

		float3 bgcol = tex2Dlod( _MainTex, float4(q, 0., 0.) );
		// add in camera render colours, if not zfar (so we exclude skybox)
		if(clouds.a < .99 && depthValue < _DepthCeiling)
		{
			// Blend out camera render when outside marching range
			if( depthValue >= _DepthCeiling - _DepthBlend )
				clouds.a = lerp(clouds.a, 1, (depthValue - (_DepthCeiling - _DepthBlend)) / _DepthBlend);
			
			//clouds.xyz = lerp(clouds.xyz, bgcol, 1-clouds.a);
			clouds.xyz += (1. - clouds.a) * bgcol;
			// assume zbuffer represents opaque surface
			
		}
		clouds.a = 1.;

		//outDepth = LinearEyeDepthToOutDepth(min(depthValue, rayDist));

		o.dest0 = clouds;
		return o;
	}
	

	ENDCG

	Subshader
	{
		Pass
		{
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
			#pragma target 3.0   
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
	}

	Fallback off

} // shader
