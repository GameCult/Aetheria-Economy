using System;
using System.Collections;
using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

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
    public List<PlanetData> Planets = new List<PlanetData>();
    
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
 Union(1, typeof(AsteroidBeltData)),
 Union(2, typeof(GasGiantData)),
 Union(3, typeof(SunData))]
public class PlanetData : DatabaseEntry, INamedEntry
{
    [JsonProperty("name"), Key(1)]
    public string Name;

    [JsonProperty("orbit"), Key(2)]
    public Guid Orbit;

    [JsonProperty("mass"), Key(3)]
    public float Mass;

    [JsonProperty("resources"), Key(4)]
    public Dictionary<Guid, float> Resources = new Dictionary<Guid, float>();

    [JsonProperty("radiusMul")] [Key(5)]
    public float GravityRadiusMultiplier = 1;

    [JsonProperty("depthMul")] [Key(6)]
    public float GravityDepthMultiplier = 1;
    
    [IgnoreMember] public string EntryName
    {
        get => Name;
        set => Name = value;
    }
}

public class AsteroidBeltData : PlanetData
{
    [JsonProperty("asteroids"), Key(7)]
    public Asteroid[] Asteroids;
}

public class GasGiantData : PlanetData
{
    [JsonProperty("firstOffsetDomainRotationSpeed"), Key(7)]
    public float FirstOffsetDomainRotationSpeed;
    
    [JsonProperty("firstOffsetRotationSpeed"), Key(8)]
    public float FirstOffsetRotationSpeed;
    
    [JsonProperty("secondOffsetDomainRotationSpeed"), Key(9)]
    public float SecondOffsetDomainRotationSpeed;
    
    [JsonProperty("secondOffsetRotationSpeed"), Key(10)]
    public float SecondOffsetRotationSpeed;
    
    [JsonProperty("albedoRotationSpeed"), Key(11)]
    public float AlbedoRotationSpeed;
    
    [JsonProperty("materialOverrides"), Key(12)]
    public List<string> MaterialOverrides = new List<string>();
    
    [JsonProperty("colors"), Key(13)]
    public Gradient Colors;
}

public class SunData : GasGiantData
{
    [JsonProperty("lightColor"), Key(17)]
    public Color LightColor;
    
    [JsonProperty("fogTintColor"), Key(18)]
    public Color FogTintColor;
}

[MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class Asteroid
{
    public float Distance;
    public float Phase;
    public float Size;
    public float RotationSpeed;
}

[MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class OrbitData : DatabaseEntry
{
    [JsonProperty("parent"), Key(1)]
    public Guid Parent;

    [JsonProperty("distance"), Key(2)]
    public float Distance;

    [JsonProperty("phase"), Key(3)]
    public float Phase;

    [JsonProperty("period"), Key(4)]
    public float Period;

    public static float2 Evaluate(float phase)
    {
        phase *= PI * 2;
        return new float2(cos(phase), sin(phase));
    }
}