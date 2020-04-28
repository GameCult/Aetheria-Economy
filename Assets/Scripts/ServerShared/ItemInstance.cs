using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JsonKnownTypes;
//using JM.LinqFaster;
using MessagePack;
using MessagePack.Formatters;
using Newtonsoft.Json;
using Unity.Mathematics;
// TODO: USE THIS EVERYWHERE
using static Unity.Mathematics.math;

[Union(0, typeof(SimpleCommodity)), 
 Union(1, typeof(CompoundCommodity)), 
 Union(2, typeof(Gear)),
 JsonObject(MemberSerialization.OptIn), 
 JsonConverter(typeof(JsonKnownTypesConverter<ItemInstance>))]
public abstract class ItemInstance : DatabaseEntry
{
    [JsonProperty("data"), Key(1)]  public Guid Data;

    public float Mass => Context.GetMass(this);
    public float HeatCapacity => Context.GetHeatCapacity(this);
}

[Union(0, typeof(CompoundCommodity)), 
 Union(1, typeof(Gear)), 
 JsonObject(MemberSerialization.OptIn),
 JsonConverter(typeof(JsonKnownTypesConverter<CraftedItemInstance>))]
public abstract class CraftedItemInstance : ItemInstance
{
    [JsonProperty("quality"), Key(2)]  public float Quality;

    [JsonProperty("ingredients"), Key(3)]  public List<Guid> Ingredients = new List<Guid>();
    
    [JsonProperty("blueprint"), Key(4)]  public BlueprintData Blueprint;
}

[MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class CompoundCommodity : CraftedItemInstance {}

[MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class SimpleCommodity : ItemInstance
{
    [JsonProperty("quantity"), Key(2)]  public int Quantity;

    public SimpleCommodityData ItemData => Context.GetData(this);
}

[MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class Gear : CraftedItemInstance
{
    [JsonProperty("durability"), Key(5)]  public float Durability;

    public EquippableItemData ItemData => Context.GetData(this);
}

[MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class Hardpoint
{
    [JsonProperty("item"), Key(0)]  public Gear Item;
    
    [IgnoreMember] public HardpointData HardpointData;
}