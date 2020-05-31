using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn), Order(10), Inspectable]
public class HeatData : BehaviorData
{
    [InspectableField, JsonProperty("heat"), Key(1), RuntimeInspectable]
    public PerformanceStat Heat = new PerformanceStat();
    
    [InspectableField, JsonProperty("perSecond"), Key(2)]
    public bool PerSecond;
    
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new Heat(context, this, entity, item);
    }
}

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
        Entity.AddHeat(Context.Evaluate(_data.Heat, Item, Entity) * (_data.PerSecond ? delta : 1));
        return true;
    }
}