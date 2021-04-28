#ifndef SLIME_VERTEX_INCLUDED
#define SLIME_VERTEX_INCLUDED

struct Vertex
{
    float3 pos;
    float2 uv;
    float intensity;
};

inline Vertex GetDefaultVertex() {

    Vertex ret;
    ret.pos = (0).xxx;
    ret.uv = (-1).xx;

    return ret;
}


#endif // SLIME_VERTEX_INCLUDED
