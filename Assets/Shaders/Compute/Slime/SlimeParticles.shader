Shader "Aetheria/Slime Particles" {
Properties {
	_StartColor("StartColor", Color) = (1,1,1,1)
	_EndColor("EndColor", Color) = (0,0,0,1)
	_Intensity("Intensity", Float) = 1
	_Roundedness("Roundedness", Float) = .1
}
   
SubShader {
Pass{
    Tags { "RenderType"="Opaque" }

	CGPROGRAM
	#pragma target 5.0

	#pragma vertex vert
	#pragma fragment frag

	#include "UnityCG.cginc"
	#include "SlimeVertex.cginc"
    #include "Assets/Shaders/Dither Functions.cginc"

	float _Intensity;
	float _Roundedness;
	fixed4 _StartColor;
	fixed4 _EndColor;
	StructuredBuffer<uint> _IndexBuffer;
	StructuredBuffer<Vertex> _VertexBuffer;

	Vertex GetVertex(uint indexBufferIdx) {
		uint idx = _IndexBuffer[indexBufferIdx];
		return _VertexBuffer[idx];
	}

	struct vs_out {
		float4 pos : SV_POSITION;
		float4 col : COLOR;
		float2 uv  : TEXCOORD;
		float4 screenPos  : TEXCOORD1;
	};

	vs_out vert (uint id : SV_VertexID, uint iId : SV_InstanceID)
	{
		vs_out Out;
		Vertex vtx = GetVertex(id);

		Out.pos = UnityObjectToClipPos(float4(vtx.pos, 1.0));
		Out.uv = vtx.uv;
		Out.col = lerp(_StartColor, _EndColor, vtx.uv.x);
		Out.screenPos = ComputeScreenPos(float4(vtx.pos, 1.0));

		return Out;
	}

	fixed4 frag (vs_out In) : COLOR0
	{
		// Simple quadratic curve "texture"
		float x = 1 - 2 * abs(In.uv.y - .5);
		//return float4(x,In.uv.x, 0 ,1);
		float i = x * x * smoothstep(0, _Roundedness, In.uv.x) * (1-In.uv.x) * _Intensity;
		
	    // Apply screen space dithering
	    float2 screenUV = In.screenPos.xy / In.screenPos.w;
        ditherClip(screenUV, i);
		
		return In.col * i;
	}

	ENDCG
   
   }
Pass{
    Name "ShadowCaster"
    Tags { "LightMode" = "ShadowCaster" }

	CGPROGRAM
	#pragma target 5.0

	#pragma vertex vert
	#pragma fragment frag

	#include "UnityCG.cginc"
	#include "SlimeVertex.cginc"
    #include "Assets/Shaders/Dither Functions.cginc"

	float _Intensity;
	float _Roundedness;
	StructuredBuffer<uint> _IndexBuffer;
	StructuredBuffer<Vertex> _VertexBuffer;

	Vertex GetVertex(uint indexBufferIdx) {
		uint idx = _IndexBuffer[indexBufferIdx];
		return _VertexBuffer[idx];
	}

	struct vs_out {
		float4 pos : SV_POSITION;
		float4 col : COLOR;
		float2 uv  : TEXCOORD;
		float4 screenPos  : TEXCOORD1;
	};

	vs_out vert (uint id : SV_VertexID, uint iId : SV_InstanceID)
	{
		vs_out Out;
		Vertex vtx = GetVertex(id);

		Out.pos = UnityObjectToClipPos(float4(vtx.pos, 1.0));
		Out.uv = vtx.uv;
		Out.col = 1;
		Out.screenPos = ComputeScreenPos(float4(vtx.pos, 1.0));

		return Out;
	}

	fixed4 frag (vs_out In) : COLOR0
	{
		// Simple quadratic curve "texture"
		float x = 1 - 2 * abs(In.uv.y - .5);
		float i = x * x * smoothstep(0, _Roundedness, In.uv.x) * (1-In.uv.x) * _Intensity;
		
	    // Apply screen space dithering
	    float2 screenUV = In.screenPos.xy / In.screenPos.w;
        ditherClip(screenUV, i);
		
        SHADOW_CASTER_FRAGMENT(In)
	}

	ENDCG
   
   }
}

Fallback Off
}

