using System;
using System.Collections;
using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using static Unity.Mathematics.noise;
using static NoiseFbm;

[Serializable]
[RethinkTable("Galaxy")]
[MessagePackObject]
[JsonObject(MemberSerialization.OptIn)]
public class GlobalData : DatabaseEntry
{
    [InspectableField] [JsonProperty("targetPersistenceDuration")] [Key(1)]
    public float TargetPersistenceDuration = 3;
    
    [InspectableField] [JsonProperty("heatRadiationPower")] [Key(2)]
    public float HeatRadiationPower = 1;
    
    [InspectableField] [JsonProperty("heatRadiationMultiplier")] [Key(3)]
    public float HeatRadiationMultiplier = 1;

    [InspectableField] [JsonProperty("radiusPower")] [Key(4)]
    public float RadiusPower = 1.75f;

    [InspectableField] [JsonProperty("massFloor")] [Key(5)]
    public float MassFloor = 1;

    [InspectableField] [JsonProperty("sunMass")] [Key(6)]
    public float SunMass = 10000;

    [InspectableField] [JsonProperty("gasGiantMass")] [Key(7)]
    public float GasGiantMass = 2000;
    
    [InspectableField] [JsonProperty("dockingDistance")] [Key(8)]
    public float DockingDistance = 10;
    
    [InspectableField] [JsonProperty("satelliteCreationMassFloor")] [Key(9)]
    public float SatelliteCreationMassFloor = 100;
    
    [InspectableField] [JsonProperty("satelliteCreationProbability")] [Key(10)]
    public float SatelliteCreationProbability = .25f;
    
    [InspectableField] [JsonProperty("binaryCreationProbability")] [Key(11)]
    public float BinaryCreationProbability = .25f;
    
    [InspectableField] [JsonProperty("rosetteProbability")] [Key(12)]
    public float RosetteProbability = .25f;
    
    [InspectableField] [JsonProperty("galaxyArms")] [Key(13)]
    public int Arms = 4;
    
    [InspectableField] [JsonProperty("galaxyTwist")] [Key(14)]
    public float Twist = 10;
    
    [InspectableField] [JsonProperty("galaxyTwistPower")] [Key(15)]
    public float TwistPower = 2;
    
    [InspectableField] [JsonProperty("mapLayers")] [Key(16)]
    public Dictionary<string, Guid> MapLayers = new Dictionary<string, Guid>();
}

[Serializable]
[RethinkTable("Galaxy")]
[MessagePackObject]
[JsonObject(MemberSerialization.OptIn)]
public class GalaxyMapLayerData : DatabaseEntry
{
    [InspectableField] [JsonProperty("coreBoost")] [Key(1)]
    public float CoreBoost = 1.05f;
    
    [InspectableField] [JsonProperty("coreBoostOffset")] [Key(2)]
    public float CoreBoostOffset = .1f;
    
    [InspectableField] [JsonProperty("coreBoostPower")] [Key(3)]
    public float CoreBoostPower = 2.25f;
    
    [InspectableField] [JsonProperty("spokeScale")] [Key(4)]
    public float SpokeScale = 1;
    
    [InspectableField] [JsonProperty("spokeOffset")] [Key(5)]
    public float SpokeOffset = 0;
    
    [InspectableField] [JsonProperty("edgeReduction")] [Key(6)]
    public float EdgeReduction = 3;
    
    [InspectableField] [JsonProperty("noiseOffset")] [Key(7)]
    public float NoiseOffset = 0;
    
    [InspectableField] [JsonProperty("noiseAmplitude")] [Key(8)]
    public float NoiseAmplitude = 1.5f;
    
    [InspectableField] [JsonProperty("noiseGain")] [Key(9)]
    public float NoiseGain = .7f;
    
    [InspectableField] [JsonProperty("noiseLacunarity")] [Key(10)]
    public float NoiseLacunarity = 2;
    
    [InspectableField] [JsonProperty("noiseOctaves")] [Key(11)]
    public int NoiseOctaves = 7;
    
    [InspectableField] [JsonProperty("noiseFrequency")] [Key(12)]
    public float NoiseFrequency = 1;
    
    [InspectableField] [JsonProperty("noisePosition")] [Key(13)]
    public float NoisePosition = 1337;

    public float Evaluate(float2 uv, GlobalData data)
    {
        float2 offset = -float2(.5f, .5f)+uv;
        float circle = (.5f-length(offset))*2;
        float angle = pow(length(offset)*2,data.TwistPower) * data.Twist;
        float2 t = float2(offset.x*cos(angle) - offset.y*sin(angle), offset.x*sin(angle) + offset.y*cos(angle));
        float atan = atan2(t.y,t.x);
        float spokes = (sin(atan*data.Arms) + SpokeOffset) * SpokeScale;
        float noise = fBm(uv + float2(NoisePosition), NoiseOctaves, NoiseFrequency, NoiseOffset, NoiseAmplitude, NoiseLacunarity, NoiseGain);
        float shape = lerp(spokes - EdgeReduction * length(offset), 1, pow(circle + CoreBoostOffset, CoreBoostPower) * CoreBoost);
        float gal = max(shape - noise * saturate(circle), 0);

        return gal;
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