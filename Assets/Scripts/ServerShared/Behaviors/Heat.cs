using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class HeatData : BehaviorData
{
    [InspectableField, JsonProperty("heat"), Key(1)]
    public PerformanceStat Heat = new PerformanceStat();
    
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new Heat(context, this, entity, item);
    }
}

[UpdateOrder(10)]
public class Heat : IBehavior
{
    private HeatData _data;

    private Entity Entity { get; }
    private Gear Item { get; }
    private GameContext Context { get; }

    public BehaviorData Data => _data;

    public Heat(GameContext context, HeatData data, Entity entity, Gear item)
    {
        _data = data;
        Entity = entity;
        Item = item;
        Context = context;
    }

    public bool Update(float delta)
    {
        Entity.AddHeat(Context.Evaluate(_data.Heat, Item, Entity));
        return true;
    }
}