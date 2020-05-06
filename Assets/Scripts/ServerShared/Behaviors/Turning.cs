using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class TurningData : BehaviorData
{
    [InspectableField, JsonProperty("torque"), Key(0)]  
    public PerformanceStat Torque = new PerformanceStat();

    [InspectableField, JsonProperty("visibility"), Key(1)]
    public PerformanceStat Visibility = new PerformanceStat();

    [InspectableField, JsonProperty("heat"), Key(2)]  
    public PerformanceStat Heat = new PerformanceStat();
    
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new Turning(context, this, entity, item);
    }
}

public class Turning : IAnalogBehavior
{
    public Entity Entity { get; }
    public Gear Item { get; }
    public GameContext Context { get; }

    public BehaviorData Data => _data;
    
    private TurningData _data;
    
    private float _turning;

    public Turning(GameContext context, TurningData data, Entity entity, Gear item)
    {
        Context = context;
        _data = data;
        Entity = entity;
        Item = item;
    }
    
    public void SetAxis(float value)
    {
        _turning = clamp(value, -1, 1);
    }

    public void Initialize()
    {
    }

    public void Update(float delta)
    {
        Entity.Direction = mul(Entity.Direction, Unity.Mathematics.float2x2.Rotate(_turning * Context.Evaluate(_data.Torque, Item, Entity) / Entity.Mass * delta));
        Entity.AddHeat(abs(_turning) * Context.Evaluate(_data.Heat, Item, Entity) * delta);
        Entity.VisibilitySources[this] = abs(_turning) * Context.Evaluate(_data.Visibility, Item, Entity);
    }
}