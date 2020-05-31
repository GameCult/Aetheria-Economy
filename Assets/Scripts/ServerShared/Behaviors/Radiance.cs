using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn), RuntimeInspectable]
public class RadianceData : BehaviorData
{
    [InspectableField, JsonProperty("radiance"), Key(1), RuntimeInspectable]
    public PerformanceStat Radiance = new PerformanceStat();
    
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new Radiance(context, this, entity, item);
    }
}

public class Radiance : IBehavior
{
    private RadianceData _data;

    private Entity Entity { get; }
    private Gear Item { get; }
    private GameContext Context { get; }

    public BehaviorData Data => _data;

    public Radiance(GameContext context, RadianceData data, Entity entity, Gear item)
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