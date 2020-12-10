using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn), Order(12), RuntimeInspectable]
public class EnergyDrawData : BehaviorData
{
    [InspectableField, JsonProperty("draw"), Key(1), RuntimeInspectable]
    public PerformanceStat Draw = new PerformanceStat();
    
    [InspectableField, JsonProperty("perSecond"), Key(2)]
    public bool PerSecond;
    
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new EnergyDraw(context, this, entity, item);
    }
}

public class EnergyDraw : IBehavior, IOrderedBehavior
{
    private EnergyDrawData _data;

    public int Order => -100;
    private Entity Entity { get; }
    private EquippedItem Item { get; }
    private ItemManager Context { get; }

    public BehaviorData Data => _data;

    public EnergyDraw(ItemManager context, EnergyDrawData data, Entity entity, EquippedItem item)
    {
        _data = data;
        Entity = entity;
        Item = item;
        Context = context;
    }

    public bool Update(float delta)
    {
        Entity.Energy -= Context.Evaluate(_data.Draw, Item.EquippableItem, Entity) * (_data.PerSecond ? delta : 1);
        return true;
    }
}