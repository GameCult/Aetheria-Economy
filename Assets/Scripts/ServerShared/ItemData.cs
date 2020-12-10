using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JsonKnownTypes;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using int2 = Unity.Mathematics.int2;

public class Shape
{
    [JsonProperty("name"), Key(0)] public bool[,] Cells;
    
    private bool _dirty = true;

    public Shape(int width, int height)
    {
        Cells = new bool[width, height];
    }
    
    public int Width => Cells.GetLength(0);

    public int Height => Cells.GetLength(1);

    private static int mod(int x, int m) {
        int r = x%m;
        return r<0 ? r+m : r;
    }
    
    public int2 Rotate(int2 position, int rotation)
    {
        switch (mod(rotation, 4))
        {
            case 1:
                return int2(position.y, Width - 1 - position.x);
            case 2:
                return int2(Width - 1 - position.x, Height - 1 - position.y);
            case 3:
                return int2(Height - 1 - position.y, position.x);
            default:
                return int2(position.x, position.y);
        }
    }

    private int2[] _cachedShapeCoordinates;
    
    [IgnoreMember]
    public int2[] Coordinates
    {
        get
        {
            if (_dirty) _cachedShapeCoordinates = EnumerateShapeCoordinates().ToArray();
            return _cachedShapeCoordinates;
        }
    }

    private IEnumerable<int2> EnumerateShapeCoordinates()
    {
        for(int x = 0; x < Cells.GetLength(0); x++)
        {
            for (int y = 0; y < Cells.GetLength(0); y++)
            {
                if(Cells[x,y]) yield return int2(x, y);
            }
        }
    }

    public bool this[int2 pos] {
        get { return pos.x >= 0 && pos.y >= 0 && pos.x < Cells.GetLength(0) && pos.y < Cells.GetLength(1) && Cells[pos.x, pos.y]; }
        set
        {
            _dirty = true;
            Cells[pos.x, pos.y] = value;
        }
    }

    public Shape Shrink()
    {
        var shape = new Shape(Width, Height);
        foreach(var shapeCoord in Coordinates)
            shape[shapeCoord] = (
                this[shapeCoord + int2(-1,-1)] && this[shapeCoord + int2(0,-1)] && this[shapeCoord + int2(1,-1)] &&
                this[shapeCoord + int2(-1,0)] && this[shapeCoord + int2(1,0)] && 
                this[shapeCoord + int2(-1,1)] && this[shapeCoord + int2(0,1)] && this[shapeCoord + int2(1,1)]
            );
        return shape;
    }
}

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

    [InspectableField, JsonProperty("shape"), Key(4)]
    public Shape Shape;

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
    // // Types of body where this resource can be found
    // [InspectableField, JsonProperty("resourceBodyType"), Key(6)]  
    // public BodyType ResourceBodyType;
    //
    // // Link to map(s) controlling density of resource, multiplied together when more than one
    // [InspectableDatabaseLink(typeof(GalaxyMapLayerData)), JsonProperty("resourceDensity"), Key(7)]  
    // public List<Guid> ResourceDensity = new List<Guid>();
    //
    // // Controls the lowest value in the resource distribution curve
    // [InspectableField, JsonProperty("distribution"), Key(8)]
    // public ExponentialLerp Distribution;
    //
    // // Minimum amount of resources needed for presence to register
    // [InspectableField, JsonProperty("floor"), Key(11)]
    // public float Floor = 5f;

    [InspectableField, JsonProperty("maxStackSize"), Key(6)]
    public int MaxStack;

    [InspectableField, JsonProperty("category"), Key(7)]  
    public SimpleCommodityCategory Category;
}

[MessagePackObject, 
 Union(0, typeof(CompoundCommodityData)), 
 Union(1, typeof(GearData)), 
 Union(2, typeof(HullData)),
 JsonObject(MemberSerialization.OptIn),
 JsonConverter(typeof(JsonKnownTypesConverter<CraftedItemData>))]
public abstract class CraftedItemData : ItemData
{
    [InspectableField, JsonProperty("ingredientQualityWeight"), Key(6)]  
    public float IngredientQualityWeight = .5f;
}

[RethinkTable("Items"), Inspectable, MessagePackObject]
public class CompoundCommodityData : CraftedItemData
{
    [InspectableDatabaseLink(typeof(PersonalityAttribute)), JsonProperty("demandProfile"), Key(7)]
    public Dictionary<Guid, float> DemandProfile = new Dictionary<Guid, float>();

    [InspectableField, JsonProperty("category"), Key(8)] 
    public CompoundCommodityCategory Category;
}

[Union(0, typeof(GearData)), 
 Union(1, typeof(HullData)), 
 JsonObject(MemberSerialization.OptIn), JsonConverter(typeof(JsonKnownTypesConverter<EquippableItemData>))]
public abstract class EquippableItemData : CraftedItemData
{
    [InspectableField, JsonProperty("behaviors"), Key(7)]  
    public List<BehaviorData> Behaviors = new List<BehaviorData>();

    [InspectableField, JsonProperty("durability"), Key(8)]
    public float Durability;
    
    [TemperatureInspectable, JsonProperty("minTemp"), Key(9)]
    public float MinimumTemperature;

    [TemperatureInspectable, JsonProperty("maxTemp"), Key(10)]
    public float MaximumTemperature;

    [InspectableField, JsonProperty("durabilityExponent"), Key(11), SimplePerformanceStat]
    public PerformanceStat DurabilityExponent = new PerformanceStat();
    
    [IgnoreMember]
    public abstract HardpointType HardpointType { get; }
    
    [IgnoreMember] private const int STEPS = 64;

    [IgnoreMember] private float? _optimum;
}

[RethinkTable("Items"), Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class GearData : EquippableItemData
{
    [InspectableField, JsonProperty("hardpointType"), Key(12)]
    public HardpointType Hardpoint;

    [IgnoreMember] public override HardpointType HardpointType => Hardpoint;
}

[RethinkTable("Items"), Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class CargoBayData : EquippableItemData
{
    [InspectableField, JsonProperty("interiorShape"), Key(12)]
    public Shape InteriorShape;
    [IgnoreMember] public override HardpointType HardpointType => HardpointType.Tool;
}

[RethinkTable("Items"), Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class HullData : EquippableItemData
{
    [InspectableField, JsonProperty("hardpoints"), Key(12)]  
    public List<HardpointData> Hardpoints = new List<HardpointData>();

    [InspectablePrefab, JsonProperty("prefab"), Key(13)]  
    public string Prefab;

    [InspectableField, JsonProperty("hullType"), Key(14)]
    public HullType HullType;

    [IgnoreMember]
    public Shape InteriorCells
    {
        get
        {
            if (_interiorCells == null)
            {
                _interiorCells = Shape.Shrink();
            }

            return _interiorCells;
        }
    }

    private Shape _interiorCells;

    [IgnoreMember] public override HardpointType HardpointType => HardpointType.Hull;
}

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class HardpointData
{
    [InspectableField, JsonProperty("type"), Key(0)] public HardpointType Type;
    [InspectableField, JsonProperty("position"), Key(1)] public int2 Position;
    [InspectableField, JsonProperty("shape"), Key(2)] public Shape Shape;
    [InspectableField, JsonProperty("transform"), Key(3)] public string Transform;
}

[MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class PerformanceStat
{
    [JsonProperty("min"), Key(0)]  public float Min;

    [JsonProperty("max"), Key(1)]  public float Max;

    [JsonProperty("durabilityDependent"), Key(2)] 
    public bool DurabilityDependent;

    // [JsonProperty("heatDependent"), Key(3)] 
    // public bool HeatDependent;

    [JsonProperty("qualityExponent"), Key(3)] 
    public float QualityExponent;
    
    //[JsonProperty("id"), Key(5)]  public Guid ID = Guid.NewGuid();

    // [JsonProperty("ingredient"), Key(5)]  public Guid? Ingredient;
    
    [IgnoreMember] private Dictionary<Entity,Dictionary<IBehavior,float>> _scaleModifiers;
    [IgnoreMember] private Dictionary<Entity,Dictionary<IBehavior,float>> _constantModifiers;

    [IgnoreMember]
    private Dictionary<Entity, Dictionary<IBehavior, float>> ScaleModifiers =>
        _scaleModifiers = _scaleModifiers ?? new Dictionary<Entity, Dictionary<IBehavior, float>>();

    [IgnoreMember]
    private Dictionary<Entity, Dictionary<IBehavior, float>> ConstantModifiers =>
        _constantModifiers = _constantModifiers ?? new Dictionary<Entity, Dictionary<IBehavior, float>>();

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

[RethinkTable("Items"), Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class PersonalityAttribute : DatabaseEntry, INamedEntry
{
    [InspectableField, JsonProperty("name"), Key(1)]
    public string Name;
    
    [InspectableField, JsonProperty("low"), Key(2)]
    public string LowName;
    
    [InspectableField, JsonProperty("high"), Key(3)]
    public string HighName;
    
    [IgnoreMember] public string EntryName
    {
        get => Name;
        set => Name = value;
    }
}