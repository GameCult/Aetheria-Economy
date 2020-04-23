using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;

public class OrbitalEntity : Entity
{
    private Guid _orbitData;
    private static Dictionary<Guid, float2> _orbitPositions = new Dictionary<Guid, float2>();
    
    public OrbitalEntity(GameContext context, Gear hull, IEnumerable<Gear> items, IEnumerable<ItemInstance> cargo, Guid orbit) : base(context, hull, items, cargo)
    {
        _orbitData = orbit;
    }

    public static void ClearOrbits()
    {
        _orbitPositions.Clear();
    }

    // Determine orbital position recursively, caching parent positions to avoid repeated calculations
    private float2 GetOrbitPosition(Guid orbit)
    {
        // Root orbit is fixed at center
        if(orbit==Guid.Empty)
            return float2.zero;
        
        if (!_orbitPositions.ContainsKey(orbit))
        {
            var orbitData = Context.Cache.Get<OrbitData>(orbit);
            _orbitPositions[orbit] = GetOrbitPosition(orbitData.Parent) + (orbitData.Period < .01f ? float2.zero : 
                OrbitData.Evaluate((float) (Context.Time / -orbitData.Period + orbitData.Phase)) *
                orbitData.Distance);
        }

        var position = _orbitPositions[orbit];
        if (float.IsNaN(position.x))
        {
            Context.Log("Orbit position is NaN, something went very wrong!");
            return float2.zero;
        }
        return _orbitPositions[orbit];
    }

    public override void Update(float delta)
    {
        var newPosition = GetOrbitPosition(_orbitData);
        Velocity = (newPosition - Position) / delta;
        Position = newPosition;
        base.Update(delta);
    }
}
