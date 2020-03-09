using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.UI.Extensions;
// TODO: USE THIS EVERYWHERE
using static Unity.Mathematics.math;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class NameAttribute : Attribute
{
    public string Name;

    public NameAttribute(string name)
    {
        Name = name;
    }
}

public class UpdateOrderAttribute : Attribute
{
    public int Order;

    public UpdateOrderAttribute(int order)
    {
        Order = order;
    }
}

public class GlobalData : IDatabaseEntry
{
    [Inspectable] [Key("entry")] public DatabaseEntry DatabaseEntry = new DatabaseEntry();

    [Inspectable] [Key("targetPersistenceDuration")]
    public float TargetPersistenceDuration;
    [Inspectable] [Key("heatRadiationPower")]
    public float HeatRadiationPower;
    [Inspectable] [Key("heatRadiationMultiplier")]
    public float HeatRadiationMultiplier;

    [IgnoreMember] public static GlobalData Instance => Database.GetAll<GlobalData>().FirstOrDefault();
    [IgnoreMember] public DatabaseEntry Entry => DatabaseEntry;
}

[Union(5, typeof(Hull))]
[Union(6, typeof(Shield))]
[Union(9, typeof(Thruster))]
[Union(12, typeof(EquippableItem))]
public interface IEquippable : ICraftable
{
    HardpointType HardpointType { get; }
    EquippableData Equippable { get; }
}

[Union(5, typeof(Hull))]
[Union(6, typeof(Shield))]
[Union(9, typeof(Thruster))]
[Union(12, typeof(CraftedItem))]
[Union(13, typeof(EquippableItem))]
public interface ICraftable : IItem
{
    Dictionary<Guid, int> CraftingIngredients { get; }
}

[Union(5, typeof(Hull))]
[Union(6, typeof(Shield))]
[Union(9, typeof(Thruster))]
[Union(12, typeof(ItemData))]
[Union(13, typeof(CraftedItem))]
[Union(14, typeof(EquippableItem))]
public interface IItem : IDatabaseEntry
{
    ItemData Data { get; }
}

[Union(5, typeof(Hull))]
[Union(6, typeof(Shield))]
[Union(9, typeof(Thruster))]
[Union(12, typeof(ItemData))]
[Union(13, typeof(CraftedItem))]
[Union(14, typeof(LoadoutDefinition))]
[Union(15, typeof(ZoneData))]
[Union(16, typeof(SimpleCommodity))]
[Union(17, typeof(CompoundCommodity))]
[Union(18, typeof(Gear))]
[Union(19, typeof(EquippableItem))]
//[Union(21, typeof(ContractData))]
//[Union(22, typeof(Station))]
[Union(23, typeof(Corporation))]
[Union(24, typeof(Player))]
[Union(25, typeof(GlobalData))]
public interface IDatabaseEntry
{
    DatabaseEntry Entry { get; }
}

[Union(0, typeof(SimpleCommodity))]
[Union(1, typeof(CompoundCommodity))]
[Union(2, typeof(Gear))]
public interface IItemInstance : IDatabaseEntry
{
    Guid ItemGuid { get; }
    IItem ItemData { get; }
    float Mass { get; }
}

[Union(0, typeof(Gear))]
[Union(1, typeof(CompoundCommodity))]
public interface ICraftedItemInstance : IItemInstance
{
    float CraftedQuality { get; }
    IEnumerable<IItemInstance> CraftedIngredients { get; }
}

[Inspectable]
[MessagePackObject]
public class DatabaseEntry
{
    [Key("id")]          public Guid   ID = Guid.NewGuid();
    [Inspectable] [Key("name")]        public string Name;
    [Inspectable] [Key("description")] public string Description;
}

[Inspectable]
[MessagePackObject]
[Name("Simple Commodity")]
public class ItemData : IItem
{
    [Inspectable] [Key("entry")] public DatabaseEntry DatabaseEntry = new DatabaseEntry();
    [Inspectable] [Key("mass")] public float Mass;
    [Inspectable] [Key("size")] public float Size;

    [RangedIntInspectable(0, 20)] [Key("level")] public int TechLevel;

    [IgnoreMember] public ItemData Data => this;

    [IgnoreMember] public DatabaseEntry Entry => DatabaseEntry;
}

[MessagePackObject]
public class EquippableItem : IEquippable
{
    [Inspectable] [Key("hardpointType")]  public HardpointType         Hardpoint;
    [Inspectable] [Key("itemData")]       public ItemData              ItemData = new ItemData();
    [Inspectable] [Key("equippableData")] public EquippableData        EquippableData = new EquippableData();
    [Inspectable] [Key("ingredients")]    public Dictionary<Guid, int> Ingredients = new Dictionary<Guid, int>();

    [IgnoreMember] public HardpointType HardpointType => Hardpoint;
    [IgnoreMember] public EquippableData        Equippable =>          EquippableData;
    [IgnoreMember] public ItemData              Data =>                ItemData;
    [IgnoreMember] public Dictionary<Guid, int> CraftingIngredients => Ingredients;
    [IgnoreMember] public DatabaseEntry         Entry =>               ItemData.Entry;
    
}

[Inspectable]
[MessagePackObject]
public class EquippableData
{
    [Inspectable] [Key("draw")]         public PerformanceStat EnergyDraw;
    // [Inspectable] [Key("conductivity")] public float           ThermalConductivity;
    // [Inspectable] [Key("specificHeat")] public float           SpecificHeat;
    [Inspectable] [Key("performance")]  public AnimationCurve  HeatPerformanceCurve;
    [TemperatureInspectable] [Key("minTemp")] public float MinimumTemperature;
    [TemperatureInspectable] [Key("maxTemp")] public float MaximumTemperature;
    [Inspectable] [Key("ruggedness")]   public float           Ruggedness;
    [Inspectable] [Key("durability")]   public float           Durability;
    [Inspectable] [Key("durabilityExponent")] public PerformanceStat DurabilityExponent;
    [Inspectable] [Key("heatExponent")]       public PerformanceStat HeatExponent;
    [Inspectable] [Key("behaviors")]    public List<IItemBehaviorData> Behaviors = new List<IItemBehaviorData>();
    
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

[MessagePackObject]
[Name("Compound Commodity")]
public class CraftedItem : ICraftable
{
    [Inspectable] [Key("itemData")]    public ItemData              ItemData = new ItemData();
    [Inspectable] [Key("ingredients")] public Dictionary<Guid, int> Ingredients = new Dictionary<Guid, int>();

    [IgnoreMember] public ItemData              Data =>                ItemData;
    [IgnoreMember] public Dictionary<Guid, int> CraftingIngredients => Ingredients;
    [IgnoreMember] public DatabaseEntry         Entry =>               ItemData.Entry;
}

[MessagePackObject]
public class Hull : IEquippable
{
    [Inspectable] [Key("itemData")]       public ItemData              ItemData = new ItemData();
    [Inspectable] [Key("equippableData")] public EquippableData        EquippableData = new EquippableData();
    [Inspectable] [Key("ingredients")]    public Dictionary<Guid, int> Ingredients = new Dictionary<Guid, int>();
    [Inspectable] [Key("hullCapacity")]   public PerformanceStat       Capacity;
    [Inspectable] [Key("traction")]       public PerformanceStat       Traction;
    [Inspectable] [Key("topSpeed")]       public PerformanceStat       TopSpeed;
    [Inspectable] [Key("emissivity")]     public PerformanceStat       Emissivity;
    [Inspectable] [Key("crossSection")]   public PerformanceStat       CrossSection;
    [Key("schemaSprite")] [MessagePackFormatter(typeof(UnitySpriteFormatter))]
    public Sprite Schematic;
    [Inspectable] [Key("hardpoints")]                   public List<HardpointData>   Hardpoints = new List<HardpointData>();
    [Inspectable] [Key("visibility")]     public AnimationCurve        VisibilityCurve;
    [MessagePackFormatter(typeof(UnityGameObjectFormatter))]
    [Inspectable] [Key("prefab")]         public GameObject            Prefab;
    
    [IgnoreMember] public HardpointType         HardpointType =>       HardpointType.Hull;
    [IgnoreMember] public EquippableData        Equippable =>          EquippableData;
    [IgnoreMember] public ItemData              Data =>                ItemData;
    [IgnoreMember] public Dictionary<Guid, int> CraftingIngredients => Ingredients;
    [IgnoreMember] public DatabaseEntry         Entry =>               ItemData.Entry;
}

//[MessagePackObject]
//public class Sensor : IEquippable
//{
//    [Key("itemData")]       public ItemData              ItemData = new ItemData();
//    [Key("equippableData")] public EquippableData        EquippableData = new EquippableData();
//    [Key("ingredients")]    public Dictionary<Guid, int> Ingredients = new Dictionary<Guid, int>();
//    [Key("radiance")]       public PerformanceStat       Radiance;
//    [Key("masking")]        public PerformanceStat       RadianceMasking;
//    [Key("sensitivity")]    public PerformanceStat       Sensitivity;
//    
//    [IgnoreMember] public HardpointType         HardpointType =>       HardpointType.Sensors;
//    [IgnoreMember] public EquippableData        Equippable =>          EquippableData;
//    [IgnoreMember] public ItemData              Data =>                ItemData;
//    [IgnoreMember] public Dictionary<Guid, int> CraftingIngredients => Ingredients;
//    [IgnoreMember] public DatabaseEntry Entry => ItemData.Entry;
//}
//
[MessagePackObject]
public class Shield : IEquippable
{
    [Inspectable] [Key("itemData")]
    public ItemData ItemData = new ItemData();

    [Inspectable] [Key("equippableData")] 
    public EquippableData EquippableData = new EquippableData();

    [Inspectable] [Key("ingredients")] 
    public Dictionary<Guid, int> Ingredients = new Dictionary<Guid, int>();

    [Inspectable] [Key("efficiency")] 
    public PerformanceStat Efficiency;

    [Inspectable] [Key("shielding")]
    public PerformanceStat Shielding;

    [IgnoreMember] public HardpointType HardpointType => HardpointType.Shield;
    [IgnoreMember] public EquippableData Equippable => EquippableData;
    [IgnoreMember] public ItemData Data => ItemData;
    [IgnoreMember] public Dictionary<Guid, int> CraftingIngredients => Ingredients;
    [IgnoreMember] public DatabaseEntry Entry => ItemData.Entry;
}

[MessagePackObject]
public class Thruster : IEquippable
{
    [Inspectable] [Key("itemData")]       public ItemData              ItemData = new ItemData();
    [Inspectable] [Key("equippableData")] public EquippableData        EquippableData = new EquippableData();
    [Inspectable] [Key("ingredients")]    public Dictionary<Guid, int> Ingredients = new Dictionary<Guid, int>();
    [Inspectable] [Key("thrust")]         public PerformanceStat       Thrust;
    [Inspectable] [Key("torque")]         public PerformanceStat       Torque;
    [Inspectable] [Key("visibility")]     public PerformanceStat       Visibility;
    [Inspectable] [Key("thrusterHeat")]   public PerformanceStat       Heat;
//    [Inspectable] [Key("minSound")]       public GranularEmitterStats  MinSound;
//    [Inspectable] [Key("maxSound")]       public GranularEmitterStats  MaxSound;
//    [Inspectable] [Key("time")] public float SoundTransitionTime;
//    [Inspectable] [Key("perlin")] public bool OscillatorPerlin;
    // [MessagePackFormatter(typeof(UnityAudioClipFormatter))]
    // [Inspectable] [Key("clip")] public AudioClip Clip;
    
    [IgnoreMember] public HardpointType         HardpointType =>       HardpointType.Thruster;
    [IgnoreMember] public EquippableData        Equippable =>          EquippableData;
    [IgnoreMember] public ItemData              Data =>                ItemData;
    [IgnoreMember] public Dictionary<Guid, int> CraftingIngredients => Ingredients;
    [IgnoreMember] public DatabaseEntry Entry => ItemData.Entry;
}

//[MessagePackObject]
//public class Launcher : IEquippable
//{
//    [Key("itemData")]        public ItemData              ItemData = new ItemData();
//    [Key("equippableData")]  public EquippableData        EquippableData = new EquippableData();
////    [Key("weaponData")]      public WeaponData            WeaponData = new WeaponData();
//    [Key("ingredients")]     public Dictionary<Guid, int> Ingredients = new Dictionary<Guid, int>();
//    [MessagePackFormatter(typeof(UnityGameObjectFormatter))]
//    [Key("projectile")]      public GameObject            ProjectilePrefab;
//    [Key("launcherCaliber")] public LauncherCaliber       Caliber;
//    [Key("salvoDuration")]   public PerformanceStat       SalvoDuration;
//    [Key("salvoSize")]       public int                   SalvoSize;
//    
//    [IgnoreMember] public HardpointType         HardpointType =>       HardpointType.Launcher;
//    [IgnoreMember] public EquippableData        Equippable =>          EquippableData;
//    [IgnoreMember] public ItemData              Data =>                ItemData;
//    [IgnoreMember] public Dictionary<Guid, int> CraftingIngredients => Ingredients;
////    [IgnoreMember] public WeaponData            Weapon =>              WeaponData;
//    [IgnoreMember] public DatabaseEntry         Entry =>               ItemData.Entry;
//}

//[MessagePackObject]
//public class Beam : IEquippable
//{
//    [Key("itemData")]       public ItemData              ItemData = new ItemData();
//    [Key("equippableData")] public EquippableData        EquippableData = new EquippableData();
////    [Key("weaponData")]     public WeaponData            WeaponData = new WeaponData();
//    [Key("ingredients")]    public Dictionary<Guid, int> Ingredients = new Dictionary<Guid, int>();
//    [Key("duration")]       public PerformanceStat       Duration;
//    [Key("diameter")]       public PerformanceStat       Diameter;
//    [Key("amplitude")]      public AnimationCurve        Amplitude;
//    [MessagePackFormatter(typeof(UnityMaterialFormatter))]
//    [Key("material")]       public Material              BeamMaterial;
//    [Key("damageType")]     public ProjectileType        DamageType;
//    
//    [IgnoreMember] public HardpointType         HardpointType =>       DamageType==ProjectileType.Physical?HardpointType.Ballistic:HardpointType.Energy;
//    [IgnoreMember] public EquippableData        Equippable =>          EquippableData;
//    [IgnoreMember] public ItemData              Data =>                ItemData;
//    [IgnoreMember] public Dictionary<Guid, int> CraftingIngredients => Ingredients;
////    [IgnoreMember] public WeaponData            Weapon =>              WeaponData;
//    [IgnoreMember] public DatabaseEntry         Entry =>               ItemData.Entry;
//}

[MessagePackObject]
public struct PerformanceStat
{
    [Key("min")]
    public float Min;
    [Key("max")]
    public float Max;
    [Key("durabilityDependent")]
    public bool DurabilityDependent;
    [Key("heatDependent")]
    public bool HeatDependent;
    [Key("qualityExponent")] 
    public float QualityExponent;
    [Key("ingredient")] 
    public Guid Ingredient;
    
    [IgnoreMember] private Dictionary<Ship,Dictionary<IItemBehavior,float>> _scaleModifiers;
    [IgnoreMember] private Dictionary<Ship,Dictionary<IItemBehavior,float>> _constantModifiers;

    [IgnoreMember]
    private Dictionary<Ship,Dictionary<IItemBehavior, float>> ScaleModifiers
    {
        get { return _scaleModifiers ?? (_scaleModifiers = new Dictionary<Ship,Dictionary<IItemBehavior, float>>()); }
    }

    [IgnoreMember]
    private Dictionary<Ship,Dictionary<IItemBehavior, float>> ConstantModifiers
    {
        get { return _constantModifiers ?? (_constantModifiers = new Dictionary<Ship,Dictionary<IItemBehavior, float>>()); }
    }

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

    // Determine quality of either the item itself or the specific ingredient this stat depends on
    public float Quality(Gear item)
    {
        Guid ingredientID = Ingredient;
        float quality;
        if (ingredientID == Guid.Empty)
            quality = item.CompoundQuality();
        else
        {
            var ingredientInstance =
                item.CraftedIngredients.FirstOrDefault(i => i.ItemGuid == ingredientID) as ICraftedItemInstance;
            if (ingredientInstance == null)
                throw new InvalidOperationException(
                    $"Item {item.Entry.Name} has invalid crafting ingredient set for performance stat.");
            quality = ingredientInstance.CompoundQuality();
        }

        return quality;
    }

    // Returns stat when not equipped
    public float Evaluate(Gear item)
    {
        var quality = pow(Quality(item), QualityExponent);

        var result = lerp(Min, Max, quality);
        
        if (float.IsNaN(result))
            return Min;
        
        return result;
    }

    // Returns stat using ship temperature and modifiers
    public float Evaluate(Gear item, Ship ship)
    {
        var itemData = item.EquippedItemData;

        var heat = !HeatDependent ? 1 : pow(itemData.Performance(ship.Temperature), itemData.Equippable.HeatExponent.Evaluate(item));
        var durability = !DurabilityDependent ? 1 : pow(item.Durability / itemData.Equippable.Durability, itemData.Equippable.DurabilityExponent.Evaluate(item));
        var quality = pow(Quality(item), QualityExponent);

        var scaleModifier = 1.0f;
        if (_scaleModifiers != null && _scaleModifiers.ContainsKey(ship))
        {
            foreach (var mod in _scaleModifiers[ship].Values)
            {
                scaleModifier *= mod;
            }
        }

        var constantModifier = 0.0f;
        if (_constantModifiers != null && _constantModifiers.ContainsKey(ship))
        {
            foreach (var mod in _constantModifiers[ship].Values)
            {
                scaleModifier += mod;
            }
        }
        
        var result = lerp(Min, Max, heat * durability * quality) * scaleModifier + constantModifier;
        if (float.IsNaN(result))
            return Min;
        return result;
    }
}

[MessagePackObject]
public class Hardpoint
{
    [Key("item")]        public Gear  Item;
    
    // [IgnoreMember] public Ship Ship;
    [IgnoreMember] public HardpointData HardpointData;

    // [IgnoreMember]
    // public float HeatCapacity => Item != null
    //     ? Item.EquippedItemData.Data.Mass * Item.EquippedItemData.Equippable.SpecificHeat
    //     : Ship.Hull.EquippedItemData.Data.Mass * Ship.Hull.EquippedItemData.Equippable.SpecificHeat / Ship.HullHardpointCount;
}


[Name("Loadout")]
[MessagePackObject]
public class LoadoutDefinition : IDatabaseEntry
{
    [Inspectable] [Key("entry")] public DatabaseEntry DatabaseEntry = new DatabaseEntry();
    [InspectableDatabaseLink(typeof(Hull))] [Key("hull")]  public Guid Hull;
    [Key("items")] public List<Guid>    Items = new List<Guid>();

    [IgnoreMember] public DatabaseEntry Entry => DatabaseEntry;
}

[MessagePackObject]
public class Player : IDatabaseEntry
{
    [Inspectable] [Key("entry")] public DatabaseEntry DatabaseEntry = new DatabaseEntry();
    [Key("email")] public string Email;
    [Key("password")] public string Password;
    
    [IgnoreMember] public DatabaseEntry Entry => DatabaseEntry;
}


[MessagePackObject]
public class HardpointData
{
    [Key("type")]     public HardpointType Type;
}

[MessagePackObject]
public class ZoneData : IDatabaseEntry
{
    [Inspectable] [Key("entry")]        public DatabaseEntry DatabaseEntry = new DatabaseEntry();
    [Inspectable] [Key("radius")]       public float         Radius = 500;
    [Inspectable] [Key("radiusPower")]  public float         RadiusPower = 1.75f;
    [Inspectable] [Key("mass")]         public float         Mass = 100000;
    [Inspectable] [Key("massFloor")]    public float         MassFloor = 1;
    [Inspectable] [Key("sunMass")]      public float         SunMass = 10000;
    [Inspectable] [Key("gasGiantMass")] public float         GasGiantMass = 2000;
    [Inspectable] [Key("stations")]     public List<Station> Stations = new List<Station>();

    [InspectableDatabaseLink(typeof(ZoneData))] [Key("wormholes")]
    public List<Guid> Wormholes = new List<Guid>();

    [IgnoreMember] public DatabaseEntry Entry => DatabaseEntry;
}

[Inspectable]
[MessagePackObject]
public class Station
{
    [Inspectable] [Key("name")] public string Name;
    [InspectableDatabaseLink(typeof(Corporation))] [Key("faction")] public Guid Faction;
    [Inspectable] [Key("dockingDistance")] public float DockingDistance = 10;
    [Inspectable] [Key("buying")] public Dictionary<Guid, float> BuyPrices = new Dictionary<Guid, float>();
    [Inspectable] [Key("selling")] public Dictionary<Guid, float> SellPrices = new Dictionary<Guid, float>();
    [MessagePackFormatter(typeof(UnityGameObjectFormatter))] [Inspectable] [Key("prefab")] public GameObject Prefab;
}

[MessagePackObject]
public class Corporation : IDatabaseEntry
{
    [Inspectable] [Key("entry")] public DatabaseEntry DatabaseEntry = new DatabaseEntry();
    [InspectableDatabaseLink(typeof(Corporation))] [Key("parent")] public Guid ParentCorporation;
    [Inspectable] [MessagePackFormatter(typeof(UnitySpriteFormatter))] [Key("logo")] public Sprite Logo;
    [Inspectable] [Key("livery")] public Skin Livery = new Skin();
    
    [IgnoreMember] public DatabaseEntry Entry => DatabaseEntry;
}

[Inspectable]
[MessagePackObject]
public class Skin
{
    [Inspectable] [MessagePackFormatter(typeof(UnityMaterialFormatter))] [Key("primary")] public Material Primary;
    [Inspectable] [MessagePackFormatter(typeof(UnityMaterialFormatter))] [Key("secondary")] public Material Secondary;
    [Inspectable] [MessagePackFormatter(typeof(UnityMaterialFormatter))] [Key("trim")] public Material Trim;
    [Inspectable] [MessagePackFormatter(typeof(UnityMaterialFormatter))] [Key("windshield")] public Material Windshield;
}


public enum ProjectileType
{
    Physical, 
    Energy
}

public enum LauncherCaliber
{
    Missile,
    Torpedo,
    Fighter
}

public enum HardpointType
{
    Hull,
    Thruster,
    WarpThruster,
    Reactor,
    Radiator,
    Shield,
    Cooler,
    Sensors,
    Tool,
    Energy,
    Ballistic,
    Launcher
}

public enum DamageType
{
    Kinetic,
    Energy,
    Thermal,
    Optical,
    Ionizing
}