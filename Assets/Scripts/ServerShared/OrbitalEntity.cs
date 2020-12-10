using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;

public class OrbitalEntity : Entity
{
    public Guid OrbitData;
    
    public OrbitalEntity(ItemManager itemManager, Zone zone, EquippableItem hull, Guid orbit) : base(itemManager, zone, hull)
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
