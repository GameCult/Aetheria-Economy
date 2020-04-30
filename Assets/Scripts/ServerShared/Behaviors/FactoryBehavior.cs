using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Random = Unity.Mathematics.Random;

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
    
    private Guid _blueprint;
    private float _remainingManHours;
    private bool _retooling;
    private bool _producing;
    private List<ItemInstance> _reservedStock = new List<ItemInstance>();
    private int _assignedPopulation;
    private float _productionQuality;

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
        var blueprint = Context.Cache.Get<BlueprintData>(_blueprint);
        if (_retooling || _producing)
        {
            _remainingManHours -= delta / 3600 * (_assignedPopulation + _data.AutomationPoints) /
                                  pow(lerp(blueprint.QualityFloor, 1, saturate(_productionQuality)),
                                      blueprint.ProductionExponent);
        }

        if (_remainingManHours < 0)
        {
            if (_retooling)
                _retooling = false;
            else if (_producing)
            {
                var blueprintItemData = Context.Cache.Get(blueprint.Item);

                if (blueprintItemData is CraftedItemData)
                {
                    for (int i = 0; i < blueprint.Quantity; i++)
                    {
                        CraftedItemInstance newItem;

                        // Applying exponents to two random numbers and adding them produces a range of interesting probability distributions
                        var quality = blueprint.Quality *
                            pow(_productionQuality, blueprint.QualityExponent) *
                            (pow(Context.Random.NextFloat(), blueprint.RandomExponent) +
                             pow(Context.Random.NextFloat(), blueprint.RandomExponent)) / 2;
                    
                        if (blueprintItemData is EquippableItemData equippableItemData)
                        {
                            var newGear = new Gear
                            {
                                Context = Context,
                                Data = blueprintItemData.ID,
                                ID = Guid.NewGuid(),
                                Ingredients = _reservedStock.Select(ii=>ii.ID).ToList(),
                                Quality = quality,
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
                                Quality = quality,
                                Blueprint = _blueprint
                            };
                        }
                        Context.Cache.Add(newItem);
                        Entity.Cargo.Add(newItem);
                    }
                }
                else
                {
                    var simpleCommodityData = blueprintItemData as SimpleCommodityData;
                    var simpleCommodityInstance = Entity.Cargo.FirstOrDefault(i => i.Data == simpleCommodityData.ID) as SimpleCommodity;
                    if (simpleCommodityInstance == null)
                    {
                        simpleCommodityInstance = new SimpleCommodity
                        {
                            Context = Context,
                            Data = simpleCommodityData.ID,
                            ID = Guid.NewGuid(),
                            Quantity = blueprint.Quantity
                        };
                        Context.Cache.Add(simpleCommodityInstance);
                        Entity.Cargo.Add(simpleCommodityInstance);
                    }
                    else
                    {
                        simpleCommodityInstance.Quantity += blueprint.Quantity;
                    }
                }
                Entity.RecalculateMass();

                var simpleIngredients = new List<SimpleCommodity>();
                var compoundIngredients = new List<CompoundCommodity>();
                var hasAllIngredients = true;
                foreach (var kvp in blueprint.Ingredients)
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
                        var blueprintQuantity = blueprint.Ingredients
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
                    Entity.RecalculateMass();
                    _remainingManHours = blueprint.ProductionTime;
                    _producing = true;
                }
            }
        }
    }

    public PersistentBehaviorData Store()
    {
        return new FactoryPersistence
        {
            Blueprint = _blueprint,
            RemainingManHours = _remainingManHours,
            Retooling = _retooling,
            Producing = _producing,
            ReservedStock = _reservedStock.Select(i=>i.ID).ToArray(),
            AssignedPopulation = _assignedPopulation,
            ProductionQuality = _productionQuality
        };
    }

    public void Restore(PersistentBehaviorData data)
    {
        var factoryPersistence = data as FactoryPersistence;
        _blueprint = factoryPersistence.Blueprint;
        _remainingManHours = factoryPersistence.RemainingManHours;
        _retooling = factoryPersistence.Retooling;
        _producing = factoryPersistence.Producing;
        _reservedStock = factoryPersistence.ReservedStock.Select(id => Context.Cache.Get<ItemInstance>(id)).ToList();
        _assignedPopulation = factoryPersistence.AssignedPopulation;
        _productionQuality = factoryPersistence.ProductionQuality;
    }
}

[MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class FactoryPersistence : PersistentBehaviorData
{
    public Guid Blueprint;
    public float RemainingManHours;
    public bool Retooling;
    public bool Producing;
    public Guid[] ReservedStock;
    public int AssignedPopulation;
    public float ProductionQuality;
}