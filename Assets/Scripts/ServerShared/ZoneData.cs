using System;
using System.Collections;
using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;
using UniRx;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using float3 = Unity.Mathematics.float3;

[RethinkTable("Galaxy"), MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class ZoneData : DatabaseEntry, INamedEntry
{
    [JsonProperty("name"), Key(1)]
    public string Name;

    [JsonProperty("position"), Key(2)]
    public float2 Position = 500;

    [JsonProperty("radius"), Key(3)]
    public float Radius = 2000;

    [JsonProperty("mass"), Key(4)]
    public float Mass = 10000;

    // [JsonProperty("visited"), Key(5)]
    // public bool Visited;

    [JsonProperty("wormholes"), Key(5)]
    public List<Guid> Wormholes = new List<Guid>();

    // [JsonProperty("planets"), Key(7)]
    // public Guid[] Planets;// = new List<Guid>();
    //
    // [JsonProperty("orbits"), Key(8)]
    // public List<Guid> Orbits = new List<Guid>();
    
    [IgnoreMember] public string EntryName
    {
        get => Name;
        set => Name = value;
    }
}

[MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class ZonePack : INamedEntry
{
    [JsonProperty("data"), Key(0)]
    public ZoneData Data;
    
    [JsonProperty("planets"), Key(1)]
    public List<BodyData> Planets = new List<BodyData>();
    
    [JsonProperty("orbits"), Key(2)]
    public List<OrbitData> Orbits = new List<OrbitData>();
    
    [JsonProperty("entities"), Key(3)]
    public List<Entity> Entities = new List<Entity>();
    
    [IgnoreMember] public string EntryName
    {
        get => Data.Name;
        set => Data.Name = value;
    }
}

// [RethinkTable("Galaxy"), MessagePackObject, JsonObject(MemberSerialization.OptIn)]
// public class StationData : DatabaseEntry, INamedEntry
// {
//     [JsonProperty("name"), Key(1)]
//     public string Name;
//     
//     // Can be Planet or Orbit
//     [JsonProperty("parent"), Key(2)]
//     public Guid Parent;
//
//     [JsonProperty("owner"), Key(3)]
//     public Guid Owner;
//
//     [JsonProperty("inventory"), Key(4)]
//     public List<ItemInstance> Inventory = new List<ItemInstance>();
//
//     [JsonProperty("buying"), Key(5)]
//     public Dictionary<Guid, float> BuyPrices = new Dictionary<Guid, float>();
//
//     [JsonProperty("selling"), Key(6)]
//     public Dictionary<Guid, float> SellPrices = new Dictionary<Guid, float>();
//
//     [JsonProperty("zone"), Key(7)]
//     public Guid Zone;
//     
//     [IgnoreMember] public string EntryName
//     {
//         get => Name;
//         set => Name = value;
//     }
// }

[MessagePackObject, 
 JsonObject(MemberSerialization.OptIn),
 Union(0, typeof(PlanetData)),
 Union(1, typeof(AsteroidBeltData)),
 Union(2, typeof(GasGiantData)),
 Union(3, typeof(SunData))]
public abstract class BodyData : DatabaseEntry, INamedEntry
{
    [JsonProperty("name"), Key(1)]
    public ReactiveProperty<string> Name = new ReactiveProperty<string>("");

    [JsonProperty("orbit"), Key(2)]
    public Guid Orbit;

    [JsonProperty("mass"), Key(3)]
    public ReactiveProperty<float> Mass = new ReactiveProperty<float>(0);

    [JsonProperty("resources"), Key(4)]
    public Dictionary<Guid, float> Resources = new Dictionary<Guid, float>();

    [JsonProperty("bodyRadiusMul")] [Key(5)]
    public ReactiveProperty<float> BodyRadiusMultiplier = new ReactiveProperty<float>(1);

    [JsonProperty("gravRadiusMul")] [Key(6)]
    public ReactiveProperty<float> GravityRadiusMultiplier = new ReactiveProperty<float>(1);

    [JsonProperty("depthMul")] [Key(7)]
    public ReactiveProperty<float> GravityDepthMultiplier = new ReactiveProperty<float>(1);

    [JsonProperty("depthExp")] [Key(8)]
    public ReactiveProperty<float> GravityDepthExponent = new ReactiveProperty<float>(8);
    
    [IgnoreMember] public string EntryName
    {
        get => Name.Value;
        set => Name.Value = value;
    }
}

[MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class PlanetData : BodyData
{
    
}

[MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class AsteroidBeltData : BodyData
{
    [JsonProperty("asteroids"), Key(9)]
    public Asteroid[] Asteroids;
}

[MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class GasGiantData : BodyData
{
    [JsonProperty("firstOffsetDomainRotationSpeed"), Key(9)]
    public ReactiveProperty<float> FirstOffsetDomainRotationSpeed = new ReactiveProperty<float>(1);
    
    [JsonProperty("firstOffsetRotationSpeed"), Key(10)]
    public ReactiveProperty<float> FirstOffsetRotationSpeed = new ReactiveProperty<float>(1);
    
    [JsonProperty("secondOffsetDomainRotationSpeed"), Key(11)]
    public ReactiveProperty<float> SecondOffsetDomainRotationSpeed = new ReactiveProperty<float>(1);
    
    [JsonProperty("secondOffsetRotationSpeed"), Key(12)]
    public ReactiveProperty<float> SecondOffsetRotationSpeed = new ReactiveProperty<float>(1);
    
    [JsonProperty("albedoRotationSpeed"), Key(13)]
    public ReactiveProperty<float> AlbedoRotationSpeed = new ReactiveProperty<float>(1);

    [JsonProperty("gravRadiusMul")] [Key(14)]
    public ReactiveProperty<float> WaveRadiusMultiplier = new ReactiveProperty<float>(1);

    [JsonProperty("depthMul")] [Key(15)]
    public ReactiveProperty<float> WaveDepthMultiplier = new ReactiveProperty<float>(1);

    [JsonProperty("depthExp")] [Key(16)]
    public ReactiveProperty<float> WaveDepthExponent = new ReactiveProperty<float>(8);

    [JsonProperty("depthExp")] [Key(17)]
    public ReactiveProperty<float> WaveSpeedMultiplier = new ReactiveProperty<float>(8);
    
    [JsonProperty("materialOverrides"), Key(18)]
    public List<string> MaterialOverrides = new List<string>();
    
    [JsonProperty("colors"), Key(19)]
    public ReactiveProperty<float4[]> Colors = new ReactiveProperty<float4[]>();
}

public class SunData : GasGiantData
{
    [JsonProperty("lightColor"), Key(20)]
    public ReactiveProperty<float3> LightColor = new ReactiveProperty<float3>(float3.zero);
    
    [JsonProperty("fogTintColor"), Key(21)]
    public ReactiveProperty<float3> FogTintColor = new ReactiveProperty<float3>(float3.zero);
}

[MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class Asteroid
{
    [JsonProperty("distance"), Key(0)]
    public float Distance;
    
    [JsonProperty("phase"), Key(1)]
    public float Phase;
    
    [JsonProperty("size"), Key(2)]
    public float Size;
    
    [JsonProperty("rotationSpeed"), Key(3)]
    public float RotationSpeed;
}

[MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class OrbitData : DatabaseEntry
{
    [JsonProperty("parent"), Key(1)]
    public Guid Parent;

    [JsonProperty("distance"), Key(2)]
    public ReactiveProperty<float> Distance;

    [JsonProperty("phase"), Key(3)]
    public float Phase;
    
    // [JsonProperty("period"), Key(4)]
    // public float Period;

    public static float2 Evaluate(float phase)
    {
        phase *= PI * 2;
        return new float2(cos(phase), sin(phase));
    }
}