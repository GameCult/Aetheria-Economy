using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn), Order(-22), RuntimeInspectable]
public class ThermotoggleData : BehaviorData
{
    [TemperatureInspectable, JsonProperty("targetTemp"), Key(1), RuntimeInspectable]
    public float TargetTemperature;
    
    [InspectableField, JsonProperty("perSecond"), Key(2)]
    public bool HighPass;
    
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new Thermotoggle(context, this, entity, item);
    }
}

public class Thermotoggle : IBehavior
{
    private ThermotoggleData _data;

    private Entity Entity { get; }
    private Gear Item { get; }
    private GameContext Context { get; }

    public BehaviorData Data => _data;

    public Thermotoggle(GameContext context, ThermotoggleData data, Entity entity, Gear item)
    {
        _data = data;
        Entity = entity;
        Item = item;
        Context = context;
    }

    public bool Update(float delta)
    {
        return Entity.Temperature < _data.TargetTemperature ^ _data.HighPass;
    }
}