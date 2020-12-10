using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn), Order(-20)]
public class TriggerData : BehaviorData
{
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new Trigger(context, this, entity, item);
    }
}

public class Trigger : IBehavior
{
    private TriggerData _data;

    private Entity Entity { get; }
    private EquippedItem Item { get; }
    private ItemManager Context { get; }

    public BehaviorData Data => _data;

    public bool _pulled;

    public void Pull()
    {
        _pulled = true;
    }

    public Trigger(ItemManager context, TriggerData data, Entity entity, EquippedItem item)
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