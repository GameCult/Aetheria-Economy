using MessagePack;
using Newtonsoft.Json;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class ActivatedItemUsageData : BehaviorData
{
    
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new ActivatedItemUsage(context, this, entity, item);
    }
}

public class ActivatedItemUsage : IActivatedBehavior
{
    private ActivatedItemUsageData _data;

    private Entity Entity { get; }
    private Gear Item { get; }
    private GameContext Context { get; }

    public BehaviorData Data => _data;

    public ActivatedItemUsage(GameContext context, ActivatedItemUsageData data, Entity entity, Gear item)
    {
        _data = data;
        Entity = entity;
        Item = item;
        Context = context;
    }

    public void Initialize()
    {
    }

    public bool Activate()
    {
        throw new System.NotImplementedException();
    }

    public void Deactivate()
    {
        throw new System.NotImplementedException();
    }

    public bool Update(float delta)
    {
        return true;
    }

    public void Remove()
    {
    }
}