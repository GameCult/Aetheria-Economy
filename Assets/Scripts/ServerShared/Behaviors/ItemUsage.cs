
using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class ItemUsageData : BehaviorData
{
    [InspectableDatabaseLink(typeof(SimpleCommodityData)), JsonProperty("item"), Key(1)]  
    public Guid Item;
    
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new ItemUsage(context, this, entity, item);
    }
}

[UpdateOrder(-5)]
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

    public bool Update(float delta)
    {
        var cargoInstances = Entity.Cargo.Select(c => Context.Cache.Get<ItemInstance>(c));
        if (cargoInstances.FirstOrDefault(c => c.Data == _data.Item) is SimpleCommodity item)
        {
            if (item.Quantity > 1)
                item.Quantity--;
            else
            {
                Entity.Cargo.Remove(item.ID);
                Context.Cache.Delete(item);
            }

            return true;
        }
        return false;
    }
}