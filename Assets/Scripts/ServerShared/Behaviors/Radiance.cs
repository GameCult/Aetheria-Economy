using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn), RuntimeInspectable]
public class RadianceData : BehaviorData
{
    [InspectableField, JsonProperty("radiance"), Key(1), RuntimeInspectable]
    public PerformanceStat Radiance = new PerformanceStat();
    
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new Radiance(context, this, entity, item);
    }
}

public class Radiance : IBehavior
{
    private RadianceData _data;

    private Entity Entity { get; }
    private EquippedItem Item { get; }
    private ItemManager Context { get; }

    public BehaviorData Data => _data;

    public Radiance(ItemManager context, RadianceData data, Entity entity, EquippedItem item)
    {
        _data = data;
        Entity = entity;
        Item = item;
        Context = context;
    }

    public bool Update(float delta)
    {
        // TODO: Light system!
        return true;
    }
}