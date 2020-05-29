using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;

public class OrbitalEntity : Entity
{
    public Guid OrbitData;
    
    public OrbitalEntity(GameContext context, Guid hull, IEnumerable<Guid> items, IEnumerable<Guid> cargo, Guid orbit, Guid zone, Guid corporation) : base(context, hull, items, cargo, zone, corporation)
    {
        OrbitData = orbit;
    }

    public override void Update(float delta)
    {
        if (OrbitData != Guid.Empty)
        {
            Position = Context.GetOrbitPosition(OrbitData);
            Velocity = Context.GetOrbitVelocity(OrbitData);
        }
        
        base.Update(delta);
    }
}
