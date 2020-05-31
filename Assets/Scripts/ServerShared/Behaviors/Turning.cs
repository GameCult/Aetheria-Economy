using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, JsonObject(MemberSerialization.OptIn), RuntimeInspectable]
public class TurningData : BehaviorData
{
    [InspectableField, JsonProperty("torque"), Key(1), RuntimeInspectable]  
    public PerformanceStat Torque = new PerformanceStat();

    [InspectableField, JsonProperty("visibility"), Key(2)]
    public PerformanceStat Visibility = new PerformanceStat();

    [InspectableField, JsonProperty("heat"), Key(3)]  
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
    
    public float Torque { get; private set; }

    public BehaviorData Data => _data;
    
    private TurningData _data;
    
    private float _input;

    public Turning(GameContext context, TurningData data, Entity entity, Gear item)
    {
        Context = context;
        _data = data;
        Entity = entity;
        Item = item;
    }
    
    public void SetAxis(float value)
    {
        _input = clamp(value, -1, 1);
    }

    public void Initialize()
    {
    }

    public bool Update(float delta)
    {
        Torque = Context.Evaluate(_data.Torque, Item, Entity);
        Entity.Direction = mul(Entity.Direction, Unity.Mathematics.float2x2.Rotate(_input * Torque / Entity.Mass * delta));
        Entity.AddHeat(abs(_input) * Context.Evaluate(_data.Heat, Item, Entity) * delta);
        Entity.VisibilitySources[this] = abs(_input) * Context.Evaluate(_data.Visibility, Item, Entity);
        return true;
    }

    public void Remove()
    {
    }
}