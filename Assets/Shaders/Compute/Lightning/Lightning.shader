Shader "Aetheria/GPU Lightning" {
Properties {
	[HDR]_StartColor("StartColor", Color) = (1,1,1,1)
	[HDR]_EndColor("EndColor", Color) = (0,0,0,1)
}
   
SubShader {
Pass{
    Tags { "RenderType"="Opaque" }

	CGPROGRAM
	#pragma target 5.0
	#pragma shader_feature GPUTRAIL_TRAIL_INDEX_ON

	#pragma vertex vert
	#pragma fragment frag

	#include "UnityCG.cginc"
	#include "LightningVertex.cginc"
    #include "Assets/Shaders/Dither Functions.cginc"
	
	fixed4 _StartColor;
	fixed4 _EndColor;
	uint _VerticesPerInstance;
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
		float x = 2 * In.uv.y - 1;
		float a = 1 - x * x;
		
	    // Apply screen space dithering
	    float2 screenUV = In.screenPos.xy / In.screenPos.w;
        ditherClip(screenUV, a);
		
		return In.col * a;
	}

	ENDCG
   
   }
}

Fallback Off
}

