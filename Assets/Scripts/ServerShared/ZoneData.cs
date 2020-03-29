using System;
using System.Collections;
using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[RethinkTable("Galaxy")]
[MessagePackObject]
[JsonObject(MemberSerialization.OptIn)]
public class ZoneData : DatabaseEntry, INamedEntry
{
    [JsonProperty("name")] [Key(1)]
    public string Name;
    
    [JsonProperty("position")] [Key(2)]
    public float2 Position = 500;

    [JsonProperty("wormholes")] [Key(3)]
    public List<Guid> Wormholes = new List<Guid>();
    
    [JsonProperty("stations")] [Key(4)]
    public List<Guid> Stations = new List<Guid>();
    
    [JsonProperty("planets")] [Key(5)]
    public List<Guid> Planets = new List<Guid>();
    
    [JsonProperty("orbits")] [Key(6)]
    public List<Guid> Orbits = new List<Guid>();
    
    [JsonProperty("radius")] [Key(7)]
    public float Radius = 2000;

    [JsonProperty("visited")] [Key(8)]
    public bool Visited;
    
    [IgnoreMember] public string EntryName
    {
        get => Name;
        set => Name = value;
    }
}

[RethinkTable("Galaxy")]
[MessagePackObject]
[JsonObject(MemberSerialization.OptIn)]
public class Station : DatabaseEntry, INamedEntry
{
    [JsonProperty("name")] [Key(1)]
    public string Name;
    
    // Can be Planet or Orbit
    [JsonProperty("parent")] [Key(2)]
    public Guid Parent;

    [JsonProperty("owner")] [Key(3)]
    public Guid Owner;

    [JsonProperty("inventory")] [Key(4)]
    public List<ItemInstance> Inventory = new List<ItemInstance>();

    [JsonProperty("buying")] [Key(5)]
    public Dictionary<Guid, float> BuyPrices = new Dictionary<Guid, float>();

    [JsonProperty("selling")] [Key(6)]
    public Dictionary<Guid, float> SellPrices = new Dictionary<Guid, float>();
    
    [JsonProperty("zone")] [Key(7)]
    public Guid Zone;
    
    [IgnoreMember] public string EntryName
    {
        get => Name;
        set => Name = value;
    }
}

[RethinkTable("Galaxy")]
[MessagePackObject]
[JsonObject(MemberSerialization.OptIn)]
public class PlanetData : DatabaseEntry, INamedEntry
{
    [JsonProperty("name")] [Key(1)]
    public string Name;
    
    [JsonProperty("orbit")] [Key(2)]
    public Guid Orbit;
    
    [JsonProperty("mass")] [Key(3)]
    public float Mass;
    
    [JsonProperty("zone")] [Key(4)]
    public Guid Zone;
    
    [JsonProperty("radius")] [Key(5)]
    public float GravityRadius;
    
    [IgnoreMember] public string EntryName
    {
        get => Name;
        set => Name = value;
    }
}

[RethinkTable("Galaxy")]
[MessagePackObject]
[JsonObject(MemberSerialization.OptIn)]
public class OrbitData : DatabaseEntry
{
    [JsonProperty("parent")] [Key(1)]
    public Guid Parent;
    
    [JsonProperty("distance")] [Key(2)]
    public float Distance;
    
    [JsonProperty("phase")] [Key(3)]
    public float Phase;
    
    [JsonProperty("period")] [Key(4)]
    public float Period;
    
    [JsonProperty("zone")] [Key(5)]
    public Guid Zone;

    public static float2 Evaluate(float phase)
    {
        phase *= (PI * 2);
        return new float2(sin(phase), cos(phase));
    }
}