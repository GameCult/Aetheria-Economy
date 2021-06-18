#include "UnityCG.cginc"

//
//  Value Noise 3D Deriv
//  Return value range of 0.0->1.0, with format float4( value, xderiv, yderiv, zderiv )
//
float4 Value3D_Deriv( float3 P )
{
    //  https://github.com/BrianSharpe/Wombat/blob/master/Value3D_Deriv.glsl

    // establish our grid cell and unit position
    float3 Pi = floor(P);
    float3 Pf = P - Pi;
    float3 Pf_min1 = Pf - 1.0;

    // clamp the domain
    Pi.xyz = Pi.xyz - floor(Pi.xyz * ( 1.0 / 69.0 )) * 69.0;
    float3 Pi_inc1 = step( Pi, float3( (69.0 - 1.5).xxx ) ) * ( Pi + 1.0 );

    // calculate the hash
    float4 Pt = float4( Pi.xy, Pi_inc1.xy ) + float2( 50.0, 161.0 ).xyxy;
    Pt *= Pt;
    Pt = Pt.xzxz * Pt.yyww;
    float2 hash_mod = float2( 1.0 / ( 635.298681 + float2( Pi.z, Pi_inc1.z ) * 48.500388 ) );
    float4 hash_lowz = frac( Pt * hash_mod.xxxx );
    float4 hash_highz = frac( Pt * hash_mod.yyyy );

    //	blend the results and return
    float3 blend = Pf * Pf * Pf * (Pf * (Pf * 6.0 - 15.0) + 10.0);
    float3 blendDeriv = Pf * Pf * (Pf * (Pf * 30.0 - 60.0) + 30.0);
    float4 res0 = lerp( hash_lowz, hash_highz, blend.z );
    float4 res1 = lerp( res0.xyxz, res0.zwyw, blend.yyxx );
    float4 res3 = lerp( float4( hash_lowz.xy, hash_highz.xy ), float4( hash_lowz.zw, hash_highz.zw ), blend.y );
    float2 res4 = lerp( res3.xz, res3.yw, blend.x );
    return float4( res1.x, 0.0, 0.0, 0.0 ) + ( float4( res1.yyw, res4.y ) - float4( res1.xxz, res4.x ) ) * float4( blend.x, blendDeriv );
    return float4(1,1,1,1);
}

uniform sampler2D _NebulaSurfaceHeight;
float4 _NebulaSurfaceHeight_TexelSize;
uniform sampler2D _NebulaPatch;
uniform sampler2D _NebulaPatchHeight;
uniform sampler2D _NebulaTint;
float4 _NebulaTint_TexelSize;

uniform float3 _GridTransform;

uniform half _NebulaFillDensity,
            _NebulaFloorDensity,
            _NebulaPatchDensity,
            _NebulaFloorOffset,
            _NebulaFloorBlend,
            _NebulaPatchBlend,
            _TintExponent,
            _NoiseScale,
            _NoiseExponent,
            _NoiseAmplitude,
            _NoiseSpeed,
            _FlowScale,
            _FlowAmplitude,
            _FlowScroll,
            _FlowSpeed,
            _SafetyDistance;

float tri(in float x){return abs(frac(x)-.5);}
float3 tri3(in float3 p){return float3( tri(p.z+tri(p.y*1.)), tri(p.z+tri(p.x*1.)), tri(p.y+tri(p.x*1.)));}

float triNoise3d(in float3 p)
{
    float z=1.4;
    float rz = 0.;
    float3 bp = p;
    for (float i=0.; i<=2.; i++ )
    {
        float3 dg = tri3(bp * 2.);
        p += dg + _Time.y * _NoiseSpeed;

        bp *= 1.8;
        z *= 1.5;
        p *= 1.2;
	        
        rz+= (tri(p.z+tri(p.x+tri(p.y))))/z;
        bp += 0.14;
    }
    return rz;
}

float parabola( float x, float k )
{
    return pow( 4.0*x*(1.0-x), k );
}

float f(float3 pos)
{
    float2 uv = -(pos.xz-_GridTransform.xy)/_GridTransform.z + float2(.5,.5);
    const float surfaceDisp = tex2Dlod(_NebulaSurfaceHeight, half4(uv, 0, 0)).r;
    const float dist = pos.y + surfaceDisp - _NebulaFloorOffset;

    const float fillDensity = smoothstep(_SafetyDistance,0,dist) * _NebulaFillDensity;

    if(dist < _SafetyDistance)
    {
        const float patch = tex2Dlod(_NebulaPatch, half4(uv, 0, 0)).r;
        const float patchDisp = tex2Dlod(_NebulaPatchHeight, half4(uv, 0, 0)).r;

        const float patchDensity = saturate((-abs(pos.y+patchDisp - _NebulaFloorOffset)+patch)/_NebulaPatchBlend)*_NebulaPatchDensity;
        const float floorDist = -dist;
        const float floorDensity = floorDist/_NebulaFloorBlend*_NebulaFloorDensity;
        const float fogDensity = patchDensity + max(0,floorDensity);
        return fogDensity + fillDensity;
    }

    return fillDensity;
    
}

float3 calcNormal (float3 pos, float2 uv)
{
    float3 n = float3(pos.x, 
                      tex2D(_NebulaSurfaceHeight, float2(uv.x, uv.y + _NebulaSurfaceHeight_TexelSize.y)).x, 
                      pos.z + _NebulaSurfaceHeight_TexelSize.y * _GridTransform.z);
    float3 me = float3(pos.x, 
                     tex2D(_NebulaSurfaceHeight, float2(uv.x, uv.y)).x, 
                     pos.z);
    float3 e = float3(pos.x + _NebulaSurfaceHeight_TexelSize.x * _GridTransform.z, 
                     tex2D(_NebulaSurfaceHeight, float2(uv.x + _NebulaSurfaceHeight_TexelSize.x, uv.y)).x, 
                     pos.z);
    float3 norm = cross(normalize(n-me),normalize(e-me));
    return norm;
}

float2 tintGradient (float3 pos, float2 uv, float mip, out float3 tint)
{
    tint = tex2Dlod(_NebulaTint, float4(uv.x, uv.y, 0, mip));
    float me = length(tint);
    return float2(
        length(tex2Dlod(_NebulaTint, float4(uv.x + 0.001953125, uv.y, 0, mip))) - me,
        length(tex2Dlod(_NebulaTint, float4(uv.x, uv.y + 0.001953125, 0, mip))) - me
    );
}

float3 calcNormal( float3 p ) // for function f(p)
{
    const float h = 0.0001; // replace by an appropriate value
    const float2 k = float2(1,-1);
    return normalize( k.xyy*f( p + k.xyy*h ) + 
                      k.yyx*f( p + k.yyx*h ) + 
                      k.yxy*f( p + k.yxy*h ) + 
                      k.xxx*f( p + k.xxx*h ) );
}

float3 flow(float3 pos, out float noise)
{
    //float2 uv = -(pos.xz-_GridTransform.xy)/_GridTransform.z + float2(.5,.5);
    //normal = calcNormal(pos);
    //return normalize(float3(-normal.x,0,-normal.z)) * _TurbulenceAmplitude;
    const float4 noiseSample1 = Value3D_Deriv( pos / _FlowScale - float3(0,_FlowScroll,0) );
    const float4 noiseSample2 = Value3D_Deriv( (pos / _FlowScale - float3(0,_FlowScroll,0)) / 1.61803398875 ); // make it golden
    noise = noiseSample1.x;
    return cross(normalize(noiseSample2.yzw), normalize(noiseSample1.yzw)) * _FlowAmplitude;
    //return BitangentNoise3D(pos / _FlowScale - float3(0,_FlowScroll,0)) * _FlowAmplitude;
}

float d(float3 pos)
{
    float noise;
    const float3 fl = flow(pos, noise);
    const float lerp1 = frac(_Time.x * _FlowSpeed + noise);
    const float lerp2 = frac(_Time.x * _FlowSpeed + noise + .5);
    const float noise1 = pow(triNoise3d((pos+fl * lerp1) / _NoiseScale),_NoiseExponent) * _NoiseAmplitude * parabola(lerp1, 2);
    const float noise2 = pow(triNoise3d((pos+fl * lerp2) / _NoiseScale),_NoiseExponent) * _NoiseAmplitude * parabola(lerp2, 2);
    pos.y += (noise1 + noise2);
    return f(pos);
}

float4 VolumeSampleColor(float3 pos)
{
    float2 uv = -(pos.xz-_GridTransform.xy)/_GridTransform.z + float2(.5,.5);
	float density = d(pos);
    float3 tint = tex2Dlod(_NebulaTint, float4(uv.x, uv.y, 0, 1 / pow(density, .25)));
    //float2 tintGrad = tintGradient(pos, uv, 1 / pow(density, .25), tint);
    //float3 tintNormal = normalize(float3(tintGrad.x, 0, tintGrad.y));
    //tint *= abs(dot(normal, float3(0,-1,0)));
    // pow(abs(1-density), _TintExponent)
    const float albedo = smoothstep(0,-250,pos.y) * pow(1/max(.01,density), _TintExponent);
    return float4(albedo*tint, density);
}
