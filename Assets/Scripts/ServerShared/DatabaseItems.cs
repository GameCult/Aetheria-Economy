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
 Union(14, typeof(OrbitalEntity)), 
 Union(15, typeof(OrbitData)), 
 Union(16, typeof(PlanetData)),
 Union(17, typeof(PersonalityAttribute)),
 Union(18, typeof(AgentTask)),
 Union(19, typeof(LoadoutData)),
 Union(20, typeof(Ship)), 
 Union(21, typeof(Mining)), 
 Union(22, typeof(StationTowing)), 
 Union(23, typeof(Survey)), 
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

[RethinkTable("Items"), Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class LoadoutData : DatabaseEntry, INamedEntry
{
    [InspectableField, JsonProperty("name"), Key(1)]
    public string Name;

    [InspectableDatabaseLink(typeof(HullData)), JsonProperty("hull"), Key(2)]  
    public Guid Hull;

    [JsonProperty("items"), Key(3)]  
    public List<Guid> Items = new List<Guid>();

    [InspectableDatabaseLink(typeof(SimpleCommodityData)), JsonProperty("resourceRequirements"), Key(4)]  
    public Dictionary<Guid, int> SimpleCargo = new Dictionary<Guid, int>();

    [IgnoreMember] public string EntryName
    {
        get => Name;
        set => Name = value;
    }
}