using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class CooldownData : BehaviorData
{
    [InspectableField, JsonProperty("cooldown"), Key(1)]
    public PerformanceStat Cooldown = new PerformanceStat();
    
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new Cooldown(context, this, entity, item);
    }
}

[UpdateOrder(-10)]
public class Cooldown : IBehavior, IAlwaysUpdatedBehavior
{
    private CooldownData _data;

    private Entity Entity { get; }
    private Gear Item { get; }
    private GameContext Context { get; }

    public BehaviorData Data => _data;

    private float _cooldown; // Normalized

    public Cooldown(GameContext context, CooldownData data, Entity entity, Gear item)
    {
        _data = data;
        Entity = entity;
        Item = item;
        Context = context;
    }

    public bool Update(float delta)
    {
        if (_cooldown < 0)
        {
            _cooldown = 1;
            return true;
        }

        return false;

    }

    public void AlwaysUpdate(float delta)
    {
        _cooldown -= delta / Context.Evaluate(_data.Cooldown, Item, Entity);
    }
}