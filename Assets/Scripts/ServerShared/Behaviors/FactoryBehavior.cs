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
    
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new Factory(context, this, entity, item);
    }
}

public class Factory : IBehavior, IPersistentBehavior
{
    public float ProductionQuality;
    public double RetoolingTime;
    public string ItemName;
    public List<Guid> ItemsUnderConstruction = new List<Guid>();
    
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
                RetoolingTime = ToolingTime;
                _retooling = true;
                OnToolingUpdate?.Invoke();
                OnToolingUpdate = null;
                ItemName = Context.Cache.Get<BlueprintData>(value).Name;
            }
        }
    }

    public event Action OnToolingUpdate;
    
    private FactoryData _data;
    
    private Guid _blueprint;
    private int _assignedPopulation;
    private bool _retooling = false;

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
        
        if (RetoolingTime > 0)
        {
            RetoolingTime -= delta;
            return false;
        }

        if (_retooling)
        {
            _retooling = false;
            OnToolingUpdate?.Invoke();
            OnToolingUpdate = null;
        }

        if (ItemsUnderConstruction.Any())
        {
            var finishedConstruction = false;
            foreach (var item in ItemsUnderConstruction.ToArray())
            {
                Entity.IncompleteCargo[item] =
                    Entity.IncompleteCargo[item] - delta * (_assignedPopulation + _data.AutomationPoints) /
                    pow(lerp(blueprint.QualityFloor, 1, saturate(ProductionQuality)),
                        blueprint.ProductionExponent);
                if(Entity.IncompleteCargo[item] < 0)
                {
                    Entity.Cargo.Add(item);
                    Entity.IncompleteCargo.Remove(item);
                    ItemsUnderConstruction.Remove(item);
                    finishedConstruction = true;
                }
            }
            if(finishedConstruction)
                Entity.RecalculateMass();

            return true;
        }

        // Applying exponents to two random numbers and adding them produces a range of interesting probability distributions for quality
        ItemsUnderConstruction = Entity.Build(blueprint, blueprint.Quality *
            pow(ProductionQuality, blueprint.QualityExponent) *
            (pow(Context.Random.NextFloat(), blueprint.RandomExponent) +
             pow(Context.Random.NextFloat(), blueprint.RandomExponent)) / 2, ItemName);
        if(ItemsUnderConstruction.Any())
            Entity.RecalculateMass(); // TODO: FIX THIS UGLY HACK, needed to update UI
        
        return false;
    }

    public PersistentBehaviorData Store()
    {
        return new FactoryPersistence
        {
            Blueprint = _blueprint,
            RetoolingTime = RetoolingTime,
            AssignedPopulation = _assignedPopulation,
            ProductionQuality = ProductionQuality
        };
    }

    public void Restore(PersistentBehaviorData data)
    {
        var factoryPersistence = data as FactoryPersistence;
        _blueprint = factoryPersistence.Blueprint;
        RetoolingTime = factoryPersistence.RetoolingTime;
        _assignedPopulation = factoryPersistence.AssignedPopulation;
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