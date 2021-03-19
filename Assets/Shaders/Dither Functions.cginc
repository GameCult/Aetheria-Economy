#ifndef __DITHER_FUNCTIONS__
#define __DITHER_FUNCTIONS__
#include "UnityCG.cginc"

sampler2D _DitheringTex;
float4 _DitheringCoords;

float isDithered(float2 pos, float alpha) {
    float dither = tex2D(_DitheringTex, pos * _DitheringCoords.xy + _DitheringCoords.zw).r;
    return alpha - dither - 0.0001 * (1 - ceil(alpha));
}

void ditherClip(float2 pos, float alpha) {
    clip(isDithered(pos, alpha));
}

#endif