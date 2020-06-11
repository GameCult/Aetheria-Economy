using System;
using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;

[RethinkTable("Items"), Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class LoadoutData : DatabaseEntry, INamedEntry
{
    [InspectableField, JsonProperty("name"), Key(1)]
    public string Name;

    [InspectableDatabaseLink(typeof(HullData)), JsonProperty("hull"), Key(2)]  
    public Guid Hull;

    [JsonProperty("items"), Key(3)]  
    public List<Guid> Gear = new List<Guid>();

    [InspectableDatabaseLink(typeof(SimpleCommodityData)), JsonProperty("simpleCargo"), Key(4)]  
    public Dictionary<Guid, int> SimpleCargo = new Dictionary<Guid, int>();

    [InspectableDatabaseLink(typeof(CraftedItemData)), JsonProperty("compoundCargo"), Key(5)]  
    public Dictionary<Guid, int> CompoundCargo = new Dictionary<Guid, int>();

    [IgnoreMember] public string EntryName
    {
        get => Name;
        set => Name = value;
    }
}