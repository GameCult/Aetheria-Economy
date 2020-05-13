using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn), Order(-20)]
public class TriggerData : BehaviorData
{
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new Trigger(context, this, entity, item);
    }
}

public class Trigger : IBehavior
{
    private TriggerData _data;

    private Entity Entity { get; }
    private Gear Item { get; }
    private GameContext Context { get; }

    public BehaviorData Data => _data;

    public bool _pulled;

    public void Pull()
    {
        _pulled = true;
    }

    public Trigger(GameContext context, TriggerData data, Entity entity, Gear item)
    {
        _data = data;
        Entity = entity;
        Item = item;
        Context = context;
    }

    public void Initialize()
    {
    }

    public bool Update(float delta)
    {
        if (_pulled)
        {
            _pulled = false;
            return true;
        }

        return false;
    }
}