#ifndef GPUTRAIL_VARIABLES_INCLUDED
#define GPUTRAIL_VARIABLES_INCLUDED

#include "GPUTrailVertex.cginc"

fixed4 _StartColor;
fixed4 _EndColor;
uint _VerticesPerInstance;
StructuredBuffer<uint> _IndexBuffer;
StructuredBuffer<Vertex> _VertexBuffer;

#ifdef GPUTRAIL_TRAIL_INDEX_ON
StructuredBuffer<uint> _TrailIndexBuffer;
#endif

Vertex GetVertex(uint indexBufferIdx, uint trailIdx){
#ifdef GPUTRAIL_TRAIL_INDEX_ON
	trailIdx = _TrailIndexBuffer[trailIdx];
#endif
	uint idx = _IndexBuffer[indexBufferIdx];
	idx += trailIdx * _VerticesPerInstance;
	return _VertexBuffer[idx];
}

#endif // GPUTRAIL_VARIABLES_INCLUDED
