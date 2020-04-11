using System;
using System.Collections.Generic;
using System.Linq;
using JsonKnownTypes;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;

[MessagePackObject, 
 Union(0, typeof(SimpleCommodityData)), 
 Union(1, typeof(CompoundCommodityData)),
 Union(2, typeof(GearData)), 
 Union(3, typeof(HullData)), 
 JsonObject(MemberSerialization.OptIn), JsonConverter(typeof(JsonKnownTypesConverter<ItemData>))]
public abstract class ItemData : DatabaseEntry, INamedEntry
{
    [InspectableField, JsonProperty("name"), Key(1)]  
    public string Name;
    
    [InspectableText, JsonProperty("description"), Key(2)]  
    public string Description;

    [InspectableField, JsonProperty("mass"), Key(3)]  
    public float Mass;

    [InspectableField, JsonProperty("size"), Key(4)]  
    public float Size;

    // Heat needed to change temperature of 1 gram by 1 degree
    [InspectableField, JsonProperty("specificHeat"), Key(5)]  
    public float SpecificHeat;
    
    [IgnoreMember] public string EntryName
    {
        get => Name;
        set => Name = value;
    }
}

[RethinkTable("Items"), Inspectable, MessagePackObject]
public class SimpleCommodityData : ItemData
{
    // Types of body where this resource can be found
    [InspectableField, JsonProperty("resourceBodyType"), Key(6)]  
    public BodyType ResourceBodyType;

    // Link to map(s) controlling density of resource, multiplied together when more than one
    [InspectableDatabaseLink(typeof(GalaxyMapLayerData)), JsonProperty("resourceDensity"), Key(7)]  
    public List<Guid> ResourceDensity = new List<Guid>();

    // Controls the bias power variable during resource placement
    [InspectableField, JsonProperty("resourceDensityBiasPower"), Key(8)]
    public float ResourceDensityBiasPower = 5;

    // Minimum amount of resources needed for presence to register
    [InspectableField, JsonProperty("resourceFloor"), Key(9)]
    public float ResourceFloor = 10f;
}

[MessagePackObject, 
 Union(0, typeof(CompoundCommodityData)), 
 Union(1, typeof(GearData)), 
 Union(2, typeof(HullData)),
 JsonObject(MemberSerialization.OptIn),
 JsonConverter(typeof(JsonKnownTypesConverter<CraftedItemData>))]
public abstract class CraftedItemData : ItemData
{
}

[RethinkTable("Items"), Inspectable, MessagePackObject]
public class CompoundCommodityData : CraftedItemData {}

[Union(0, typeof(GearData)), 
 Union(1, typeof(HullData)), 
 JsonObject(MemberSerialization.OptIn), JsonConverter(typeof(JsonKnownTypesConverter<EquippableItemData>))]
public abstract class EquippableItemData : CraftedItemData
{
    [InspectableField, JsonProperty("draw"), Key(6)]  
    public PerformanceStat EnergyDraw = new PerformanceStat();

    [InspectableAnimationCurve, JsonProperty("performanceCurve"), Key(7)]  
    public float4[] HeatPerformanceCurve;

    [TemperatureInspectable, JsonProperty("minTemp"), Key(8)]  
    public float MinimumTemperature;

    [TemperatureInspectable, JsonProperty("maxTemp"), Key(9)]  
    public float MaximumTemperature;

    [InspectableField, JsonProperty("durability"), Key(10)]  
    public float Durability;

    [InspectableField, JsonProperty("durabilityExponent"), Key(11)]  
    public PerformanceStat DurabilityExponent = new PerformanceStat();

    [InspectableField, JsonProperty("heatExponent"), Key(12)]  
    public PerformanceStat HeatExponent = new PerformanceStat();

    [InspectableField, JsonProperty("behaviors"), Key(13)]  
    public List<IBehaviorData> Behaviors = new List<IBehaviorData>();
    
    [IgnoreMember]
    public abstract HardpointType HardpointType { get; }
    
    [IgnoreMember] private const int STEPS = 64;

    [IgnoreMember] private float? _optimum;
    [IgnoreMember]
    public float OptimalTemperature
    {
        get
        {
            if (_optimum==null)
            {
                var samples = Enumerable.Range(0, STEPS).Select(i => (float) i / STEPS).ToArray();
                float max = 0;
                _optimum = 0;
                foreach (var f in samples)
                {
                    var p = HeatPerformanceCurve?.Evaluate(f)??0;
                    if (p > max)
                    {
                        max = p;
                        _optimum = f;
                    }
                }
            }

            return (float) (MinimumTemperature + _optimum * (MaximumTemperature - MinimumTemperature));
        }
    }
}

[RethinkTable("Items"), Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class GearData : EquippableItemData
{
    [InspectableField, JsonProperty("hardpointType"), Key(14)]  
    public HardpointType Hardpoint;

    [IgnoreMember] public override HardpointType HardpointType => Hardpoint;
}

[RethinkTable("Items"), Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class HullData : EquippableItemData
{
    [InspectableField, JsonProperty("hullCapacity"), Key(14)]  
    public PerformanceStat Capacity = new PerformanceStat();

    [InspectableField, JsonProperty("hardpoints"), Key(15)]  
    public List<HardpointData> Hardpoints = new List<HardpointData>();

    [InspectablePrefab, JsonProperty("prefab"), Key(16)]  
    public string Prefab;

    [InspectableField, JsonProperty("hullType"), Key(17)]
    public HullType HullType;

    [IgnoreMember] public override HardpointType HardpointType => HardpointType.Hull;
}

[RethinkTable("Items"), InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class HardpointData
{
    [InspectableField, JsonProperty("type"), Key(0)]  public HardpointType Type;
}

[MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class PerformanceStat
{
    [JsonProperty("min"), Key(0)]  public float Min;

    [JsonProperty("max"), Key(1)]  public float Max;

    [JsonProperty("durabilityDependent"), Key(2)] 
    public bool DurabilityDependent;

    [JsonProperty("heatDependent"), Key(3)] 
    public bool HeatDependent;

    [JsonProperty("qualityExponent"), Key(4)] 
    public float QualityExponent;
    
    //[JsonProperty("id"), Key(5)]  public Guid ID = Guid.NewGuid();

    // [JsonProperty("ingredient"), Key(5)]  public Guid? Ingredient;
    
    [IgnoreMember] private Dictionary<Entity,Dictionary<IBehavior,float>> _scaleModifiers;
    [IgnoreMember] private Dictionary<Entity,Dictionary<IBehavior,float>> _constantModifiers;

    [IgnoreMember]
    private Dictionary<Entity, Dictionary<IBehavior, float>> ScaleModifiers =>
        _scaleModifiers ?? (_scaleModifiers = new Dictionary<Entity, Dictionary<IBehavior, float>>());

    [IgnoreMember]
    private Dictionary<Entity, Dictionary<IBehavior, float>> ConstantModifiers =>
        _constantModifiers ?? (_constantModifiers = new Dictionary<Entity, Dictionary<IBehavior, float>>());

    public Dictionary<IBehavior, float> GetScaleModifiers(Entity entity)
    {
        if(!ScaleModifiers.ContainsKey(entity))
            ScaleModifiers[entity] = new Dictionary<IBehavior, float>();

        return ScaleModifiers[entity];
    }

    public Dictionary<IBehavior, float> GetConstantModifiers(Entity entity)
    {
        if(!ConstantModifiers.ContainsKey(entity))
            ConstantModifiers[entity] = new Dictionary<IBehavior, float>();

        return ConstantModifiers[entity];
    }
}