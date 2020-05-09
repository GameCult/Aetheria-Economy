using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;

public class OrbitalEntity : Entity
{
    public Guid OrbitData;
    
    public OrbitalEntity(GameContext context, Guid hull, IEnumerable<Guid> items, IEnumerable<Guid> cargo, Guid orbit, Guid zone) : base(context, hull, items, cargo, zone)
    {
        OrbitData = orbit;
    }

    public override void Update(float delta)
    {
        if (OrbitData != Guid.Empty)
        {
            Velocity = Context.GetOrbitVelocity(OrbitData);
            Position = Context.GetOrbitPosition(OrbitData);
        }
        
        base.Update(delta);
    }
}
