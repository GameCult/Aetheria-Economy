using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn), Order(10)]
public class MiningToolData : BehaviorData
{
    [InspectableField, JsonProperty("dps"), Key(1)]
    public PerformanceStat DamagePerSecond = new PerformanceStat();
    
    [InspectableField, JsonProperty("efficiency"), Key(2)]
    public PerformanceStat Efficiency = new PerformanceStat();
    
    [InspectableField, JsonProperty("penetration"), Key(3)]
    public PerformanceStat Penetration = new PerformanceStat();
    
    [InspectableField, JsonProperty("range"), Key(4)]
    public PerformanceStat Range = new PerformanceStat();
    
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new MiningTool(context, this, entity, item);
    }
}

public class MiningTool : IBehavior
{
    public Guid AsteroidBelt;
    public int Asteroid;
    
    private MiningToolData _data;

    private Entity Entity { get; }
    private Gear Item { get; }
    private GameContext Context { get; }

    public BehaviorData Data => _data;
    public float Range { get; private set; }

    public MiningTool(GameContext context, MiningToolData data, Entity entity, Gear item)
    {
        _data = data;
        Entity = entity;
        Item = item;
        Context = context;
    }

    public bool Update(float delta)
    {
        Range = Context.Evaluate(_data.Range, Item, Entity);
        var asteroidTransform = Context.GetAsteroidTransform(AsteroidBelt, Asteroid);
        if (AsteroidBelt != Guid.Empty && 
            Context.AsteroidExists(AsteroidBelt, Asteroid) && 
            length(Entity.Position - asteroidTransform.xy) - asteroidTransform.w < Range)
        {
            Context.MineAsteroid(
                Entity,
                AsteroidBelt,
                Asteroid,
                Context.Evaluate(_data.DamagePerSecond, Item, Entity) * delta,
                Context.Evaluate(_data.Efficiency, Item, Entity),
                Context.Evaluate(_data.Penetration, Item, Entity));
            return true;
        }

        return false;
    }
}