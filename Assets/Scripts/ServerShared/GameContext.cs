using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static Unity.Mathematics.math;

public class GameContext
{
    private Action<string> _logger;

    private Dictionary<BlueprintStatEffect, PerformanceStat> AffectedStats =
        new Dictionary<BlueprintStatEffect, PerformanceStat>();

    private double _time;
    private float _deltaTime;

    public GlobalData GlobalData => Cache.GetAll<GlobalData>().FirstOrDefault();
    public DatabaseCache Cache { get; }

    public double Time
    {
        get => _time;
        set
        {
            _deltaTime = (float) (value - _time);
            _time = value;
        }
    }

    private Dictionary<ZoneData, Dictionary<DatabaseEntry, Entity>> ZoneContents = new Dictionary<ZoneData, Dictionary<DatabaseEntry, Entity>>();

    // private readonly Dictionary<CraftedItemData, int> Tier = new Dictionary<CraftedItemData, int>();

    public GameContext(DatabaseCache cache, Action<string> logger)
    {
        Cache = cache;
        _logger = logger;
        var globalData = Cache.GetAll<GlobalData>().FirstOrDefault();
        if (globalData == null)
        {
            globalData = new GlobalData();
            Cache.Add(globalData);
        }
    }

    public void Log(string s)
    {
        _logger(s);
    }

    public Dictionary<DatabaseEntry, Entity> InitializeZone(Guid zoneID)
    {
        var contents = new Dictionary<DatabaseEntry, Entity>();

        var zoneData = Cache.Get<ZoneData>(zoneID);
        
        var planetsData = zoneData.Planets.Select(id => Cache.Get<PlanetData>(id)).ToArray();
        var planetEntities = planetsData.ToDictionary(p => p,
            p => new OrbitalEntity(this, Enumerable.Empty<Gear>(), Enumerable.Empty<ItemInstance>(), p.Orbit));
        foreach(var kvp in planetEntities)
            contents.Add(kvp.Key, kvp.Value);

        var stationsData = zoneData.Stations.Select(id => Cache.Get<StationData>(id));
        var stationEntities = stationsData.ToDictionary(s => s,
            s => new OrbitalEntity(this, Enumerable.Empty<Gear>(), Enumerable.Empty<ItemInstance>(), s.Parent));
        foreach(var kvp in stationEntities)
            contents.Add(kvp.Key, kvp.Value);
        
        ZoneContents.Add(zoneData, contents);
        
        return contents;
    }

    public void Update()
    {
        foreach (var kvp in ZoneContents)
        {
            var zone = kvp.Key;
            var entities = kvp.Value.Values;
            foreach (var entity in entities)
            {
                entity.Update(_deltaTime);
            }
        }
    }

    public void UnloadZone(ZoneData zone)
    {
        ZoneContents.Remove(zone);
    }
    
    // public int ItemTier(CraftedItemData itemData)
    // {
    //     if (Tier.ContainsKey(itemData)) return Tier[itemData];
    //
    //     Tier[itemData] = itemData.Ingredients.Keys.Max(ci => _cache.Get<ItemData>(ci) is CraftedItemData craftableIngredient ? ItemTier(craftableIngredient) : 0);
		  //
    //     return Tier[itemData];
    // }

    public ItemData GetData(ItemInstance item)
    {
        return Cache.Get<ItemData>(item.Data);
    }

    public SimpleCommodityData GetData(SimpleCommodity item)
    {
        return Cache.Get<SimpleCommodityData>(item.Data);
    }

    public CraftedItemData GetData(CraftedItemInstance item)
    {
        return Cache.Get<CraftedItemData>(item.Data);
    }

    public EquippableItemData GetData(Gear gear)
    {
        return Cache.Get<EquippableItemData>(gear.Data);
    }

    public float GetMass(ItemInstance item)
    {
        var data = GetData(item);
        switch (item)
        {
            case CraftedItemInstance _:
                return data.Mass;
            case SimpleCommodity commodity:
                return data.Mass * commodity.Quantity;
        }

        return 0;
    }

    public float GetHeatCapacity(ItemInstance item)
    {
        var data = GetData(item);
        switch (item)
        {
            case CraftedItemInstance _:
                return data.Mass * data.SpecificHeat;
            case SimpleCommodity commodity:
                return data.Mass * data.SpecificHeat * commodity.Quantity;
        }

        return 0;
    }

    public PerformanceStat GetAffectedStat(BlueprintData blueprint, BlueprintStatEffect effect)
    {
        // We've already cached this effect's stat, return it directly
        if (AffectedStats.ContainsKey(effect)) return AffectedStats[effect];
        
        // Get the data for the item the blueprint is for, return null if not found or not equippable
        var blueprintItem = Cache.Get<EquippableItemData>(blueprint.Item);
        if (blueprintItem == null)
        {
            _logger($"Attempted to get stat effect but Blueprint {blueprint.ID} is not for an equippable item!");
            return AffectedStats[effect] = null;
        }
        
        // Get the first behavior of the type specified in the blueprint stat effect, return null if not found
        var effectObject = effect.StatReference.Behavior == blueprintItem.Name ? (object) blueprintItem : 
            blueprintItem.Behaviors.FirstOrDefault(b => b.GetType().Name == effect.StatReference.Behavior);
        if (effectObject == null)
        {
            _logger($"Attempted to get stat effect for Blueprint {blueprint.ID} but stat references missing behavior \"{effect.StatReference.Behavior}\"!");
            return AffectedStats[effect] = null;
        }
        
        // Get the first field in the behavior matching the name specified in the stat effect, return null if not found
        var type = effectObject.GetType();
        var field = type.GetField(effect.StatReference.Stat);
        if (field == null)
        {
            _logger($"Attempted to get stat effect for Blueprint {blueprint.ID} but object {effect.StatReference.Behavior} does not have a stat named \"{effect.StatReference.Stat}\"!");
            return AffectedStats[effect] = null;
        }
        
        // Finally we've confirmed the stat effect is valid, return the affected stat
        return AffectedStats[effect] = field.GetValue(effectObject) as PerformanceStat;
    }

    // Determine quality of either the item itself or the specific ingredient this stat depends on
    public float Quality(PerformanceStat stat, Gear item)
    {
        
        var activeEffects = item.Blueprint.StatEffects.Where(x => GetAffectedStat(item.Blueprint, x) == stat).ToArray();
        float quality;
        if (!activeEffects.Any())
            quality = item.CompoundQuality();
        else
        {
            var ingredients = item.Ingredients.Where(i => activeEffects.Any(e => e.Ingredient == i.Data)).ToArray();
            if(ingredients.Length != activeEffects.Length)
            {
                _logger($"Item {item.ID} does not have the ingredients specified by the stat effects of its blueprint!");
                return 0;
            }

            float sum = 0;
            foreach (var i in ingredients)
            {
                if (i is CraftedItemInstance ci)
                    sum += ci.CompoundQuality();
                else _logger($"Blueprint stat effect for item {item.ID} specifies invalid (non crafted) ingredient!");
            }

            quality = sum / ingredients.Length;
        }

        return quality;
    }

    // Returns stat when not equipped
    public float Evaluate(PerformanceStat stat, Gear item)
    {
        var quality = pow(Quality(stat, item), stat.QualityExponent);

        var result = lerp(stat.Min, stat.Max, quality);
        
        if (float.IsNaN(result))
            return stat.Min;
        
        return result;
    }

    // Returns stat using ship temperature and modifiers
    public float Evaluate(PerformanceStat stat, Gear item, Entity entity)
    {
        var itemData = GetData(item);

        var heat = !stat.HeatDependent ? 1 : pow(itemData.Performance(entity.Temperature), Evaluate(itemData.HeatExponent,item));
        var durability = !stat.DurabilityDependent ? 1 : pow(item.Durability / Evaluate(itemData.Durability, item), Evaluate(itemData.DurabilityExponent,item));
        var quality = pow(Quality(stat, item), stat.QualityExponent);

        var scaleModifier = stat.GetScaleModifiers(entity).Values.Aggregate(1.0f, (current, mod) => current * mod);

        var constantModifier = stat.GetConstantModifiers(entity).Values.Sum();

        var result = lerp(stat.Min, stat.Max, heat * durability * quality) * scaleModifier + constantModifier;
        if (float.IsNaN(result))
            return stat.Min;
        return result;
    }
    
    public SimpleCommodity CreateInstance(Guid data, int count)
    {
        var item = Cache.Get<SimpleCommodityData>(data);
        if (item != null)
            return new SimpleCommodity
            {
                Data = data,
                Quantity = count,
                ID = Guid.NewGuid()
            };
        
        _logger("Attempted to create Simple Commodity instance using missing or incorrect item id");
        return null;
    }
    
    public CraftedItemInstance CreateInstance(Guid data, float quality)
    {
        var item = Cache.Get<CraftedItemData>(data);
        if (item == null)
        {
            _logger("Attempted to create crafted item instance using missing or incorrect item id!");
            return null;
        }

        var blueprint = Cache.GetAll<BlueprintData>().FirstOrDefault(b => b.Item == data);
        if (blueprint == null)
        {
            _logger("Attempted to create crafted item instance which has no blueprint!");
            return null;
        }
        
        var ingredients = blueprint.Ingredients.SelectMany(ci =>
            {
                var ingredient = Cache.Get(ci.Key);
                return ingredient is SimpleCommodityData
                    ? (IEnumerable<ItemInstance>) new[] {CreateInstance(ci.Key, ci.Value)}
                    : Enumerable.Range(0, ci.Value).Select(i => CreateInstance(ci.Key, quality));
            })
            .ToList();
        
        if (item is EquippableItemData equippableItemData)
        {
            var newGear = new Gear
            {
                Context = this,
                Data = data,
                ID = Guid.NewGuid(),
                Ingredients = ingredients,
                Quality = quality
            };
            newGear.Durability = Evaluate(equippableItemData.Durability, newGear);
            return newGear;
        }

        return new CompoundCommodity
        {
            Context = this,
            Data = data,
            ID = Guid.NewGuid(),
            Ingredients = ingredients,
            Quality = quality
        };
    }
}