using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;

public class OrbitalEntity : Entity
{
    public Guid OrbitData;
    
    public OrbitalEntity(GameContext context, Guid hull, IEnumerable<Guid> gear, IEnumerable<Guid> cargo, Guid orbit, Zone zone, Guid corporation) : base(context, hull, gear, cargo, zone, corporation)
    {
        OrbitData = orbit;
    }

    public override void Update(float delta)
    {
        if (OrbitData != Guid.Empty)
        {
            Position = Zone.GetOrbitPosition(OrbitData);
            Velocity = Zone.GetOrbitVelocity(OrbitData);
        }
        
        base.Update(delta);
    }
}
