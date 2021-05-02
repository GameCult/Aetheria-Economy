#ifndef __PD_COLOR_CGINC__
#define __PD_COLOR_CGINC__

//note: Symmetric triangular distribution on [-1,1] with maximal density at 0
//note: from https://github.com/Unity-Technologies/ScriptableRenderPipeline/blob/master/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/FinalPass.shader#L124
float remap_noise_tri( float v )
{
    v = v*2.0-1.0;
    return sign(v) * (1.0 - sqrt(1.0 - abs(v)));
}
float4 remap_noise_tri( float4 v )
{
    v = v*2.0-1.0;
    return sign(v) * (1.0 - sqrt(1.0 - abs(v)));
}

//from https://github.com/h3r2tic/rtoy-samples/blob/master/assets/shaders/inc/color.inc
// Rec. 709
float3 RGB_YCbCr(float3 col)
{
    return float3( dot( col, float3(0.2126, 0.7152, 0.0722)),
                   dot( col, float3(-0.1146, -0.3854, 0.5)),
                   dot( col, float3(0.5, -0.4542, -0.0458)) );
}
float3 YCbCr_RGB(float3 col)
{
    return float3( col.r + 1.5748 * col.b,
                    col.r - 0.1873 * col.g -0.4681 * col.b, 
                    col.r + 1.8556 * col.g );
}

// https://software.intel.com/en-us/node/503873
float3 RGB_YCoCg(float3 c)
{
    // Y = R/4 + G/2 + B/4
    // Co = R/2 - B/2
    // Cg = -R/4 + G/2 - B/4
    return float3( c.x/4.0 + c.y/2.0 + c.z/4.0,
                   c.x/2.0 - c.z/2.0,
                  -c.x/4.0 + c.y/2.0 - c.z/4.0 );
}

// https://software.intel.com/en-us/node/503873
float3 YCoCg_RGB(float3 c)
{
    // R = Y + Co - Cg
    // G = Y + Cg
    // B = Y - Co - Cg
    return saturate(float3( c.x + c.y - c.z,
                            c.x + c.z,
                            c.x - c.y - c.z ));
}

// Samples a texture with Catmull-Rom filtering, using 9 texture fetches instead of 16.
// Source: https://gist.github.com/TheRealMJP/c83b8c0f46b63f3a88a5986f4fa982b1
// See http://vec3.ca/bicubic-filtering-in-fewer-taps/ for more details
float4 sample_catmull_rom(in sampler2D linearSampler, in float2 uv, in float2 texSize)
{
    // We're going to sample a a 4x4 grid of texels surrounding the target UV coordinate. We'll do this by rounding
    // down the sample location to get the exact center of our "starting" texel. The starting texel will be at
    // location [1, 1] in the grid, where [0, 0] is the top left corner.
    float2 samplePos = uv * texSize;
    float2 texPos1 = floor(samplePos - 0.5f) + 0.5f;

    // Compute the fractional offset from our starting texel to our original sample location, which we'll
    // feed into the Catmull-Rom spline function to get our filter weights.
    float2 f = samplePos - texPos1;

    // Compute the Catmull-Rom weights using the fractional offset that we calculated earlier.
    // These equations are pre-expanded based on our knowledge of where the texels will be located,
    // which lets us avoid having to evaluate a piece-wise function.
    float2 w0 = f * (-0.5f + f * (1.0f - 0.5f * f));
    float2 w1 = 1.0f + f * f * (-2.5f + 1.5f * f);
    float2 w2 = f * (0.5f + f * (2.0f - 1.5f * f));
    float2 w3 = f * f * (-0.5f + 0.5f * f);

    // Work out weighting factors and sampling offsets that will let us use bilinear filtering to
    // simultaneously evaluate the middle 2 samples from the 4x4 grid.
    float2 w12 = w1 + w2;
    float2 offset12 = w2 / w12;

    // Compute the final UV coordinates we'll use for sampling the texture
    float2 texPos0 = texPos1 - 1;
    float2 texPos3 = texPos1 + 2;
    float2 texPos12 = texPos1 + offset12;

    texPos0 /= texSize;
    texPos3 /= texSize;
    texPos12 /= texSize;

    float4 result = 0.0f;
    result += tex2D(linearSampler, float2(texPos0.x, texPos0.y)) * w0.x * w0.y;
    result += tex2D(linearSampler, float2(texPos12.x, texPos0.y)) * w12.x * w0.y;
    result += tex2D(linearSampler, float2(texPos3.x, texPos0.y)) * w3.x * w0.y;

    result += tex2D(linearSampler, float2(texPos0.x, texPos12.y)) * w0.x * w12.y;
    result += tex2D(linearSampler, float2(texPos12.x, texPos12.y)) * w12.x * w12.y;
    result += tex2D(linearSampler, float2(texPos3.x, texPos12.y)) * w3.x * w12.y;

    result += tex2D(linearSampler, float2(texPos0.x, texPos3.y)) * w0.x * w3.y;
    result += tex2D(linearSampler, float2(texPos12.x, texPos3.y)) * w12.x * w3.y;
    result += tex2D(linearSampler, float2(texPos3.x, texPos3.y)) * w3.x * w3.y;

    return max( 0.0, result );
}

//note: see http://vec3.ca/bicubic-filtering-in-fewer-taps/ for more details
//note: also http://mate.tue.nl/mate/pdfs/10318.pdf
float4 sample_cubic( in sampler2D linearSampler, float2 uv, float2 texSiz )
{
    //return 
    float2 iTc = uv * texSiz;
    float2 tc = floor(iTc - 0.5) + 0.5;

    float2 f = iTc - tc;
    
    const float2 f2 = float2( f * f );
    float2 w0 = f * ( f * ( 0.5 - 1.0/6.0 * f ) - 0.5 ) + (1.0/6.0);
    float2 w1 = (0.5 * f - 1.0) * f2 + (2.0/3.0);
    float2 w2 = f * ( f * (0.5 - 0.5 * f) + 0.5) + (1.0/6.0);
    float2 w3 = (1.0/6.0) * f*f2;

    float2 s0 = w0 + w1;
    float2 s1 = w2 + w3;

    float2 f0 = w1 / s0;
    float2 f1 = w3 / s1;

    float2 t0 = tc - 1.0 + f0;
    float2 t1 = tc + 1.0 + f1;

    const float2 rcpTexSiz = 1.0 / texSiz;

    return
        (tex2D( linearSampler, float2( t0.x, t0.y )*rcpTexSiz) * s0.x
    +  tex2D( linearSampler, float2( t1.x, t0.y )*rcpTexSiz) * s1.x) * s0.y
    + (tex2D( linearSampler, float2( t0.x, t1.y )*rcpTexSiz) * s0.x
    +  tex2D( linearSampler, float2( t1.x, t1.y )*rcpTexSiz) * s1.x ) * s1.y;
}


#endif // __PD_COLOR_CGINC__
