using System;
using System.Collections;
using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using static Unity.Mathematics.noise;
using static NoiseFbm;

[Inspectable, Serializable, RethinkTable("Galaxy"), MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class GlobalData : DatabaseEntry
{
    [InspectableField, JsonProperty("targetPersistenceDuration"), Key(1)]  
    public float TargetPersistenceDuration = 3;

    [InspectableField, JsonProperty("heatRadiationPower"), Key(2)]  
    public float HeatRadiationPower = 1;

    [InspectableField, JsonProperty("heatRadiationMultiplier"), Key(3)]  
    public float HeatRadiationMultiplier = 1;

    [InspectableField, JsonProperty("galaxyArms"), Key(4)]  
    public int Arms = 4;

    [InspectableField, JsonProperty("galaxyTwist"), Key(5)]  
    public float Twist = 10;

    [InspectableField, JsonProperty("galaxyTwistPower"), Key(6)]  
    public float TwistPower = 2;

    [InspectableField, JsonProperty("radiusPower"), Key(7)]  
    public float RadiusPower = 1.75f;

    [InspectableField, JsonProperty("massFloor"), Key(8)]  
    public float MassFloor = 1;

    [InspectableField, JsonProperty("sunMass"), Key(9)]  
    public float SunMass = 10000;

    [InspectableField, JsonProperty("gasGiantMass"), Key(10)]  
    public float GasGiantMass = 2000;

    [InspectableField, JsonProperty("planetMass"), Key(11)]  
    public float PlanetMass = 100f;

    [InspectableField, JsonProperty("satelliteCreationMassFloor"), Key(12)]  
    public float SatelliteCreationMassFloor = 100;

    [InspectableField, JsonProperty("satelliteCreationProbability"), Key(13)]  
    public float SatelliteCreationProbability = .25f;

    [InspectableField, JsonProperty("binaryCreationProbability"), Key(14)]  
    public float BinaryCreationProbability = .25f;

    [InspectableField, JsonProperty("rosetteProbability"), Key(15)]  
    public float RosetteProbability = .25f;
    
    // [InspectableField] [JsonProperty("mapLayers")] [Key(16)]
    // public Dictionary<string, Guid> MapLayers = new Dictionary<string, Guid>();

    [InspectableField, JsonProperty("zoneRadiusMin"), Key(16)]  
    public float MinimumZoneRadius = 500;

    [InspectableField, JsonProperty("zoneRadiusMax"), Key(17)]  
    public float MaximumZoneRadius = 2000;

    [InspectableField, JsonProperty("zoneMassMin"), Key(18)]  
    public float MinimumZoneMass = 10000;

    [InspectableField, JsonProperty("zoneMassMax"), Key(19)]  
    public float MaximumZoneMass = 100000;

    [InspectableField, JsonProperty("orbitPeriodExponent"), Key(20)]  
    public float OrbitPeriodExponent = 1.5f;

    [InspectableField, JsonProperty("orbitPeriodMultiplier"), Key(21)]  
    public float OrbitPeriodMultiplier = 1f;

    [InspectableField, JsonProperty("gravityRadiusExponent"), Key(22)]  
    public float GravityRadiusExponent = 1.5f;

    [InspectableField, JsonProperty("gravityRadiusMultiplier"), Key(23)]  
    public float GravityRadiusMultiplier = 1f;

    [InspectableField, JsonProperty("beltProbability"), Key(24)]  
    public float BeltProbability = .05f;

    [InspectableField, JsonProperty("beltMassCeiling"), Key(25)]  
    public float BeltMassCeiling = 500f;

    [InspectableField, JsonProperty("beltMassRatio"), Key(26)]  
    public float BeltMassRatio = 100f;

    [InspectableField, JsonProperty("beltMassExponent"), Key(27)]  
    public float BeltMassExponent = 100f;

    [InspectableField, JsonProperty("asteroidSizeMin"), Key(28)]
    public float AsteroidSizeMin = 2f;

    [InspectableField, JsonProperty("asteroidSizeMax"), Key(29)]
    public float AsteroidSizeMax = 10f;

    [InspectableField, JsonProperty("asteroidSizeExponent"), Key(30)]
    public float AsteroidSizeExponent = 2f;

    [InspectableField, JsonProperty("asteroidRotationSpeed"), Key(31)]
    public float AsteroidRotationSpeed = 2f;

    [InspectableField, JsonProperty("dockingDistance"), Key(32)]
    public float DockingDistance = 25;

    [InspectableField, JsonProperty("warpDistance"), Key(33)]
    public float WarpDistance = 25;

    [InspectableField, JsonProperty("orbitSpeedMultiplier"), Key(34)]
    public float OrbitSpeedMultiplier = .25f;
    
    public float PlanetRadius(float mass)
    {
        return pow(mass, 1 / RadiusPower);
    }
}

[Inspectable, Serializable, RethinkTable("Galaxy"), MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class GalaxyMapLayerData : DatabaseEntry, INamedEntry
{
    [InspectableField, JsonProperty("coreBoost"), Key(1)]  
    public float CoreBoost = 1.05f;

    [InspectableField, JsonProperty("coreBoostOffset"), Key(2)]  
    public float CoreBoostOffset = .1f;

    [InspectableField, JsonProperty("coreBoostPower"), Key(3)]  
    public float CoreBoostPower = 2.25f;

    [InspectableField, JsonProperty("spokeScale"), Key(4)]  
    public float SpokeScale = 1;

    [InspectableField, JsonProperty("spokeOffset"), Key(5)]  
    public float SpokeOffset = 0;

    [InspectableField, JsonProperty("edgeReduction"), Key(6)]  
    public float EdgeReduction = 3;

    [InspectableField, JsonProperty("noiseOffset"), Key(7)]  
    public float NoiseOffset = 0;

    [InspectableField, JsonProperty("noiseAmplitude"), Key(8)]  
    public float NoiseAmplitude = 1.5f;

    [InspectableField, JsonProperty("noiseGain"), Key(9)]  
    public float NoiseGain = .7f;

    [InspectableField, JsonProperty("noiseLacunarity"), Key(10)]  
    public float NoiseLacunarity = 2;

    [InspectableField, JsonProperty("noiseOctaves"), Key(11)]  
    public int NoiseOctaves = 7;

    [InspectableField, JsonProperty("noiseFrequency"), Key(12)]  
    public float NoiseFrequency = 1;

    [InspectableField, JsonProperty("noisePosition"), Key(13)]  
    public float NoisePosition = 1337;

    [InspectableField, JsonProperty("name"), Key(14)]  
    public string Name;

    public float Evaluate(float2 uv, GlobalData data)
    {
        float2 offset = -float2(.5f, .5f)+uv;
        float circle = (.5f-length(offset))*2;
        float angle = pow(length(offset)*2,data.TwistPower) * data.Twist;
        float2 t = float2(offset.x*cos(angle) - offset.y*sin(angle), offset.x*sin(angle) + offset.y*cos(angle));
        float atan = atan2(t.y,t.x);
        float spokes = (sin(atan*data.Arms) + SpokeOffset) * SpokeScale;
        float noise = fBm(uv + float2(NoisePosition), NoiseOctaves, NoiseFrequency, NoiseOffset, NoiseAmplitude, NoiseLacunarity, NoiseGain);
        float shape = lerp(spokes - EdgeReduction * length(offset), 1, pow(circle, CoreBoostPower) * CoreBoost) + CoreBoostOffset;
        float gal = max(shape - noise * saturate(circle), 0);

        return gal;
    }

    [IgnoreMember] public string EntryName
    {
        get => Name;
        set => Name = value;
    }
}

public static class NoiseFbm
{
    public static float fBm(float2 p, int octaves, float frequency, float offset, float amplitude, float lacunarity, float gain)
    {
        float freq = frequency, amp = .5f;
        float sum = 0;	
        for(int i = 0; i < octaves; i++) 
        {
            sum += snoise(p * freq) * amp;
            freq *= lacunarity;
            amp *= gain;
        }
        return (sum + offset)*amplitude;
    }
}