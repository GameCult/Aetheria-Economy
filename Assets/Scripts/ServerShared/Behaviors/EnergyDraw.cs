using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn), Order(12)]
public class EnergyDrawData : BehaviorData
{
    [InspectableField, JsonProperty("draw"), Key(1)]
    public PerformanceStat Draw = new PerformanceStat();
    
    [InspectableField, JsonProperty("perSecond"), Key(2)]
    public bool PerSecond;
    
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new EnergyDraw(context, this, entity, item);
    }
}

public class EnergyDraw : IBehavior
{
    private EnergyDrawData _data;

    private Entity Entity { get; }
    private Gear Item { get; }
    private GameContext Context { get; }

    public BehaviorData Data => _data;

    public EnergyDraw(GameContext context, EnergyDrawData data, Entity entity, Gear item)
    {
        _data = data;
        Entity = entity;
        Item = item;
        Context = context;
    }

    public bool Update(float delta)
    {
        Entity.Energy -= Context.Evaluate(_data.Draw, Item, Entity) * (_data.PerSecond ? delta : 1);
        return true;
    }
}