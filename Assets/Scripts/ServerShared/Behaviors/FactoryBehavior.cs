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
    
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new Factory(context, this, entity, item);
    }
}

public class Factory : IBehavior, IPersistentBehavior, IPopulationAssignment
{
    public float ProductionQuality;
    public double RetoolingTime;
    public string ItemName;
    public Guid ItemUnderConstruction;
    public bool Active;
    public int AssignedPopulation { get; set; }
    
    public Entity Entity { get; }
    public Gear Item { get; }
    public GameContext Context { get; }

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
                    ItemName = Context.Cache.Get<ItemData>(Context.Cache.Get<BlueprintData>(value).Item).Name;
                    RetoolingTime = ToolingTime;
                    _retooling = true;
                }
                Item.Change();
            }
        }
    }
    
    private FactoryData _data;
    
    private Guid _blueprint;
    private bool _retooling = false;
    private float _currentProductionQuality;

    public Factory(GameContext context, FactoryData data, Entity entity, Gear item)
    {
        Context = context;
        _data = data;
        Entity = entity;
        Item = item;
    }

    public bool Update(float delta)
    {
        ToolingTime = Context.Evaluate(_data.ToolingTime, Item, Entity);
        var blueprint = Context.Cache.Get<BlueprintData>(_blueprint);
        if (blueprint == null)
            return false;

        if (ItemUnderConstruction != Guid.Empty)
        {
            Entity.IncompleteCargo[ItemUnderConstruction] =
                Entity.IncompleteCargo[ItemUnderConstruction] - delta * (AssignedPopulation + _data.AutomationPoints) /
                pow(lerp(blueprint.QualityFloor, 1, saturate(_currentProductionQuality)),
                    blueprint.ProductionExponent);
            
            if(Entity.IncompleteCargo[ItemUnderConstruction] < 0)
            {
                var item = Context.Cache.Get<ItemInstance>(ItemUnderConstruction);
                if (item is SimpleCommodity simpleCommodity)
                    Entity.AddCargo(simpleCommodity);
                else if (item is CraftedItemInstance craftedItemInstance)
                    Entity.AddCargo(craftedItemInstance);
                Entity.IncompleteCargo.Remove(ItemUnderConstruction);
                ItemUnderConstruction = Guid.Empty;
                foreach (var attribute in _data.ProductionProfile)
                    Entity.Personality[attribute.Key] = lerp(Entity.Personality[attribute.Key], attribute.Value,
                        Context.GlobalData.ProductionPersonalityLerp * ((float) AssignedPopulation / Entity.Population));
                Item.Change();
            }

            return true;
        }
        
        if (RetoolingTime > 0)
        {
            RetoolingTime -= delta;
            return false;
        }

        if (_retooling)
        {
            _retooling = false;
            Item.Change();
        }

        if (Active)
        {
            var profileDistance = 0f;
            if (_data.ProductionProfile.Any())
                profileDistance = _data.ProductionProfile.Sum(x => abs(x.Value - Entity.Personality[x.Key])) /
                                  _data.ProductionProfile.Count;
            var personalityQuality = 
                (_data.AutomationPoints * _data.AutomationQuality + AssignedPopulation * (1 - profileDistance)) / 
                (_data.AutomationPoints + AssignedPopulation);
            ItemUnderConstruction = Entity.Build(blueprint, 
                blueprint.Quality *
                pow(personalityQuality, blueprint.PersonalityExponent) *
                pow(ProductionQuality, blueprint.QualityExponent) *
                // Applying exponents to two random numbers and averaging them produces a range of interesting probability distributions for quality
                (pow(Context.Random.NextFloat(), blueprint.RandomQualityExponent) +
                 pow(Context.Random.NextFloat(), blueprint.RandomQualityExponent)) / 2, ItemName);
        
            if(ItemUnderConstruction!=Guid.Empty)
                Item.Change();
        }
        
        return false;
    }

    // TODO Update Factory Persistence
    public PersistentBehaviorData Store()
    {
        return new FactoryPersistence
        {
            Blueprint = _blueprint,
            RetoolingTime = RetoolingTime,
            AssignedPopulation = AssignedPopulation,
            ProductionQuality = ProductionQuality
        };
    }

    public void Restore(PersistentBehaviorData data)
    {
        var factoryPersistence = data as FactoryPersistence;
        _blueprint = factoryPersistence.Blueprint;
        RetoolingTime = factoryPersistence.RetoolingTime;
        AssignedPopulation = factoryPersistence.AssignedPopulation;
        ProductionQuality = factoryPersistence.ProductionQuality;
    }
}

[MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class FactoryPersistence : PersistentBehaviorData
{
    [JsonProperty("blueprint"), Key(0)] public Guid Blueprint;
    [JsonProperty("retoolingTime"), Key(2)] public double RetoolingTime;
    [JsonProperty("assignedPopulation"), Key(5)] public int AssignedPopulation;
    [JsonProperty("productionQuality"), Key(6)] public float ProductionQuality;
}