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

uniform sampler2D _Surface;
uniform sampler2D _Patch;
uniform sampler2D _Displacement;
uniform sampler2D _TintTexture;

uniform float3 _GridTransform;
	
uniform half4 _Tint;

uniform half _GridFillDensity,
            _GridFloorDensity,
            _GridPatchDensity,
            _GridFloorOffset,
            _GridFloorBlend,
            _GridPatchBlend,
            _TintExponent,
            _DepthBlend,
            _NoiseFrequency,
            _NoiseExponent,
            _NoiseStrength,
            _NoiseSpeed,
            _TurbulenceScale,
            _TurbulenceAmplitude,
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
    const float surface = tex2Dlod(_Surface, half4(uv, 0, 0)).r;
    const float dist = pos.y + surface - _GridFloorOffset;

    const float fillDensity = saturate(-pos.y * _GridFillDensity);

    // if(dist < _SafetyDistance)
    // {
        const float patch = tex2Dlod(_Patch, half4(uv, 0, 0)).r;
        const float displacement = tex2Dlod(_Displacement, half4(uv, 0, 0)).r;

        const float patchDensity = saturate((-abs(pos.y+displacement)+patch)/_GridPatchBlend)*_GridPatchDensity;
        const float floorDist = -pos.y-surface+_GridFloorOffset;
        const float floorDensity = floorDist/_GridFloorBlend*_GridFloorDensity;
        const float fogDensity = patchDensity + max(0,floorDensity);
        return min(max(fogDensity, 0) + fillDensity, .99);
    // }

    // return fillDensity;
    
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

float d(float3 pos)
{
    const float4 simplex = Value3D_Deriv( pos / _TurbulenceScale - float3(0,_Time.x,0) );
    const float3 normal = calcNormal(pos);
    const float3 turbulence = cross(normalize(simplex.yzw), normal) * _TurbulenceAmplitude;
    //const float3 turbulence = (simplex.yzw) * _TurbulenceAmplitude;
    const float lerp1 = frac(_Time.x * _FlowSpeed);
    const float lerp2 = frac(_Time.x * _FlowSpeed + .5);
    const float noise1 = pow(triNoise3d((pos+turbulence * lerp1)*_NoiseFrequency),_NoiseExponent) * _NoiseStrength * parabola(lerp1, 2);
    const float noise2 = pow(triNoise3d((pos+turbulence * lerp2)*_NoiseFrequency),_NoiseExponent) * _NoiseStrength * parabola(lerp2, 2);
    //pos.y += noise1 + noise2;
    return f(pos - normalize(turbulence) * (noise1 + noise2));
}

float4 VolumeSampleColor(float3 pos)
{
    float2 uv = -(pos.xz-_GridTransform.xy)/_GridTransform.z + float2(.5,.5);
	float density = d(pos);
    const float4 tint = tex2Dlod(_TintTexture, half4(uv, 0, 1 / density)) * density;
    const float albedo = pow(1-density, _TintExponent) * smoothstep(0,-250,pos.y);
    return float4((albedo*_Tint*tint).rgb, density) ;
}
