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
    public float ThermalMass => Context.GetThermalMass(this);
    public float Size => Context.GetSize(this);
}

[Union(0, typeof(CompoundCommodity)), 
 Union(1, typeof(Gear)), 
 JsonObject(MemberSerialization.OptIn),
 JsonConverter(typeof(JsonKnownTypesConverter<CraftedItemInstance>))]
public abstract class CraftedItemInstance : ItemInstance
{
    [JsonProperty("quality"), Key(2)]  public float Quality;

    [JsonProperty("ingredients"), Key(3)]  public List<Guid> Ingredients = new List<Guid>();
    
    [JsonProperty("blueprint"), Key(4)]  public Guid Blueprint;
    
    [JsonProperty("name"), Key(5)]  public string Name;
    
    [JsonProperty("sourceEntity"), Key(6)]  public Guid SourceEntity;
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
    [JsonProperty("durability"), Key(7)]  public float Durability;

    public EquippableItemData ItemData => Context.GetData(this);
}