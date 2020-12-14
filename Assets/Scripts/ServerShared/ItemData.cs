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

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class Shape
{
    [JsonProperty("name"), Key(0)] public bool[,] Cells;
    
    private bool _dirty = true;

    public Shape()
    {
        Cells = new bool[1,1];
        Cells[0, 0] = true;
    }

    public Shape(int width, int height)
    {
        Cells = new bool[width, height];
    }
    
    [IgnoreMember]
    public int Width
    {
        get { return Cells.GetLength(0); }
        set { Resize(value, Height); }
    }

    [IgnoreMember]
    public int Height
    {
        get { return Cells.GetLength(1); }
        set { Resize(Width, value); }
    }

    public void Resize(int width, int height)
    {
        var newCells = new bool[width, height];
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                newCells[x, y] = x >= Width || y >= Height || Cells[x, y];
            }
        }

        Cells = newCells;
    }

    private static int mod(int x, int m) {
        int r = x%m;
        return r<0 ? r+m : r;
    }
    
    public int2 Rotate(int2 position, ItemRotation rotation)
    {
        switch (rotation)
        {
            case ItemRotation.Clockwise:
                return int2(position.y, Width - 1 - position.x);
            case ItemRotation.Reversed:
                return int2(Width - 1 - position.x, Height - 1 - position.y);
            case ItemRotation.CounterClockwise:
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
        for (int y = 0; y < Height; y++)
        {
            for(int x = 0; x < Width; x++)
            {
                if(Cells[x,y]) yield return int2(x, y);
            }
        }
    }

    private int2[] _cachedAllShapeCoordinates;
    
    [IgnoreMember]
    public int2[] AllCoordinates => _cachedAllShapeCoordinates ?? (_cachedAllShapeCoordinates = EnumerateAllShapeCoordinates().ToArray());

    private IEnumerable<int2> EnumerateAllShapeCoordinates()
    {
        for (int y = 0; y < Height; y++)
        {
            for(int x = 0; x < Width; x++)
            {
                yield return int2(x, y);
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
    public int MaxStack = 10;

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
    [InspectableTexture, JsonProperty("schematic"), Key(7)]
    public string Schematic;
    
    [InspectableField, JsonProperty("behaviors"), Key(8)]  
    public List<BehaviorData> Behaviors = new List<BehaviorData>();

    [InspectableField, JsonProperty("durability"), Key(9)]
    public float Durability;
    
    [TemperatureInspectable, JsonProperty("minTemp"), Key(10)]
    public float MinimumTemperature;

    [TemperatureInspectable, JsonProperty("maxTemp"), Key(11)]
    public float MaximumTemperature;

    [InspectableField, JsonProperty("durabilityExponent"), Key(12), SimplePerformanceStat]
    public PerformanceStat DurabilityExponent = new PerformanceStat();
    
    [IgnoreMember]
    public abstract HardpointType HardpointType { get; }
    
    [IgnoreMember] private const int STEPS = 64;

    [IgnoreMember] private float? _optimum;
}

[RethinkTable("Items"), Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class GearData : EquippableItemData
{
    [InspectableField, JsonProperty("hardpointType"), Key(13)]
    public HardpointType Hardpoint;

    [IgnoreMember] public override HardpointType HardpointType => Hardpoint;
}

[RethinkTable("Items"), Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class CargoBayData : EquippableItemData
{
    [InspectableField, JsonProperty("interiorShape"), Key(13)]
    public Shape InteriorShape;
    [IgnoreMember] public override HardpointType HardpointType => HardpointType.Tool;
}

[RethinkTable("Items"), Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class HullData : EquippableItemData
{
    [InspectableField, JsonProperty("hardpoints"), Key(13)]  
    public List<HardpointData> Hardpoints = new List<HardpointData>();

    [InspectablePrefab, JsonProperty("prefab"), Key(14)]  
    public string Prefab;

    [InspectableField, JsonProperty("hullType"), Key(15)]
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
public class HardpointData : ITintInspector
{
    [InspectableField, JsonProperty("type"), Key(0)] public HardpointType Type;
    [InspectableField, JsonProperty("position"), Key(1)] public int2 Position;
    [InspectableField, JsonProperty("shape"), Key(2)] public Shape Shape = new Shape();
    [InspectableField, JsonProperty("transform"), Key(3)] public string Transform;
    [InspectableField, JsonProperty("rotation"), Key(4)] public ItemRotation Rotation;

    public override string ToString()
    {
        return $"{Enum.GetName(typeof(HardpointType), Type)} Hardpoint {Rotation.Arrow()}";
    }

    [IgnoreMember]
    public float3 TintColor
    {
        get
        {
            return GetColor(Type);
        }
    }

    public static float3 GetColor(HardpointType type)
    {
        if (_tintColors == null)
        {
            var hardpointTypes = ((HardpointType[]) Enum.GetValues(typeof(HardpointType)));
            _tintColors = hardpointTypes.ToDictionary(x => x,
                x => ColorMath.HsvToRgb(float3((float)(int)x/hardpointTypes.Length, 1, 1)));
        }

        return _tintColors.ContainsKey(type) ? _tintColors[type] : _tintColors[HardpointType.Hull];
    }
    
    private static Dictionary<HardpointType, float3> _tintColors;
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

public interface ITintInspector
{
    float3 TintColor { get; }
}