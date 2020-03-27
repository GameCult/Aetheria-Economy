using System;
using System.Collections.Generic;
using System.Linq;
using JsonKnownTypes;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;

[MessagePackObject]
[Union(0, typeof(SimpleCommodityData))]
[Union(1, typeof(CompoundCommodityData))]
[Union(2, typeof(GearData))]
[Union(3, typeof(HullData))]
[Union(4, typeof(ShieldData))]
[Union(5, typeof(ThrusterData))]
[JsonObject(MemberSerialization.OptIn)]
[JsonConverter(typeof(JsonKnownTypesConverter<ItemData>))]
public abstract class ItemData : DatabaseEntry, INamedEntry
{
    [InspectableField] [JsonProperty("name")] [Key(1)]
    public string Name;

    [InspectableField] [JsonProperty("mass")] [Key(2)]
    public float Mass;

    [InspectableField] [JsonProperty("size")] [Key(3)]
    public float Size;

    [InspectableField] [JsonProperty("specificHeat")] [Key(4)]
    public float SpecificHeat;
    
    [IgnoreMember] public string EntryName
    {
        get => Name;
        set => Name = value;
    }
}

[RethinkTable("Items")]
[Inspectable]
[MessagePackObject]
public class SimpleCommodityData : ItemData {}

[MessagePackObject]
[Union(0, typeof(CompoundCommodityData))]
[Union(1, typeof(GearData))]
[Union(2, typeof(HullData))]
[Union(3, typeof(ShieldData))]
[Union(4, typeof(ThrusterData))]
[JsonObject(MemberSerialization.OptIn)]
[JsonConverter(typeof(JsonKnownTypesConverter<CraftedItemData>))]
public abstract class CraftedItemData : ItemData
{
    [InspectableField] [JsonProperty("ingredients")] [Key(5)]
    public Dictionary<Guid, int> Ingredients = new Dictionary<Guid, int>();
}

[RethinkTable("Items")]
[Inspectable]
[MessagePackObject]
public class CompoundCommodityData : CraftedItemData {}

[Union(0, typeof(GearData))]
[Union(1, typeof(HullData))]
[Union(2, typeof(ShieldData))]
[Union(3, typeof(ThrusterData))]
[JsonObject(MemberSerialization.OptIn)]
[JsonConverter(typeof(JsonKnownTypesConverter<EquippableItemData>))]
public abstract class EquippableItemData : CraftedItemData
{

    [InspectableField] [JsonProperty("draw")] [Key(6)]
    public PerformanceStat EnergyDraw;
    
    [InspectableAnimationCurve] [JsonProperty("performanceCurve")] [Key(7)]
    public float4[] HeatPerformanceCurve;
    
    [TemperatureInspectable] [JsonProperty("minTemp")] [Key(8)]
    public float MinimumTemperature;
    
    [TemperatureInspectable] [JsonProperty("maxTemp")] [Key(9)]
    public float MaximumTemperature;
    
    [InspectableField] [JsonProperty("durability")] [Key(10)]
    public float Durability;

    [InspectableField] [JsonProperty("durabilityExponent")] [Key(11)]
    public PerformanceStat DurabilityExponent;

    [InspectableField] [JsonProperty("heatExponent")] [Key(12)]
    public PerformanceStat HeatExponent;
    
    [InspectableField] [JsonProperty("behaviors")] [Key(13)]
    public List<IItemBehaviorData> Behaviors = new List<IItemBehaviorData>();
    
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

[RethinkTable("Items")]
[Inspectable]
[MessagePackObject]
[JsonObject(MemberSerialization.OptIn)]
public class GearData : EquippableItemData
{
    [InspectableField] [JsonProperty("hardpointType")] [Key(14)] public HardpointType Hardpoint;

    [IgnoreMember] public override HardpointType HardpointType => Hardpoint;
}

[RethinkTable("Items")]
[Inspectable]
[MessagePackObject]
[JsonObject(MemberSerialization.OptIn)]
public class HullData : EquippableItemData
{
    [InspectableField] [JsonProperty("hullCapacity")] [Key(14)]
    public PerformanceStat Capacity;

    [InspectableField] [JsonProperty("traction")] [Key(15)]
    public PerformanceStat Traction;

    [InspectableField] [JsonProperty("topSpeed")] [Key(16)]
    public PerformanceStat TopSpeed;

    [InspectableField] [JsonProperty("emissivity")] [Key(17)]
    public PerformanceStat Emissivity;

    [InspectableField] [JsonProperty("crossSection")] [Key(18)]
    public PerformanceStat CrossSection;

    [InspectableField] [JsonProperty("hardpoints")] [Key(19)]
    public List<HardpointData> Hardpoints = new List<HardpointData>();

    [InspectableAnimationCurve] [JsonProperty("visibility")] [Key(20)]
    public float4[] VisibilityCurve;

    [InspectablePrefab] [JsonProperty("prefab")] [Key(21)]
    public string Prefab;

    [IgnoreMember] public override HardpointType HardpointType => HardpointType.Hull;
}

[RethinkTable("Items")]
[InspectableField]
[MessagePackObject]
[JsonObject(MemberSerialization.OptIn)]
public class HardpointData
{
    [JsonProperty("type")] [Key(0)]
    public HardpointType Type;
}

[RethinkTable("Items")]
[Inspectable]
[MessagePackObject]
[JsonObject(MemberSerialization.OptIn)]
public class ShieldData : EquippableItemData
{
    [InspectableField] [JsonProperty("efficiency")] [Key(14)] 
    public PerformanceStat Efficiency;

    [InspectableField] [JsonProperty("shielding")] [Key(15)]
    public PerformanceStat Shielding;

    [IgnoreMember] public override HardpointType HardpointType => HardpointType.Shield;
}

[RethinkTable("Items")]
[Inspectable]
[MessagePackObject]
[JsonObject(MemberSerialization.OptIn)]
public class ThrusterData : EquippableItemData
{
    [InspectableField] [JsonProperty("thrust")] [Key(14)]
    public PerformanceStat Thrust;

    [InspectableField] [JsonProperty("torque")] [Key(15)]
    public PerformanceStat Torque;

    [InspectableField] [JsonProperty("visibility")] [Key(16)]
    public PerformanceStat Visibility;

    [InspectableField] [JsonProperty("thrusterHeat")] [Key(17)]
    public PerformanceStat Heat;
    
    [IgnoreMember] public override HardpointType HardpointType => HardpointType.Thruster;
}

[MessagePackObject]
[JsonObject(MemberSerialization.OptIn)]
public struct PerformanceStat
{
    [JsonProperty("min")] [Key(0)]
    public float Min;
    
    [JsonProperty("max")] [Key(1)]
    public float Max;
    
    [JsonProperty("durabilityDependent")] [Key(2)]
    public bool DurabilityDependent;
    
    [JsonProperty("heatDependent")] [Key(3)]
    public bool HeatDependent;
    
    [JsonProperty("qualityExponent")] [Key(4)] 
    public float QualityExponent;
    
    [JsonProperty("ingredient")] [Key(5)] 
    public Guid? Ingredient;
    
    [IgnoreMember] private Dictionary<Ship,Dictionary<IItemBehavior,float>> _scaleModifiers;
    [IgnoreMember] private Dictionary<Ship,Dictionary<IItemBehavior,float>> _constantModifiers;

    [IgnoreMember]
    private Dictionary<Ship, Dictionary<IItemBehavior, float>> ScaleModifiers =>
        _scaleModifiers ?? (_scaleModifiers = new Dictionary<Ship, Dictionary<IItemBehavior, float>>());

    [IgnoreMember]
    private Dictionary<Ship, Dictionary<IItemBehavior, float>> ConstantModifiers =>
        _constantModifiers ?? (_constantModifiers = new Dictionary<Ship, Dictionary<IItemBehavior, float>>());

    public Dictionary<IItemBehavior, float> GetScaleModifiers(Ship ship)
    {
        if(!ScaleModifiers.ContainsKey(ship))
            ScaleModifiers[ship] = new Dictionary<IItemBehavior, float>();

        return ScaleModifiers[ship];
    }

    public Dictionary<IItemBehavior, float> GetConstantModifiers(Ship ship)
    {
        if(!ConstantModifiers.ContainsKey(ship))
            ConstantModifiers[ship] = new Dictionary<IItemBehavior, float>();

        return ConstantModifiers[ship];
    }
}