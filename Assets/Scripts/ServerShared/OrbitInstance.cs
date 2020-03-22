using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public class OrbitInstance
{
    private OrbitData _data;
    
    public OrbitInstance(OrbitData data)
    {
        _data = data;
    }

    public static float2 Evaluate(float phase)
    {
        phase *= (PI * 2);
        return new float2(sin(phase), cos(phase));
    }
}
