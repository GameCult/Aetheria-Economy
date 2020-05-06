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
 Union(13, typeof(ShipData)), 
 Union(14, typeof(StationData)), 
 Union(15, typeof(OrbitData)), 
 Union(16, typeof(PlanetData)),
 Union(17, typeof(PersonalityAttribute)),
 // Union(17, typeof(ShipData)),
 JsonObject(MemberSerialization.OptIn), JsonConverter(typeof(JsonKnownTypesConverter<DatabaseEntry>))]
//[Union(21, typeof(ContractData))]
//[Union(22, typeof(Station))]
public abstract class DatabaseEntry
{
    [JsonProperty("id"), Key(0)]  public Guid ID = Guid.NewGuid();
    [IgnoreMember] public GameContext Context { get; set; }
}

[RethinkTable("Users"), MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class Player : DatabaseEntry, INamedEntry
{
    [JsonProperty("email"), Key(1)]  public string Email;

    [JsonProperty("password"), Key(2)]  public string Password;

    [JsonProperty("username"), Key(3)]  public string Username;

    [JsonProperty("corporation"), Key(4)]  public Guid Corporation;
    
    [IgnoreMember] public string EntryName
    {
        get => Username;
        set => Username = value;
    }
}

[RethinkTable("Galaxy"), MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class Corporation : DatabaseEntry, INamedEntry
{
    [JsonProperty("name"), Key(1)]  public string Name;
    [JsonProperty("tasks"), Key(2)]  public List<AgentTask> Tasks = new List<AgentTask>();
    
    [IgnoreMember] public string EntryName
    {
        get => Name;
        set => Name = value;
    }
}

[RethinkTable("Galaxy"), MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class ShipData : DatabaseEntry
{
    
}