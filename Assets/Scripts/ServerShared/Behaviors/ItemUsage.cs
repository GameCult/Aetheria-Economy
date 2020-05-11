
using System;
using MessagePack;
using Newtonsoft.Json;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class ItemUsageData : BehaviorData
{
    [InspectableDatabaseLink(typeof(SimpleCommodityData)), JsonProperty("item"), Key(1)]  
    public Guid Item;

    [InspectableField, JsonProperty("cooldown"), Key(2)]  
    public PerformanceStat Cooldown = new PerformanceStat();
    
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new ItemUsage(context, this, entity, item);
    }
}

public class ItemUsage : IBehavior
{
    private ItemUsageData _data;

    private Entity Entity { get; }
    private Gear Item { get; }
    private GameContext Context { get; }

    public BehaviorData Data => _data;

    public ItemUsage(GameContext context, ItemUsageData data, Entity entity, Gear item)
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
        return true;
    }

    public void Remove()
    {
    }
}