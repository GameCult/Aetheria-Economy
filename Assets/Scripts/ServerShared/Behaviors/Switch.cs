using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn), Order(-25)]
public class SwitchData : BehaviorData
{
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new Switch(context, this, entity, item);
    }
}

public class Switch : IBehavior
{
    private SwitchData _data;

    private Entity Entity { get; }
    private Gear Item { get; }
    private GameContext Context { get; }

    public BehaviorData Data => _data;

    private bool Activated { get; set; }

    public Switch(GameContext context, SwitchData data, Entity entity, Gear item)
    {
        _data = data;
        Entity = entity;
        Item = item;
        Context = context;
    }

    public bool Update(float delta)
    {
        return Activated;
    }
}