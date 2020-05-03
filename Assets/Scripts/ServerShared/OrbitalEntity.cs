using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;

public class OrbitalEntity : Entity
{
    private Guid _orbitData;
    
    public OrbitalEntity(GameContext context, Gear hull, IEnumerable<Gear> items, IEnumerable<ItemInstance> cargo, Guid orbit) : base(context, hull, items, cargo)
    {
        _orbitData = orbit;
    }

    public override void Update(float delta)
    {
        var newPosition = Context.GetOrbitPosition(_orbitData);
        Velocity = (newPosition - Position) / delta;
        Position = newPosition;
        base.Update(delta);
    }
}
