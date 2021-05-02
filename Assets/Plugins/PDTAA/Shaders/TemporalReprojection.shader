// Copyright (c) <2015> <Playdead>
// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE.TXT)
// AUTHOR: Lasse Jon Fuglsang Pedersen <lasse@playdead.com>

Shader "Playdead/Post/TemporalReprojection"
{
	Properties
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}

	CGINCLUDE
	//--- program begin
	
	#pragma only_renderers ps4 xboxone d3d11 d3d9 xbox360 opengl glcore gles3 metal vulkan
	#pragma target 3.0

	#pragma multi_compile CAMERA_PERSPECTIVE CAMERA_ORTHOGRAPHIC
	//#pragma multi_compile __ UNJITTER_COLORSAMPLES
	//#pragma multi_compile __ UNJITTER_NEIGHBORHOOD
	//#pragma multi_compile __ UNJITTER_REPROJECTION
	#pragma multi_compile __ USE_AABB_ROUNDING
	#pragma multi_compile __ USE_CHROMA_COLORSPACE
	#pragma multi_compile __ USE_CLIPPING
	#pragma multi_compile __ USE_VARIANCE_CLIPPING
	#pragma multi_compile __ USE_CENTER_CLIPPING
	#pragma multi_compile __ USE_DILATION_5X
	#pragma multi_compile __ USE_DILATION_3X3
	#pragma multi_compile __ USE_HIGHER_ORDER_TEXTURE_FILTERING_COLOR
	#pragma multi_compile __ USE_HIGHER_ORDER_TEXTURE_FILTERING_HISTORY
	#pragma multi_compile __ USE_MOTION_BLUR
	#pragma multi_compile __ USE_MOTION_BLUR_NEIGHBORMAX

	#define UNJITTER_COLORSAMPLES 1
	#define UNJITTER_NEIGHBORHOOD 0
	#define UNJITTER_REPROJECTION 0

	//TODO: presets
	/*
	#if USE_HIGH_PERFORMANCE
	// best perf: 5 tap nearest, RGB, minmaxrounded, clamping, green_is_luminance, low-motionblur-samples
	#define USE_DILATION_5X 1
	#define USE_CENTER_CLIPPING 1
	#define APPROXIMATE_LUMINANCE_AS_GREEN 1
	#endif

	#if USE_HIGH_QUALITY
	// best qual: 9 tap nearest, YCbCg, minmax, full clip, full lum, high-motionblur-samples
	#define USE_DILATION_3X3 1
	#define USE_CLIPPING 1
	#define USE_CHROMA_COLORSPACE 1
	#define USE_HIGHER_ORDER_TEXTURE_FILTERING_COLOR 1
	#define USE_HIGHER_ORDER_TEXTURE_FILTERING_HISTORY 1
	#endif
	*/

	#include "UnityCG.cginc"
	#include "IncDepth.cginc"
	#include "IncNoise.cginc"
	#include "IncColor.cginc"

#if SHADER_API_MOBILE
	static const float FLT_EPS = 0.0001f;
#else
	static const float FLT_EPS = 0.00000001f;
#endif

	uniform float4 _JitterUV;// frustum jitter uv deltas, where xy = current frame, zw = previous

	uniform sampler2D _MainTex;
	uniform float4 _MainTex_TexelSize;

	uniform sampler2D_half _VelocityBuffer;
	uniform sampler2D _VelocityNeighborMax;
	uniform float4 _VelocityNeighborMax_TexelSize;

	uniform sampler2D _PrevTex;
	uniform float4 _PrevTex_TexelSize;

	uniform sampler2D _DitherTex;
	uniform float4 _DitherTex_TexelSize;
	uniform float4 _DitherOffset_local;

	uniform float _FeedbackMin;
	uniform float _FeedbackMinMax;
	uniform float _MotionScale;

	struct v2f
	{
		float4 cs_pos : SV_POSITION;
		float2 ss_txc : TEXCOORD0;
		nointerpolation float4 mad : TEXCOORD1;
	};

	v2f vert(appdata_img IN)
	{
		v2f OUT;

	#if UNITY_VERSION < 540
		OUT.cs_pos = UnityObjectToClipPos(IN.vertex);
	#else
		OUT.cs_pos = UnityObjectToClipPos(IN.vertex);
	#endif
	#if UNITY_SINGLE_PASS_STEREO
		OUT.ss_txc = UnityStereoTransformScreenSpaceTex(IN.texcoord.xy);
	#else
		OUT.ss_txc = IN.texcoord.xy;
	#endif

		OUT.mad.xy = _PrevTex_TexelSize.zw * _DitherTex_TexelSize.xy;
		OUT.mad.zw = _DitherOffset_local.xy * _DitherTex_TexelSize.xy;

		return OUT;
	}

	float4 to_working_colorspace( float4 c )
	{
		#if USE_CHROMA_COLORSPACE
			return float4( RGB_YCbCr(c.rgb), c.a );
			//return float4( RGB_YCoCg(c.rgb), c.a );
		#else
			return c;
		#endif
	}
	float4 from_working_colorspace( float4 c )
	{
		#if USE_CHROMA_COLORSPACE
			return float4( YCbCr_RGB(c.rgb), c.a );
			//return float4( YCoCg_RGB(c.rgb), c.a );
		#else
			return c;
		#endif
	}

	float4 clip_aabb(float3 aabb_min, float3 aabb_max, float4 p, float4 q)
	{
	#if USE_CENTER_CLIPPING
		// note: only clips towards aabb center (but fast!)
		float3 p_clip = 0.5 * (aabb_max + aabb_min);
		float3 e_clip = 0.5 * (aabb_max - aabb_min) + FLT_EPS;

		float4 v_clip = q - float4(p_clip, p.w);
		float3 v_unit = v_clip.xyz / e_clip;
		float3 a_unit = abs(v_unit);
		float ma_unit = max(a_unit.x, max(a_unit.y, a_unit.z));

		if (ma_unit > 1.0)
			return float4(p_clip, p.w) + v_clip / ma_unit;
		else
			return q;// point inside aabb
	#else
		float4 r = q - p;
		float3 rmax = aabb_max - p.xyz;
		float3 rmin = aabb_min - p.xyz;

		const float eps = FLT_EPS;
		if (r.x > rmax.x + eps) r *= (rmax.x / r.x);
		if (r.y > rmax.y + eps) r *= (rmax.y / r.y);
		if (r.z > rmax.z + eps) r *= (rmax.z / r.z);

		if (r.x < rmin.x - eps) r *= (rmin.x / r.x);
		if (r.y < rmin.y - eps) r *= (rmin.y / r.y);
		if (r.z < rmin.z - eps) r *= (rmin.z / r.z);

		return p + r;
	#endif
	}

	//TODO: option to toggle symmetric sampling
	static const int NUM_TAPS = 3;// on either side!
	static const float RCP_NUM_TAPS_F = 1.0f / float(NUM_TAPS);
	static const float RCP_NUM_TOTAL_TAPS_F = 1.0f / float(2*NUM_TAPS+1);
	half4 sample_color_motion( float2 uv, float2 ss_vel, float srand )
	{
		const float2 v = 0.5 * ss_vel;
		float2 vtap = v * RCP_NUM_TAPS_F;
		float2 pos0 = uv + vtap * srand;
		half4 accu = 0.0h;

		[unroll]
		for (int i = -NUM_TAPS; i <= NUM_TAPS; i++)
		{
			accu += to_working_colorspace( tex2D(_MainTex, pos0 + i * vtap) );
		}

		return accu * RCP_NUM_TOTAL_TAPS_F;
	}

	float lengthsq( float2 v )
	{
		return dot(v,v);
	}

	float4 temporal_reprojection(float2 ss_txc, float2 ss_vel, float vs_dist)
	{
		// read texels
		#if UNJITTER_COLORSAMPLES
		float2 cuv = ss_txc - _JitterUV.xy;
		#else
		float2 cuv = ss_txc;
		#endif

		// read texels
		#if USE_HIGHER_ORDER_TEXTURE_FILTERING_COLOR
		half4 texel0 = sample_cubic(_MainTex, cuv, _MainTex_TexelSize.zw); //softer
		#else
		half4 texel0 = tex2D(_MainTex, cuv); //note: sharper, faster
		#endif
		texel0 = to_working_colorspace( texel0 );

		#if USE_HIGHER_ORDER_TEXTURE_FILTERING_HISTORY
		half4 texel1 = sample_catmull_rom(_PrevTex, ss_txc - ss_vel, _PrevTex_TexelSize.zw);
		#else
		half4 texel1 = tex2D(_PrevTex, ss_txc - ss_vel);
		#endif
		texel1 = to_working_colorspace( texel1 );

		// calc min-max of current neighbourhood
	#if UNJITTER_NEIGHBORHOOD
		float2 uv = ss_txc - _JitterUV.xy;
	#else
		float2 uv = ss_txc;
	#endif

		float2 du = float2(_MainTex_TexelSize.x, 0.0);
		float2 dv = float2(0.0, _MainTex_TexelSize.y);
		float4 ctl = to_working_colorspace( tex2D(_MainTex, uv - dv - du) );
		float4 ctc = to_working_colorspace( tex2D(_MainTex, uv - dv) );
		float4 ctr = to_working_colorspace( tex2D(_MainTex, uv - dv + du) );
		float4 cml = to_working_colorspace( tex2D(_MainTex, uv - du) );
		float4 cmc = to_working_colorspace( tex2D(_MainTex, uv) );
		float4 cmr = to_working_colorspace( tex2D(_MainTex, uv + du) );
		float4 cbl = to_working_colorspace( tex2D(_MainTex, uv + dv - du) );
		float4 cbc = to_working_colorspace( tex2D(_MainTex, uv + dv) );
		float4 cbr = to_working_colorspace( tex2D(_MainTex, uv + dv + du) );

		float4 cmin5 = min(ctc, min(cml, min(cmc, min(cmr, cbc))));
		float4 cmax5 = max(ctc, max(cml, max(cmc, max(cmr, cbc))));
		float4 csum5 = ctc + cml + cmc + cmr + cbc;
		
		float4 cmin = min(cmin5, min(ctl, min(ctr, min(cbl, cbr))));
		float4 cmax = max(cmax5, max(ctl, max(ctr, max(cbl, cbr))));

	#if USE_CHROMA_COLORSPACE || USE_CLIPPING
		float4 cavg = (csum5 + ctl + ctr + cbl + cbr) / 9.0;
	#endif
	
	#if USE_AABB_ROUNDING
		cmin = 0.5 * (cmin + cmin5);
		cmax = 0.5 * (cmax + cmax5);
		#if USE_CLIPPING
		cavg = 0.5 * (cavg + 0.2 * csum5);
		#endif
	#endif

	#if USE_VARIANCE_CLIPPING
	//note: salvi variance-clipping
	{
		const float3 w = float3(1.0/16.0, 2.0/16.0, 4.0/16.0);

		float4 colorAvg = w.z*cmc;
		float4 colorVar = w.z*cmc*cmc;
		colorAvg += w.x * ctl; colorVar += w.x * ctl * ctl;
		colorAvg += w.y * ctc; colorVar += w.y * ctc * ctc;
		colorAvg += w.x * ctr; colorVar += w.x * ctr * ctr;
		colorAvg += w.y * cml; colorVar += w.y * cml * cml;
		//colorAvg += w.z * cmc; colorVar += w.z * cmc * cmc;
		colorAvg += w.y * cmr; colorVar += w.y * cmr * cmr;
		colorAvg += w.x * cbl; colorVar += w.x * cbl * cbl;
		colorAvg += w.y * cbc; colorVar += w.y * cbc * cbc;
		colorAvg += w.x * cbr; colorVar += w.x * cbr * cbr;

		float4 dev = sqrt(max(FLT_EPS, colorVar - colorAvg*colorAvg));
		const float gColorBoxSigma = 1.0; //TODO: expose [0.75;1.25]
		cmin = max(cmin, colorAvg - gColorBoxSigma * dev );
		cmax = min(cmax, colorAvg + gColorBoxSigma * dev );
		#if USE_CLIPPING
		cavg = colorAvg;
		#endif
	}
	#endif //USE_VARIANCE_CLIPPING

		//note: shrink chroma min-max
		#if USE_CHROMA_COLORSPACE
		float2 chroma_extent = 0.25 * 0.5 * (cmax.xx - cmin.xx);
		float2 chroma_center = texel0.yz;
		float2 ccmin = chroma_center - chroma_extent;
		float2 ccmax = chroma_center + chroma_extent;
		if ( lengthsq(ccmax-ccmin) > lengthsq(cmax.yz-cmin.yz) )
		{
			cmin.yz = ccmin;
			cmax.yz = ccmax;
			#if USE_CLIPPING
			cavg.yz = chroma_center;
			#endif
		}
		#endif //USE_CHROMA_COLORSPACE

		// clamp to neighbourhood of current sample
		#if USE_CLIPPING
		texel1 = clip_aabb(cmin.xyz, cmax.xyz, clamp(cavg, cmin, cmax), texel1); 
		#else
		texel1 = clamp(texel1, cmin, cmax);
		#endif


		// feedback weight from unbiased luminance diff (t.lottes)
	#if USE_CHROMA_COLORSPACE
		float lum0 = texel0.r;
		float lum1 = texel1.r;
	#else
		#if APPROXIMATE_LUMINANCE_AS_GREEN
		float lum0 = texel0.g;
		float lum1 = texel1.g;
		#else
		float lum0 = Luminance(texel0.rgb);
		float lum1 = Luminance(texel1.rgb);
		#endif
	#endif
		float unbiased_diff = abs(lum0 - lum1) / max(lum0, max(lum1, 0.2));
		float unbiased_weight = 1.0 - unbiased_diff;
		float unbiased_weight_sqr = unbiased_weight * unbiased_weight;
		float k_feedback = _FeedbackMin + unbiased_weight_sqr * _FeedbackMinMax;
		
		// output
		return lerp(texel0, texel1, k_feedback);
	}

	struct f2rt
	{
		fixed4 buffer : SV_Target0;
		fixed4 screen : SV_Target1;
	};

	f2rt frag(v2f IN)
	{
		f2rt OUT;

	#if UNJITTER_REPROJECTION
		float2 uv = IN.ss_txc - _JitterUV.xy;
	#else
		float2 uv = IN.ss_txc;
	#endif

		//note: RPDF blue-noise
		half4 rnd = tex2Dlod( _DitherTex, float4( IN.ss_txc * IN.mad.xy + IN.mad.zw, 0, 0) );

	#if USE_DILATION_5X
		//--- 5 tap nearest (decent)
		float3 c_frag = find_closest_fragment_5tap(uv);
		float2 ss_vel = tex2D(_VelocityBuffer, c_frag.xy).xy;
		float vs_dist = depth_resolve_linear(c_frag.z);
	#elif USE_DILATION_3X3
		//--- 3x3 nearest (good)
		float3 c_frag = find_closest_fragment_3x3(uv);
		float2 ss_vel = tex2D(_VelocityBuffer, c_frag.xy).xy;
		float vs_dist = depth_resolve_linear(c_frag.z);
	#else
		float2 ss_vel = tex2D(_VelocityBuffer, uv).xy;
		float vs_dist = depth_sample_linear(uv);
	#endif

		// temporal resolve
		float4 color_temporal = temporal_reprojection(IN.ss_txc, ss_vel, vs_dist);

		// prepare outputs
		float4 to_buffer = color_temporal;

	#if USE_MOTION_BLUR
		#if USE_MOTION_BLUR_NEIGHBORMAX
			//ss_vel = _MotionScale * tex2D(_VelocityNeighborMax, IN.ss_txc).xy;
			ss_vel = _MotionScale * sample_cubic(_VelocityNeighborMax, IN.ss_txc, _VelocityNeighborMax_TexelSize.zw).xy;
		#else
			ss_vel = _MotionScale * ss_vel;
		#endif

		float vel_mag = length(ss_vel * _MainTex_TexelSize.zw);
		const float vel_trust_full = 2.0;
		const float vel_trust_none = 15.0;
		const float vel_trust_span = vel_trust_none - vel_trust_full;
		float trust = 1.0 - clamp(vel_mag - vel_trust_full, 0.0, vel_trust_span) / vel_trust_span;

		#if UNJITTER_COLORSAMPLES
			half4 color_motion = sample_color_motion( IN.ss_txc - _JitterUV.xy, ss_vel, rnd.xy-0.5h);
		#else
			half4 color_motion = sample_color_motion( IN.ss_txc, ss_vel, rnd.xy-0.5h);
		#endif

		float4 to_screen = lerp(color_motion, color_temporal, trust);
	#else
		float4 to_screen = color_temporal;
	#endif

		to_buffer = from_working_colorspace( to_buffer );
		to_screen = from_working_colorspace( to_screen );

		//note: velocity debug
		//to_screen.g += 100.0 * length(ss_vel);
		//to_screen = float4(100.0 * abs(ss_vel), 0.0, 0.0);

		// dither color-output
		float4 noise4_tri = remap_noise_tri( rnd ) / 255.0;
		OUT.buffer = saturate(to_buffer + noise4_tri);
		OUT.screen = saturate(to_screen + noise4_tri);

		return OUT;
	}

	//--- program end
	ENDCG

	SubShader
	{
		ZTest Always
		Cull Off
		ZWrite Off
		Fog { Mode off }

		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			ENDCG
		}
	}

	Fallback off
}
