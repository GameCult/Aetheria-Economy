using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class RadiatorData : BehaviorData
{
    [InspectableField, JsonProperty("emissivity"), Key(1)]  
    public PerformanceStat Emissivity = new PerformanceStat();
    
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new Radiator(context, this, entity, item);
    }
}

public class Radiator : IBehavior
{
    public Entity Entity { get; }
    public Gear Item { get; }
    public GameContext Context { get; }

    public BehaviorData Data => _data;
    
    private RadiatorData _data;

    public Radiator(GameContext context, RadiatorData data, Entity entity, Gear item)
    {
        Context = context;
        _data = data;
        Entity = entity;
        Item = item;
    }

    public bool Update(float delta)
    {
        var rad = pow(Entity.Temperature, Context.GlobalData.HeatRadiationPower) * Context.GlobalData.HeatRadiationMultiplier;
        Entity.Temperature -= rad * delta;
        Entity.VisibilitySources[this] = rad;
        return true;
    }
}