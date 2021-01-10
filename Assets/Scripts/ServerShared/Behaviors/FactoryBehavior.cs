/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Random = Unity.Mathematics.Random;

[MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class FactoryData : BehaviorData
{
    [InspectableField, JsonProperty("toolingTime"), Key(1)]
    public PerformanceStat ToolingTime = new PerformanceStat();

    [InspectableField, JsonProperty("automation"), Key(2)]
    public int AutomationPoints;

    [InspectableField, JsonProperty("automationQuality"), Key(3)]
    public float AutomationQuality = .5f;
    
    [InspectableDatabaseLink(typeof(PersonalityAttribute)), JsonProperty("productionProfile"), Key(4)]  
    public Dictionary<Guid, float> ProductionProfile = new Dictionary<Guid, float>();
    
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new Factory(context, this, entity, item);
    }
}

public class Factory : IBehavior//, IPersistentBehavior, IPopulationAssignment
{
    public float ProductionQuality;
    public double RetoolingTime;
    public string ItemName;
    public Guid ItemUnderConstruction;
    public bool Active;
    public int AssignedPopulation { get; set; }
    
    public Entity Entity { get; }
    public EquippedItem Item { get; }
    public ItemManager Context { get; }

    public BehaviorData Data => _data;
    
    public float ToolingTime { get; private set; }
    
    public Guid Blueprint
    {
        get => _blueprint;
        set
        {
            if (value != _blueprint)
            {
                _blueprint = value;
                Active = false;
                if(value != Guid.Empty)
                {
                    ItemName = Context.ItemData.Get<ItemData>(Context.ItemData.Get<BlueprintData>(value).Item).Name;
                    RetoolingTime = ToolingTime;
                    _retooling = true;
                }
            }
        }
    }
    
    private FactoryData _data;
    
    private Guid _blueprint;
    private bool _retooling = false;
    private float _currentProductionQuality;

    public Factory(ItemManager context, FactoryData data, Entity entity, EquippedItem item)
    {
        Context = context;
        _data = data;
        Entity = entity;
        Item = item;
    }
    
    public bool Execute(float delta)
    {
    //     ToolingTime = Context.Evaluate(_data.ToolingTime, Item, Entity);
    //     var blueprint = Context.ItemData.Get<BlueprintData>(_blueprint);
    //     if (blueprint == null)
    //         return false;
    //
    //     if (ItemUnderConstruction != Guid.Empty)
    //     {
    //         Entity.IncompleteCargo[ItemUnderConstruction] =
    //             Entity.IncompleteCargo[ItemUnderConstruction] - delta * (AssignedPopulation + _data.AutomationPoints) /
    //             pow(lerp(blueprint.QualityFloor, 1, saturate(_currentProductionQuality)),
    //                 blueprint.ProductionExponent);
    //         
    //         if(Entity.IncompleteCargo[ItemUnderConstruction] < 0)
    //         {
    //             var item = Context.ItemData.Get<ItemInstance>(ItemUnderConstruction);
    //             if (item is SimpleCommodity simpleCommodity)
    //                 Entity.AddCargo(simpleCommodity);
    //             else if (item is CraftedItemInstance craftedItemInstance)
    //                 Entity.AddCargo(craftedItemInstance);
    //             Entity.IncompleteCargo.Remove(ItemUnderConstruction);
    //             ItemUnderConstruction = Guid.Empty;
    //             foreach (var attribute in _data.ProductionProfile)
    //                 Entity.Personality[attribute.Key] = lerp(Entity.Personality[attribute.Key], attribute.Value,
    //                     Context.GameplaySettings.ProductionPersonalityLerp * ((float) AssignedPopulation / Entity.Population));
    //         }
    //
    //         return true;
    //     }
    //     
    //     if (RetoolingTime > 0)
    //     {
    //         RetoolingTime -= delta;
    //         return false;
    //     }
    //
    //     if (_retooling)
    //     {
    //         _retooling = false;
    //     }
    //
    //     if (Active)
    //     {
    //         var profileDistance = 0f;
    //         if (_data.ProductionProfile.Any())
    //             profileDistance = _data.ProductionProfile.Sum(x => abs(x.Value - Entity.Personality[x.Key])) /
    //                               _data.ProductionProfile.Count;
    //         var personalityQuality = 
    //             (_data.AutomationPoints * _data.AutomationQuality + AssignedPopulation * (1 - profileDistance)) / 
    //             (_data.AutomationPoints + AssignedPopulation);
    //         ItemUnderConstruction = Entity.Build(blueprint, 
    //             blueprint.Quality *
    //             pow(personalityQuality, blueprint.PersonalityExponent) *
    //             pow(ProductionQuality, blueprint.QualityExponent) *
    //             // Applying exponents to two random numbers and averaging them produces a range of interesting probability distributions for quality
    //             (pow(Context.Random.NextFloat(), blueprint.RandomQualityExponent) +
    //              pow(Context.Random.NextFloat(), blueprint.RandomQualityExponent)) / 2, ItemName);
    //     }
    //     
        return false;
    }
    //
    // // TODO Update Factory Persistence
    // public PersistentBehaviorData Store()
    // {
    //     return new FactoryPersistence
    //     {
    //         Blueprint = _blueprint,
    //         RetoolingTime = RetoolingTime,
    //         AssignedPopulation = AssignedPopulation,
    //         ProductionQuality = ProductionQuality
    //     };
    // }
    //
    // public void Restore(PersistentBehaviorData data)
    // {
    //     var factoryPersistence = data as FactoryPersistence;
    //     _blueprint = factoryPersistence.Blueprint;
    //     RetoolingTime = factoryPersistence.RetoolingTime;
    //     AssignedPopulation = factoryPersistence.AssignedPopulation;
    //     ProductionQuality = factoryPersistence.ProductionQuality;
    // }
    //
    // public Guid Build(BlueprintData blueprint, float quality, string name, bool direct = false)
    // {
    //     if (direct)
    //     {
    //         if(blueprint.FactoryItem!=Guid.Empty)
    //             throw new ArgumentException("Attempted to directly build a blueprint which requires a factory!");
    //         var blueprintItem = Context.Cache.Get<GearData>(blueprint.Item);
    //         if(blueprintItem == null)
    //             throw new ArgumentException("Attempted to directly build a blueprint for a missing or incompatible item!");
    //     }
    //
    //     //var newItemID = Guid.Empty;
    //
    //     // GetBlueprintIngredients will assign the output lists with the items matching the blueprint, and return false if they are not found
    //     if (!GetBlueprintIngredients(blueprint, out var simpleIngredients, out var compoundIngredients))
    //         return Guid.Empty;
    //     
    //     // These will be bundled with the data for the compound commodity instance
    //     var ingredients = new List<ItemInstance>();
    //     ingredients.AddRange(compoundIngredients.Select(RemoveCargo));
    //     ingredients.AddRange(simpleIngredients.Select(sc => RemoveCargo(sc, blueprint.Ingredients[sc.Data])));
    //         
    //     var blueprintItemData = Context.Cache.Get(blueprint.Item);
    //     
    //     // Creating new crafted item instances is a bit more complicated
    //     if (blueprintItemData is CraftedItemData)
    //     {
    //         CraftedItemInstance newItem;
    //         
    //         if (blueprintItemData is EquippableItemData equippableItemData)
    //         {
    //             var newGear = new EquippableItem
    //             {
    //                 Context = Context,
    //                 Data = blueprintItemData.ID,
    //                 Ingredients = ingredients.Select(ii=>ii.ID).ToList(),
    //                 Quality = quality,
    //                 Blueprint = blueprint.ID,
    //                 Name = name,
    //                 SourceEntity = ID
    //             };
    //             newGear.Durability = Context.Evaluate(equippableItemData.Durability, newGear);
    //             newItem = newGear;
    //         }
    //         else
    //         {
    //             newItem = new CompoundCommodity
    //             {
    //                 Context = Context,
    //                 Data = blueprintItemData.ID,
    //                 Ingredients = ingredients.Select(ii=>ii.ID).ToList(),
    //                 Quality = quality,
    //                 Blueprint = blueprint.ID,
    //                 Name = name,
    //                 SourceEntity = ID
    //             };
    //         }
    //         Context.Cache.Add(newItem);
    //         if (direct)
    //             IncompleteGear[newItem.ID] = blueprint.ProductionTime;
    //         else 
    //             IncompleteCargo[newItem.ID] = blueprint.ProductionTime;
    //         return newItem.ID;
    //     }
    //
    //     var newSimpleCommodity = new SimpleCommodity
    //     {
    //         Context = Context,
    //         Data = blueprintItemData.ID,
    //         Quantity = 1
    //     };
    //     Context.Cache.Add(newSimpleCommodity);
    //     IncompleteCargo[newSimpleCommodity.ID] = blueprint.ProductionTime;
    //     return newSimpleCommodity.ID;
    // }
    //
    // public bool GetBlueprintIngredients(BlueprintData blueprint, out List<SimpleCommodity> simpleIngredients,
    //     out List<CompoundCommodity> compoundIngredients)
    // {
    //     simpleIngredients = new List<SimpleCommodity>();
    //     compoundIngredients = new List<CompoundCommodity>();
    //     var hasAllIngredients = true;
    //     var cargoInstances = Cargo.Select(c => Context.Cache.Get<ItemInstance>(c));
    //     foreach (var kvp in blueprint.Ingredients)
    //     {
    //         var itemData = Context.Cache.Get(kvp.Key);
    //         if (itemData is SimpleCommodityData)
    //         {
    //             var matchingItem = cargoInstances.FirstOrDefault(ii =>
    //             {
    //                 if (!(ii is SimpleCommodity simpleCommodity)) return false;
    //                 return simpleCommodity.Data == itemData.ID && simpleCommodity.Quantity >= kvp.Value;
    //             }) as SimpleCommodity;
    //             hasAllIngredients = hasAllIngredients && matchingItem != null;
    //             if(matchingItem != null)
    //                 simpleIngredients.Add(matchingItem);
    //         }
    //         else
    //         {
    //             var matchingItems =
    //                 cargoInstances.Where(ii => (ii as CompoundCommodity)?.Data == itemData.ID).Cast<CompoundCommodity>().ToArray();
    //             hasAllIngredients = hasAllIngredients && matchingItems.Length >= kvp.Value;
    //             if(matchingItems.Length >= kvp.Value)
    //                 compoundIngredients.AddRange(matchingItems.Take(kvp.Value));
    //         }
    //     }
    //
    //     return hasAllIngredients;
    // }
}

[MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class FactoryPersistence : PersistentBehaviorData
{
    [JsonProperty("blueprint"), Key(0)] public Guid Blueprint;
    [JsonProperty("retoolingTime"), Key(2)] public double RetoolingTime;
    [JsonProperty("assignedPopulation"), Key(5)] public int AssignedPopulation;
    [JsonProperty("productionQuality"), Key(6)] public float ProductionQuality;
}