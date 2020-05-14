using System;
using System.Collections.Generic;
using System.Linq;
using JsonKnownTypes;
using MessagePack;
using Newtonsoft.Json;
// TODO: USE THIS EVERYWHERE
using Unity.Mathematics;
using static Unity.Mathematics.math;
#if UNITY_EDITOR
using UnityEditor;
#endif

public interface INamedEntry
{
    string EntryName { get; set; }
}

[InspectableField, 
 MessagePackObject, 
 Union(0, typeof(SimpleCommodityData)), 
 Union(1, typeof(CompoundCommodityData)),
 Union(2, typeof(GearData)), 
 Union(3, typeof(HullData)), 
 Union(4, typeof(SimpleCommodity)), 
 Union(5, typeof(CompoundCommodity)), 
 Union(6, typeof(Gear)),
 Union(7, typeof(BlueprintData)),
 Union(8, typeof(GalaxyMapLayerData)),
 Union(9, typeof(GlobalData)), 
 Union(10, typeof(ZoneData)), 
 Union(11, typeof(Player)), 
 Union(12, typeof(Corporation)),
 Union(13, typeof(MegaCorporation)),
 Union(14, typeof(StationData)), 
 Union(15, typeof(OrbitData)), 
 Union(16, typeof(PlanetData)),
 Union(17, typeof(PersonalityAttribute)),
 Union(18, typeof(AgentTask)),
 Union(19, typeof(LoadoutData)),
 JsonObject(MemberSerialization.OptIn), JsonConverter(typeof(JsonKnownTypesConverter<DatabaseEntry>))]
//[Union(21, typeof(ContractData))]
//[Union(22, typeof(Station))]
public abstract class DatabaseEntry
{
    [JsonProperty("id"), Key(0)]
    public Guid ID = Guid.NewGuid();
    [IgnoreMember] public GameContext Context { get; set; }
}

[RethinkTable("Users"), MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class Player : DatabaseEntry, INamedEntry
{
    [JsonProperty("email"), Key(1)]
    public string Email;

    [JsonProperty("password"), Key(2)]
    public string Password;

    [JsonProperty("username"), Key(3)]
    public string Username;

    [JsonProperty("corporation"), Key(4)]
    public Guid Corporation;
    
    [IgnoreMember] public string EntryName
    {
        get => Username;
        set => Username = value;
    }
}

[RethinkTable("Galaxy"), MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class Corporation : DatabaseEntry, INamedEntry
{
    [JsonProperty("name"), Key(1)]
    public string Name;

    [JsonProperty("parent"), Key(2)]
    public Guid Parent;
    
    [JsonProperty("tasks"), Key(3)]
    public List<Guid> Tasks = new List<Guid>();
    
    [IgnoreMember] public string EntryName
    {
        get => Name;
        set => Name = value;
    }
}

[RethinkTable("Galaxy"), Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class MegaCorporation : DatabaseEntry, INamedEntry
{
    [InspectableField, JsonProperty("name"), Key(1)]
    public string Name;
    
    [InspectableText, JsonProperty("description"), Key(2)]
    public string Description;
    
    [InspectableTexture, JsonProperty("logo"), Key(3)]
    public string Logo;
    
    [InspectableDatabaseLink(typeof(PersonalityAttribute)), JsonProperty("personality"), Key(4)]  
    public Dictionary<Guid, float> Personality = new Dictionary<Guid, float>();
    
    [InspectableDatabaseLink(typeof(LoadoutData)), JsonProperty("initialFleet"), Key(5)]  
    public Dictionary<Guid, int> InitialFleet = new Dictionary<Guid, int>();
    
    [InspectableDatabaseLink(typeof(BlueprintData)), JsonProperty("initialTechs"), Key(6)]  
    public List<Guid> InitialTechnologies = new List<Guid>();
    
    [IgnoreMember] public string EntryName
    {
        get => Name;
        set => Name = value;
    }
}

[RethinkTable("Items"), Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class LoadoutData : DatabaseEntry, INamedEntry
{
    [InspectableField, JsonProperty("name"), Key(1)]
    public string Name;

    [InspectableDatabaseLink(typeof(HullData)), JsonProperty("hull"), Key(2)]  
    public Guid Hull;

    [JsonProperty("items"), Key(3)]  
    public List<Guid> Items = new List<Guid>();

    [IgnoreMember] public string EntryName
    {
        get => Name;
        set => Name = value;
    }
}