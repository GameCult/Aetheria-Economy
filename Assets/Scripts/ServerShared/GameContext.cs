using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static Unity.Mathematics.math;

public class GameContext
{
    private DatabaseCache _cache;
    
    public GlobalData GlobalData => _cache.GetAll<GlobalData>().FirstOrDefault();
    
    private readonly Dictionary<CraftedItemData, int> Tier = new Dictionary<CraftedItemData, int>();

    public GameContext(DatabaseCache cache)
    {
        _cache = cache;
        var globalData = _cache.GetAll<GlobalData>().FirstOrDefault();
        if (globalData == null)
        {
            globalData = new GlobalData();
            _cache.Add(globalData);
        }
    }
    
    public int ItemTier(CraftedItemData itemData)
    {
        if (Tier.ContainsKey(itemData)) return Tier[itemData];

        Tier[itemData] = itemData.Ingredients.Keys.Max(ci => _cache.Get<ItemData>(ci) is CraftedItemData craftableIngredient ? ItemTier(craftableIngredient) : 0);
		
        return Tier[itemData];
    }

    public ItemData GetData(ItemInstance item)
    {
        return _cache.Get<ItemData>(item.Data);
    }

    public SimpleCommodityData GetData(SimpleCommodity item)
    {
        return _cache.Get<SimpleCommodityData>(item.Data);
    }

    public CraftedItemData GetData(CraftedItemInstance item)
    {
        return _cache.Get<CraftedItemData>(item.Data);
    }

    public EquippableItemData GetData(Gear gear)
    {
        return _cache.Get<EquippableItemData>(gear.Data);
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

    // Determine quality of either the item itself or the specific ingredient this stat depends on
    public float Quality(PerformanceStat stat, Gear item)
    {
        Guid? ingredientID = stat.Ingredient;
        float quality;
        if (ingredientID == null)
            quality = item.CompoundQuality();
        else
        {
            var ingredientInstance =
                item.Ingredients.FirstOrDefault(i => i.Data == ingredientID) as CraftedItemInstance;
            if (ingredientInstance == null)
                throw new InvalidOperationException(
                    $"Item {item.ID} has invalid crafting ingredients!");
            quality = ingredientInstance.CompoundQuality();
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
    public float Evaluate(PerformanceStat stat, Gear item, Ship ship)
    {
        var itemData = GetData(item);

        var heat = !stat.HeatDependent ? 1 : pow(itemData.Performance(ship.Temperature), Evaluate(itemData.HeatExponent,item));
        var durability = !stat.DurabilityDependent ? 1 : pow(item.Durability / itemData.Durability, Evaluate(itemData.DurabilityExponent,item));
        var quality = pow(Quality(stat, item), stat.QualityExponent);

        var scaleModifier = stat.GetScaleModifiers(ship).Values.Aggregate(1.0f, (current, mod) => current * mod);

        var constantModifier = stat.GetConstantModifiers(ship).Values.Sum();

        var result = lerp(stat.Min, stat.Max, heat * durability * quality) * scaleModifier + constantModifier;
        if (float.IsNaN(result))
            return stat.Min;
        return result;
    }
    
    public SimpleCommodity CreateInstance(Guid data, int count)
    {
        var item = _cache.Get<SimpleCommodityData>(data);
        if (item != null)
            return new SimpleCommodity
            {
                Data = data,
                Quantity = count,
                ID = Guid.NewGuid()
            };
        
        throw new InvalidOperationException("Attempted to create Simple Commodity instance using missing or incorrect item id");
    }
    
    public CraftedItemInstance CreateInstance(Guid data, float quality)
    {
        var item = _cache.Get<CraftedItemData>(data);
        if (item == null)
        {
            throw new InvalidOperationException("Attempted to create crafted item instance using missing or incorrect item id");
        }

        var ingredients = item.Ingredients.SelectMany(ci =>
            {
                var ingredient = _cache.Get(ci.Key);
                return ingredient is SimpleCommodityData
                    ? (IEnumerable<ItemInstance>) new[] {CreateInstance(ci.Key, ci.Value)}
                    : Enumerable.Range(0, ci.Value).Select(i => CreateInstance(ci.Key, quality));
            })
            .ToList();
        
        if (item is EquippableItemData equippableItemData)
        {
            return new Gear
            {
                Context = this,
                Data = data,
                Durability = equippableItemData.Durability,
                ID = Guid.NewGuid(),
                Ingredients = ingredients,
                Quality = quality
            };
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
