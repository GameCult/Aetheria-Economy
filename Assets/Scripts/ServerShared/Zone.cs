using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;

public class Zone
{
    //public HashSet<Guid> Planets = new HashSet<Guid>();
    public Dictionary<Guid, Entity> Entities = new Dictionary<Guid, Entity>();
    //public Dictionary<Guid, OrbitData> Orbits = new Dictionary<Guid, OrbitData>();
    public Dictionary<Guid, PlanetData> Planets = new Dictionary<Guid, PlanetData>();

    public Dictionary<Guid, Orbit> Orbits = new Dictionary<Guid, Orbit>();
    public Dictionary<Guid, AsteroidBelt> AsteroidBelts = new Dictionary<Guid, AsteroidBelt>();
    
    private HashSet<Guid> _updatedOrbits;

    private GameContext _context;

    public ZoneData Data { get; }

    public Zone(GameContext context, ZonePack pack)
    {
        Data = pack.Data;
        _context = context;
        
        foreach (var orbit in pack.Orbits)
        {
            Orbits.Add(orbit.ID, new Orbit(orbit));
        }
        foreach (var planet in pack.Planets) Planets.Add(planet.ID, planet);
        foreach (var entity in pack.Entities) Entities.Add(entity.ID, entity);

        foreach (var belt in pack.Planets.Where(p => p is AsteroidBeltData).Cast<AsteroidBeltData>()) 
            AsteroidBelts[belt.ID] = new AsteroidBelt(belt.Asteroids.Length);

        // TODO: Associate planets with stored entities for planetary colonies
    }

    public ZonePack Pack()
    {
        return new ZonePack
        {
            Data = Data,
            Entities = Entities.Values.ToList(),
            Orbits = Orbits.Values.Select(o=>o.Data).ToList(),
            Planets = Planets.Values.ToList()
        };
    }

    public void AddOrbit(OrbitData orbit)
    {
        Orbits.Add(orbit.ID, new Orbit(orbit));
    }

    public void Update(float deltaTime)
    {
        _updatedOrbits = new HashSet<Guid>();
        foreach (var orbit in Orbits)
        {
            orbit.Value.PreviousPosition = orbit.Value.Position;
            orbit.Value.Position = GetOrbitPosition(orbit.Key);
            orbit.Value.Velocity = (orbit.Value.Position - orbit.Value.PreviousPosition) / deltaTime;
        }

        foreach (var asteroids in AsteroidBelts.Keys)
            UpdateAsteroidTransforms(asteroids);
        
        foreach (var entity in Entities.Values) entity.Update(deltaTime);
    }

    public float2 WormholePosition(Guid target)
    {
        var direction = _context.Cache.Get<ZoneData>(target).Position - Data.Position;
        return normalize(direction) * Data.Radius * .95f;
    }
    
    // Determine orbital position recursively, caching parent positions to avoid repeated calculations
    public float2 GetOrbitPosition(Guid orbit)
    {
        // Root orbit is fixed at center
        if(orbit==Guid.Empty)
            return float2.zero;
        
        if (!_updatedOrbits.Contains(orbit))
        {
            var orbitData = Orbits[orbit].Data;
            float2 pos = float2.zero;
            if (orbitData.Period > .01f)
            {
                var phase = (float) frac(_context.Time / orbitData.Period * _context.GlobalData.OrbitSpeedMultiplier);
                pos = OrbitData.Evaluate(frac(phase + orbitData.Phase)) * orbitData.Distance;
                
                if (float.IsNaN(pos.x))
                {
                    _context.Log("Orbit position is NaN, something went very wrong!");
                    pos = float2.zero;
                }
            }

            _updatedOrbits.Add(orbit);
            return GetOrbitPosition(orbitData.Parent) + pos;
        }

        return Orbits[orbit].Position;
    }

    public float2 GetOrbitVelocity(Guid orbit)
    {
        if (Orbits.ContainsKey(orbit))
            return Orbits[orbit].Velocity;
        return float2.zero;
    }

    public int NearestAsteroid(Guid planetDataID, float2 position)
    {
        var beltData = Planets[planetDataID] as AsteroidBeltData;

        var asteroidPositions = GetAsteroidTransforms(planetDataID);

        int nearest = 0;
        float nearestDistance = Single.MaxValue;
        for (int i = 0; i < beltData.Asteroids.Length; i++)
        {
            var dist = lengthsq(asteroidPositions[i].xy - position);
            if (AsteroidExists(planetDataID, i) && dist < nearestDistance)
            {
                nearest = i;
                nearestDistance = dist;
            }
        }

        return nearest;
    }

    public bool AsteroidExists(Guid planetDataID, int asteroid) => ((AsteroidBeltData) Planets[planetDataID]).Asteroids.Length > asteroid && asteroid >= 0;

    public float2 GetAsteroidVelocity(Guid planetDataID, int asteroid)
    {
        return (AsteroidBelts[planetDataID].Transforms[asteroid].xy - AsteroidBelts[planetDataID].PreviousTransforms[asteroid].xy);
    }

    public float4 GetAsteroidTransform(Guid planetDataID, int asteroid)
    {
        return AsteroidBelts[planetDataID].Transforms[asteroid];
    }

    public float4[] GetAsteroidTransforms(Guid planetDataID)
    {
        return AsteroidBelts[planetDataID].Transforms;
    }

    private void UpdateAsteroidTransforms(Guid planetDataID)
    {
        var beltData = Planets[planetDataID] as AsteroidBeltData;
        
        var belt = AsteroidBelts[planetDataID];
        
        Array.Copy(belt.Transforms, belt.PreviousTransforms, beltData.Asteroids.Length);

        var orbitData = Orbits[beltData.Orbit].Data;
        var orbitPosition = GetOrbitPosition(orbitData.Parent);
        for (var i = 0; i < beltData.Asteroids.Length; i++)
        {
            var size = beltData.Asteroids[i].Size;
            if(belt.RespawnTimers.ContainsKey(i)) size = 0;
            else if (belt.Damage.ContainsKey(i))
            {
                var hpLerp = unlerp(_context.GlobalData.AsteroidSizeMin, _context.GlobalData.AsteroidSizeMax, size);
                var asteroidHitpoints = lerp(_context.GlobalData.AsteroidHitpointsMin, _context.GlobalData.AsteroidHitpointsMax, pow(hpLerp, _context.GlobalData.AsteroidHitpointsPower));
                var sizeLerp = (asteroidHitpoints - belt.Damage[i]) / asteroidHitpoints;
                size = lerp(_context.GlobalData.AsteroidSizeMin, size, sizeLerp);
            }
        
            belt.Transforms[i] = float4(
                OrbitData.Evaluate((float) frac(_context.Time / _context.GlobalData.OrbitalPeriod(beltData.Asteroids[i].Distance) * _context.GlobalData.OrbitSpeedMultiplier +
                                                beltData.Asteroids[i].Phase)) * beltData.Asteroids[i].Distance + orbitPosition,
                (float) (_context.Time * beltData.Asteroids[i].RotationSpeed % 360.0), size);
        }
    }

    public OrbitData CreateOrbit(Guid parent, float2 position)
    {
        var parentPosition = GetOrbitPosition(parent);
        var delta = position - parentPosition;
        var distance = length(delta);
        var period = _context.GlobalData.OrbitalPeriod(distance);
        var phase = atan2(delta.y, delta.x) / (PI * 2);
        var currentPhase = frac(_context.Time / period * _context.GlobalData.OrbitSpeedMultiplier);
        var storedPhase = (float) frac(phase - currentPhase);

        var orbit = new OrbitData
        {
            Context = _context,
            Distance = distance,
            Parent = parent,
            Period = period,
            Phase = storedPhase
        };
        Orbits.Add(orbit.ID, new Orbit(orbit));
        return orbit;
    }

    public void MineAsteroid(Entity miner, Guid asteroidBelt, int asteroid, float damage, float efficiency, float penetration)
    {
        var planetData = Planets[asteroidBelt];
        var belt = AsteroidBelts[asteroidBelt];
        var asteroidTransform = belt.Transforms[asteroid];
        var hpLerp = unlerp(_context.GlobalData.AsteroidSizeMin, _context.GlobalData.AsteroidSizeMax, asteroidTransform.w);
        var asteroidHitpoints = lerp(_context.GlobalData.AsteroidHitpointsMin, _context.GlobalData.AsteroidHitpointsMax, pow(hpLerp, _context.GlobalData.AsteroidHitpointsPower));
        
        if (!belt.Damage.ContainsKey(asteroid))
            belt.Damage[asteroid] = 0;
        belt.Damage[asteroid] = belt.Damage[asteroid] + damage;
        
        if (!belt.MiningAccumulator.ContainsKey((miner.ID, asteroid)))
            belt.MiningAccumulator[(miner.ID, asteroid)] = 0;
        belt.MiningAccumulator[(miner.ID, asteroid)] = belt.MiningAccumulator[(miner.ID, asteroid)] + damage;
        
        if (belt.Damage[asteroid] > asteroidHitpoints)
        {
            belt.RespawnTimers[asteroid] =
                lerp(_context.GlobalData.AsteroidRespawnMin, _context.GlobalData.AsteroidRespawnMax, hpLerp);
            belt.Damage.Remove(asteroid);
            belt.MiningAccumulator.Remove((miner.ID, asteroid));
            return;
        }

        var resourceCount = planetData.Resources.Sum(x => x.Value);
        var resource = planetData.Resources.MaxBy(x => pow(x.Value, 1f / penetration) * _context.Random.NextFloat());
        if (efficiency * _context.Random.NextFloat() * belt.MiningAccumulator[(miner.ID, asteroid)] * resourceCount / _context.GlobalData.MiningDifficulty > 1 && miner.OccupiedCapacity < miner.Capacity - 1)
        {
            belt.MiningAccumulator.Remove((miner.ID, asteroid));
            var newSimpleCommodity = new SimpleCommodity
            {
                Context = _context,
                Data = resource.Key,
                Quantity = 1
            };
            _context.Cache.Add(newSimpleCommodity);
            miner.AddCargo(newSimpleCommodity);
        }
    }

    public override int GetHashCode()
    {
        return Data.ID.GetHashCode();
    }
}

public class AsteroidBelt
{
    public float4[] Transforms; // x, y, rotation, scale
    public float4[] PreviousTransforms; // x, y, rotation, scale
    public Dictionary<int, float> RespawnTimers = new Dictionary<int, float>();
    public Dictionary<int, float> Damage = new Dictionary<int, float>();
    public Dictionary<(Guid, int), float> MiningAccumulator = new Dictionary<(Guid, int), float>();

    public AsteroidBelt(int count)
    {
        Transforms = new float4[count];
        PreviousTransforms = new float4[count];
    }
}

public class Orbit
{
    public OrbitData Data { get; }
    public float2 Velocity = float2.zero;
    public float2 Position = float2.zero;
    public float2 PreviousPosition = float2.zero;

    public Orbit(OrbitData data)
    {
        Data = data;
    }
}
