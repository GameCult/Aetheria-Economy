Shader "Aetheria/Grid" {

	Properties {
		_Color ("Tint", Color) = (1, 1, 1, 1)
		_MainTex ("Albedo", 2D) = "white" {}

		[Gamma] _Metallic ("Metallic", Range(0, 1)) = 0
		_Smoothness ("Smoothness", Range(0, 1)) = 0.1

		_TessellationUniform ("Tessellation Uniform", Range(1, 64)) = 1
		_TessellationEdgeLength ("Tessellation Edge Length", Range(5, 100)) = 50
		
        [NoScaleOffset] _DisplacementMap ("Displacement Texture (R)", 2D) = "gray" {}
        _DisplacementStrength ("Displacement", Float) = 1.0
        _DisplacementStep ("Displacement Step", Float) = 1.0
		
		_EmissionColor ("Emission Color", Color) = (1,1,1,1)
		_Emission ("Emission Strength", Float) = 0
        _EmissionFresnel ("Emission Fresnel", Float) = 1.0
        
		_FresnelClip("Fresnel Clip", Float) = 0.5
		_FresnelClipOffset("Fresnel Clip Offset", Float) = 0.5
		_FresnelClipPower("Fresnel Clip Power", Float) = 2
		
		_AlphaMask ("Alpha Mask (R)", 2D) = "white" {}
		_AlphaClip("Clip Alpha", Float) = 0.5
		_AlphaTiling("Alpha Texture Tiling", Float) = 5
		_AlphaRange("Alpha Range (World Y)", Float) = 50
		_AlphaRangeFeather("Alpha Range Feather", Float) = 10
		_AlphaRangeCenter("Alpha Range Center", Float) = -60
		
		_Background ("Background", CUBE) = "" {}
        _BackgroundFresnel ("Background Fresnel", Float) = 1.0
		_BackgroundBlur ("Background Blur", Float) = 2

		[HideInInspector] _SrcBlend ("_SrcBlend", Float) = 1
		[HideInInspector] _DstBlend ("_DstBlend", Float) = 0
		[HideInInspector] _ZWrite ("_ZWrite", Float) = 1
	}

	CGINCLUDE

	#define BINORMAL_PER_FRAGMENT
	#define FOG_DISTANCE

	#define VERTEX_DISPLACEMENT_INSTEAD_OF_PARALLAX

	ENDCG

	SubShader {

		Pass {
			Tags {
				"LightMode" = "ForwardBase"
			}
			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]
		    Cull Off

			CGPROGRAM

			#pragma target 4.6

			#pragma shader_feature _ _RENDERING_CUTOUT _RENDERING_FADE _RENDERING_TRANSPARENT
			#pragma shader_feature _METALLIC_MAP
			#pragma shader_feature _ _SMOOTHNESS_ALBEDO _SMOOTHNESS_METALLIC
			#pragma shader_feature _NORMAL_MAP
			#pragma shader_feature _PARALLAX_MAP
			#pragma shader_feature _OCCLUSION_MAP
			#pragma shader_feature _EMISSION_MAP
			#pragma shader_feature _DETAIL_MASK
			#pragma shader_feature _DETAIL_ALBEDO_MAP
			#pragma shader_feature _DETAIL_NORMAL_MAP
			#pragma shader_feature _TESSELLATION_EDGE

			#pragma multi_compile _ LOD_FADE_CROSSFADE

			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog

			#pragma vertex MyTessellationVertexProgram
			#pragma fragment MyFragmentProgram
			#pragma hull MyHullProgram
			#pragma domain MyDomainProgram

			#define FORWARD_BASE_PASS

			#include "My Lighting.cginc"
			#include "MyTessellation.cginc"

			ENDCG
		}

		Pass {
			Tags {
				"LightMode" = "ForwardAdd"
			}

			Blend [_SrcBlend] One
			ZWrite Off
		    Cull Off

			CGPROGRAM

			#pragma target 4.6

			#pragma shader_feature _ _RENDERING_CUTOUT _RENDERING_FADE _RENDERING_TRANSPARENT
			#pragma shader_feature _METALLIC_MAP
			#pragma shader_feature _ _SMOOTHNESS_ALBEDO _SMOOTHNESS_METALLIC
			#pragma shader_feature _NORMAL_MAP
			#pragma shader_feature _PARALLAX_MAP
			#pragma shader_feature _DETAIL_MASK
			#pragma shader_feature _DETAIL_ALBEDO_MAP
			#pragma shader_feature _DETAIL_NORMAL_MAP
			#pragma shader_feature _TESSELLATION_EDGE

			#pragma multi_compile _ LOD_FADE_CROSSFADE

			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog
			
			#pragma vertex MyTessellationVertexProgram
			#pragma fragment MyFragmentProgram
			#pragma hull MyHullProgram
			#pragma domain MyDomainProgram

			#include "My Lighting.cginc"
			#include "MyTessellation.cginc"

			ENDCG
		}

		Pass {
			Tags {
				"LightMode" = "Deferred"
			}
		    Cull Off

			CGPROGRAM

			#pragma target 4.6
			#pragma exclude_renderers nomrt

			#pragma shader_feature _ _RENDERING_CUTOUT
			//#pragma shader_feature _RENDERING_TRANSPARENT
			#pragma shader_feature _METALLIC_MAP
			#pragma shader_feature _ _SMOOTHNESS_ALBEDO _SMOOTHNESS_METALLIC
			#pragma shader_feature _NORMAL_MAP
			#pragma shader_feature _PARALLAX_MAP
			#pragma shader_feature _OCCLUSION_MAP
			#pragma shader_feature _EMISSION_MAP
			#pragma shader_feature _DETAIL_MASK
			#pragma shader_feature _DETAIL_ALBEDO_MAP
			#pragma shader_feature _DETAIL_NORMAL_MAP
			#pragma shader_feature _TESSELLATION_EDGE

			#pragma multi_compile _ LOD_FADE_CROSSFADE

			#pragma multi_compile_prepassfinal

			#pragma vertex MyTessellationVertexProgram
			#pragma fragment MyFragmentProgram
			#pragma hull MyHullProgram
			#pragma domain MyDomainProgram

			#define DEFERRED_PASS

			#include "My Lighting.cginc"
			#include "MyTessellation.cginc"

			ENDCG
		}

		Pass {
			Tags {
				"LightMode" = "ShadowCaster"
			}
		    Cull Off

			CGPROGRAM

			#pragma target 4.6

			#pragma shader_feature _ _RENDERING_CUTOUT _RENDERING_FADE _RENDERING_TRANSPARENT
			#pragma shader_feature _SEMITRANSPARENT_SHADOWS
			#pragma shader_feature _SMOOTHNESS_ALBEDO
			#pragma shader_feature _PARALLAX_MAP
			#pragma shader_feature _TESSELLATION_EDGE

			#pragma multi_compile _ LOD_FADE_CROSSFADE

			#pragma multi_compile_shadowcaster

			#pragma vertex MyTessellationVertexProgram
			#pragma fragment MyShadowFragmentProgram
			#pragma hull MyHullProgram
			#pragma domain MyDomainProgram

			#include "My Shadows.cginc"
			#include "MyTessellation.cginc"

			ENDCG
		}
	}

	CustomEditor "MyLightingShaderGUI"
}