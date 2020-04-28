using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class FactoryData : IBehaviorData
{
    [InspectableField, JsonProperty("toolingTime"), Key(0)]
    public PerformanceStat ToolingTime = new PerformanceStat();

    [InspectableField, JsonProperty("automation"), Key(1)]
    public int AutomationPoints;
    
    public IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new Factory(context, this, entity, item);
    }
}

public class Factory : IBehavior, IPersistentBehavior//<Factory>
{
    public Entity Entity { get; }
    public Gear Item { get; }
    public GameContext Context { get; }

    public IBehaviorData Data => _data;
    
    private FactoryData _data;
    
    private BlueprintData _blueprint;
    private float _remainingManHours;
    private bool _retooling;
    private List<ItemInstance> _reservedStock = new List<ItemInstance>();
    private int _assignedPopulation;
    private int _productionQuality;

    public Factory(GameContext context, FactoryData data, Entity entity, Gear item)
    {
        Context = context;
        _data = data;
        Entity = entity;
        Item = item;
    }

    public void Initialize()
    {
    }

    public void Update(float delta)
    {
        if (_retooling || _reservedStock.Count > 0)
        {
            _remainingManHours -= delta / 3600 * (_assignedPopulation + _data.AutomationPoints) / _productionQuality;
        }

        if (_remainingManHours < 0)
        {
            if (_retooling)
                _retooling = false;
            else if (_reservedStock.Count > 0)
            {
                var blueprintItemData = Context.Cache.Get(_blueprint.Item);

                for (int i = 0; i < _blueprint.Quantity; i++)
                {
                    CraftedItemInstance newItem;
                    
                    if (blueprintItemData is EquippableItemData equippableItemData)
                    {
                        var newGear = new Gear
                        {
                            Context = Context,
                            Data = blueprintItemData.ID,
                            ID = Guid.NewGuid(),
                            Ingredients = _reservedStock.Select(ii=>ii.ID).ToList(),
                            Quality = _productionQuality * _blueprint.Quality,
                            Blueprint = _blueprint
                        };
                        newGear.Durability = Context.Evaluate(equippableItemData.Durability, newGear);
                        newItem = newGear;
                    }
                    else
                    {
                        newItem = new CompoundCommodity
                        {
                            Context = Context,
                            Data = blueprintItemData.ID,
                            ID = Guid.NewGuid(),
                            Ingredients = _reservedStock.Select(ii=>ii.ID).ToList(),
                            Quality = _productionQuality * _blueprint.Quality,
                            Blueprint = _blueprint
                        };
                    }
                    Context.Cache.Add(newItem);
                    Entity.Cargo.Add(newItem);
                }
                Entity.RecalculateMass();

                var simpleIngredients = new List<SimpleCommodity>();
                var compoundIngredients = new List<CompoundCommodity>();
                var hasAllIngredients = true;
                foreach (var kvp in _blueprint.Ingredients)
                {
                    var itemData = Context.Cache.Get(kvp.Key);
                    if (itemData is SimpleCommodityData)
                    {
                        var matchingItem = Entity.Cargo.FirstOrDefault(ii =>
                        {
                            var simpleCommodity = ii as SimpleCommodity;
                            if (simpleCommodity == null) return false;
                            return simpleCommodity.Data == itemData.ID && simpleCommodity.Quantity >= kvp.Value;
                        }) as SimpleCommodity;
                        hasAllIngredients = hasAllIngredients && matchingItem != null;
                        if(matchingItem != null)
                            simpleIngredients.Add(matchingItem);
                    }
                    else
                    {
                        var matchingItems =
                            Entity.Cargo.Where(ii => (ii as CompoundCommodity)?.Data == itemData.ID).Cast<CompoundCommodity>().ToArray();
                        hasAllIngredients = hasAllIngredients && matchingItems.Length >= kvp.Value;
                        if(matchingItems.Length >= kvp.Value)
                            compoundIngredients.AddRange(matchingItems);
                    }
                }

                if (hasAllIngredients)
                {
                    _reservedStock.AddRange(compoundIngredients);
                    foreach (var compoundCommodity in compoundIngredients)
                    {
                        Entity.Cargo.Remove(compoundCommodity);
                    }
                    foreach (var simpleCommodity in simpleIngredients)
                    {
                        var blueprintQuantity = _blueprint.Ingredients
                            .First(ingredient => ingredient.Key == simpleCommodity.ID).Value;
                        if (simpleCommodity.Quantity == blueprintQuantity)
                        {
                            _reservedStock.Add(simpleCommodity);
                            Entity.Cargo.Remove(simpleCommodity);
                        }
                        else
                        {
                            var newSimpleCommodity = new SimpleCommodity
                            {
                                Context = Context,
                                Data = simpleCommodity.Data,
                                ID = Guid.NewGuid(),
                                Quantity = blueprintQuantity
                            };
                            simpleCommodity.Quantity -= blueprintQuantity;
                            _reservedStock.Add(newSimpleCommodity);
                        }
                    }
                }
            }
        }
    }

    public object Store()
    {
        return new FactoryPersistence
        {
            Blueprint = _blueprint.ID,
            RemainingManHours = _remainingManHours,
            Retooling = _retooling,
            ReservedStock = _reservedStock.Select(i=>i.ID).ToArray()
        };
    }

    public IBehavior Restore(GameContext context, Entity entity, Gear item, Guid data)
    {
        var equippable = item.ItemData;
        var behaviorData = (FactoryData) equippable.Behaviors.First(b => b is FactoryData);
        return new Factory(context, behaviorData, entity, item);
    }
}

[MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class FactoryPersistence
{
    public Guid Blueprint;
    public float RemainingManHours;
    public bool Retooling;
    public Guid[] ReservedStock;
}