﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;
using Random = Unity.Mathematics.Random;

public class Zone
{
    //public HashSet<Guid> Planets = new HashSet<Guid>();
    public Dictionary<Guid, Entity> Entities = new Dictionary<Guid, Entity>();
    //public Dictionary<Guid, OrbitData> Orbits = new Dictionary<Guid, OrbitData>();
    public Dictionary<Guid, BodyData> Planets = new Dictionary<Guid, BodyData>();
    public Dictionary<Guid, Planet> PlanetInstances = new Dictionary<Guid, Planet>();

    public Dictionary<Guid, Orbit> Orbits = new Dictionary<Guid, Orbit>();
    public Dictionary<Guid, AsteroidBelt> AsteroidBelts = new Dictionary<Guid, AsteroidBelt>();
    
    private HashSet<Guid> _updatedOrbits = new HashSet<Guid>();

    private PlanetSettings _settings;
    private float _time;
    private Random _random;

    public ZoneData Data { get; }

    public Zone(PlanetSettings settings, ZonePack pack)
    {
        Data = pack.Data;
        _settings = settings;
        _random = new Random(Convert.ToUInt32(abs(Data.ID.GetHashCode())));
        
        foreach (var orbit in pack.Orbits)
        {
            Orbits.Add(orbit.ID, new Orbit(_settings, orbit));
        }
        
        foreach (var planet in pack.Planets)
        {
            Planets.Add(planet.ID, planet);
            switch (planet)
            {
                case AsteroidBeltData belt:
                    AsteroidBelts[belt.ID] = new AsteroidBelt(belt);
                    break;
                case GasGiantData gas:
                    PlanetInstances.Add(gas.ID, new GasGiant(settings, gas, Orbits[planet.Orbit]));
                    break;
                default:
                    PlanetInstances.Add(planet.ID, new Planet(settings, planet, Orbits[planet.Orbit]));
                    break;
            }
        }
        
        foreach (var entity in pack.Entities) Entities.Add(entity.ID, entity);

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
        Orbits.Add(orbit.ID, new Orbit(_settings, orbit));
    }

    public void Update(float time, float deltaTime)
    {
        _time = time;
        _updatedOrbits.Clear();
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

    public float2 WormholePosition(ZoneDefinition target)
    {
        var direction = target.Position - Data.Position;
        return normalize(direction) * Data.Radius * .95f;
    }
    
    // Determine orbital position recursively, caching parent positions to avoid repeated calculations
    public float2 GetOrbitPosition(Guid orbitID)
    {
        // Root orbit is fixed at origin
        if(orbitID==Guid.Empty)
            return float2.zero;
        
        if (!_updatedOrbits.Contains(orbitID))
        {
            var orbit = Orbits[orbitID];
            float2 pos = float2.zero;
            if (orbit.Period.Value > .01f)
            {
                var phase = (float) frac(_time / orbit.Period.Value);
                pos = OrbitData.Evaluate(frac(phase + orbit.Data.Phase)) * orbit.Data.Distance.Value;
                
                if (float.IsNaN(pos.x))
                {
                    //_context.Log("Orbit position is NaN, something went very wrong!");
                    pos = float2.zero;
                }
            }

            _updatedOrbits.Add(orbitID);
            return GetOrbitPosition(orbit.Data.Parent) + pos;
        }

        return Orbits[orbitID].Position;
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
            float size;
            if(belt.RespawnTimers.ContainsKey(i)) size = 0;
            else if (belt.Damage.ContainsKey(i))
            {
                var asteroidHitpoints = _settings.AsteroidHitpoints.Evaluate(beltData.Asteroids[i].Size);
                var damage = (asteroidHitpoints - belt.Damage[i]) / asteroidHitpoints;
                size = _settings.AsteroidSize.Evaluate(damage * beltData.Asteroids[i].Size);
            }
            else size = _settings.AsteroidSize.Evaluate(beltData.Asteroids[i].Size);
        
            belt.Transforms[i] = float4(
                OrbitData.Evaluate((float) frac(_time / _settings.OrbitPeriod.Evaluate(beltData.Asteroids[i].Distance) +
                                                beltData.Asteroids[i].Phase)) * beltData.Asteroids[i].Distance + orbitPosition,
                (float) (_time * beltData.Asteroids[i].RotationSpeed % (PI * 2)), size);
        }
    }

    public OrbitData CreateOrbit(Guid parent, float2 position)
    {
        var parentPosition = GetOrbitPosition(parent);
        var delta = position - parentPosition;
        var distance = length(delta);
        var period = _settings.OrbitPeriod.Evaluate(distance);
        var phase = atan2(delta.y, delta.x) / (PI * 2);
        var currentPhase = frac(_time / period);
        var storedPhase = (float) frac(phase - currentPhase);

        var orbit = new OrbitData
        {
            Distance = new ReactiveProperty<float>(distance),
            Parent = parent,
            Phase = storedPhase
        };
        Orbits.Add(orbit.ID, new Orbit(_settings, orbit));
        return orbit;
    }

    public void MineAsteroid(Entity miner, Guid asteroidBelt, int asteroid, float damage, float efficiency, float penetration)
    {
        var beltData = Planets[asteroidBelt] as AsteroidBeltData;
        var belt = AsteroidBelts[asteroidBelt];
        var asteroidTransform = belt.Transforms[asteroid];

        var size = beltData.Asteroids[asteroid].Size;
        var asteroidHitpoints = _settings.AsteroidHitpoints.Evaluate(size);
        
        if (!belt.Damage.ContainsKey(asteroid))
            belt.Damage[asteroid] = 0;
        belt.Damage[asteroid] = belt.Damage[asteroid] + damage;
        
        if (!belt.MiningAccumulator.ContainsKey((miner.ID, asteroid)))
            belt.MiningAccumulator[(miner.ID, asteroid)] = 0;
        belt.MiningAccumulator[(miner.ID, asteroid)] = belt.MiningAccumulator[(miner.ID, asteroid)] + damage;
        
        if (belt.Damage[asteroid] > asteroidHitpoints)
        {
            belt.RespawnTimers[asteroid] = _settings.AsteroidRespawnTime.Evaluate(size);
            belt.Damage.Remove(asteroid);
            belt.MiningAccumulator.Remove((miner.ID, asteroid));
            return;
        }

        var resourceCount = beltData.Resources.Sum(x => x.Value);
        var resource = beltData.Resources.MaxBy(x => pow(x.Value, 1f / penetration) * _random.NextFloat());
        if (efficiency * _random.NextFloat() * belt.MiningAccumulator[(miner.ID, asteroid)] * resourceCount / _settings.MiningDifficulty > 1 && miner.OccupiedCapacity < miner.Capacity - 1)
        {
            belt.MiningAccumulator.Remove((miner.ID, asteroid));
            var newSimpleCommodity = new SimpleCommodity
            {
                Data = resource.Key,
                Quantity = 1
            };
            miner.AddCargo(newSimpleCommodity);
        }
    }
    public float GetHeight(float2 position)
    {
        float result = -PowerPulse(length(position)/(Data.Radius*2), _settings.ZoneDepthExponent) * _settings.ZoneDepth;
        foreach (var body in PlanetInstances.Values)
        {
            var p = (position - body.Orbit.Position); //GetOrbitPosition(body.BodyData.Orbit)
            var dist = lengthsq(p);
            var gravityRadius = body.GravityWellRadius.Value;
            if (dist < gravityRadius*gravityRadius)
            {
                var depth = body.GravityWellDepth.Value;
                result -= PowerPulse(sqrt(dist) / gravityRadius, body.BodyData.GravityDepthExponent.Value) * depth;
            }

            if (body is GasGiant gas)
            {
                var waveRadius = gas.GravityWavesRadius.Value;
                if(dist < waveRadius*waveRadius)
                {
                    var depth = gas.GravityWavesDepth.Value;
                    var frequency = _settings.WaveFrequency.Evaluate(body.BodyData.Mass.Value);
                    var speed = _settings.WaveSpeed.Evaluate(body.BodyData.Mass.Value);
                    result -= RadialWaves(sqrt(dist) / waveRadius, 8, 1.5f, frequency, _time * speed) * depth;
                }
            }
        }

        return result;
    }

    public float2 GetForce(float2 position)
    {
        var normal = GetNormal(position);
        var f = new float2(normal.x, normal.z);
        return f * _settings.GravityStrength * lengthsq(f);// * Mathf.Abs(GetHeight(position));
    }

    public static float PowerPulse(float x, float exponent)
    {
        x *= 2;
        x = clamp(x, -1, 1);
        return pow((x + 1) * (1 - x), exponent);
    }

    public static float RadialWaves(float x, float maskExponent, float sineExponent, float frequency, float phase)
    {
        x *= 2;
        return PowerPulse(x, maskExponent) * cos(pow(x, sineExponent) * frequency + phase);
    }

    public float3 GetNormal(float2 pos, float step, float mul)
    {
        float hL = GetHeight(new float2(pos.x - step, pos.y)) * mul;
        float hR = GetHeight(new float2(pos.x + step, pos.y)) * mul;
        float hD = GetHeight(new float2(pos.x, pos.y - step)) * mul;
        float hU = GetHeight(new float2(pos.x, pos.y + step)) * mul;

        // Deduce terrain normal
        float3 normal = new float3((hL - hR), (hD - hU), 2);
        normalize(normal);
        return new float3(normal.x, normal.z, normal.y);
    }
    
    public float3 GetNormal(float2 pos)
    {
        return GetNormal(pos, .1f, 1);
    }

    public override int GetHashCode()
    {
        return Data.ID.GetHashCode();
    }
}

public class Planet
{
    public Orbit Orbit;
    public BodyData BodyData;
    public ReadOnlyReactiveProperty<float> GravityWellDepth;
    public ReadOnlyReactiveProperty<float> GravityWellRadius;
    public ReadOnlyReactiveProperty<float> BodyRadius;

    public Planet(PlanetSettings settings, BodyData data, Orbit orbit)
    {
        Orbit = orbit;
        BodyData = data;
        BodyRadius = new ReadOnlyReactiveProperty<float>(
            data.Mass.CombineLatest(data.BodyRadiusMultiplier,
                (mass, radius) => settings.BodyRadius.Evaluate(mass) * radius));
        GravityWellRadius = new ReadOnlyReactiveProperty<float>(
            data.Mass.CombineLatest(data.GravityRadiusMultiplier,
                (mass, radius) => settings.GravityRadius.Evaluate(mass) * radius));
        GravityWellDepth = new ReadOnlyReactiveProperty<float>(
            data.Mass.CombineLatest(data.GravityDepthMultiplier,
                (mass, depth) => settings.GravityDepth.Evaluate(mass) * depth));
    }
}

public class GasGiant : Planet
{
    public GasGiantData GasGiantData;
    public ReadOnlyReactiveProperty<float> GravityWavesDepth;
    public ReadOnlyReactiveProperty<float> GravityWavesRadius;
    public ReadOnlyReactiveProperty<float> GravityWavesSpeed;

    public GasGiant(PlanetSettings settings, GasGiantData data, Orbit orbit) : base(settings, data, orbit)
    {
        GasGiantData = data;
        GravityWavesDepth = new ReadOnlyReactiveProperty<float>(
            data.Mass.CombineLatest(data.WaveDepthMultiplier,
                (mass, depth) => settings.WaveDepth.Evaluate(mass) * depth));
        GravityWavesRadius = new ReadOnlyReactiveProperty<float>(
            data.Mass.CombineLatest(data.WaveRadiusMultiplier,
                (mass, radius) => settings.WaveRadius.Evaluate(mass) * radius));
        GravityWavesSpeed = new ReadOnlyReactiveProperty<float>(
            data.Mass.CombineLatest(data.WaveSpeedMultiplier,
                (mass, speed) => settings.WaveSpeed.Evaluate(mass) * speed));
    }
}

// public class Sun : GasGiant
// {
//     
// }

public class AsteroidBelt
{
    public AsteroidBeltData Data;
    public float4[] Transforms; // x, y, rotation, scale
    public float4[] PreviousTransforms; // x, y, rotation, scale
    public Dictionary<int, float> RespawnTimers = new Dictionary<int, float>();
    public Dictionary<int, float> Damage = new Dictionary<int, float>();
    public Dictionary<(Guid, int), float> MiningAccumulator = new Dictionary<(Guid, int), float>();

    public AsteroidBelt(AsteroidBeltData data)
    {
        Data = data;
        Transforms = new float4[data.Asteroids.Length];
        PreviousTransforms = new float4[data.Asteroids.Length];
    }
}

public class Orbit
{
    public OrbitData Data { get; }
    public float2 Velocity = float2.zero;
    public float2 Position = float2.zero;
    public float2 PreviousPosition = float2.zero;
    public ReadOnlyReactiveProperty<float> Period;

    public Orbit(PlanetSettings settings, OrbitData data)
    {
        Data = data;
        Period = new ReadOnlyReactiveProperty<float>(data.Distance.Select(f => settings.OrbitPeriod.Evaluate(f)));
    }
}