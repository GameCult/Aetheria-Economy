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
 Union(6, typeof(SimpleCommodity)), 
 Union(7, typeof(CompoundCommodity)), 
 Union(8, typeof(Gear)),
 Union(9, typeof(GlobalData)), 
 Union(10, typeof(ZoneData)), 
 Union(11, typeof(Player)), 
 Union(12, typeof(Corporation)),
 Union(13, typeof(Ship)), 
 Union(14, typeof(Station)), 
 Union(15, typeof(OrbitData)), 
 Union(16, typeof(PlanetData)),
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
    
    [IgnoreMember] public string EntryName
    {
        get => Name;
        set => Name = value;
    }
}