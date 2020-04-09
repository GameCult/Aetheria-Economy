using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, JsonObject(MemberSerialization.OptIn), EntityTypeRestriction(typeof(Ship))]
public class ThrusterData : IBehaviorData
{
    [InspectableField, JsonProperty("thrust"), Key(0)]  
    public PerformanceStat Thrust = new PerformanceStat();

    [InspectableField, JsonProperty("torque"), Key(1)]  
    public PerformanceStat Torque = new PerformanceStat();

    [InspectableField, JsonProperty("visibility"), Key(2)]  
    public PerformanceStat Visibility = new PerformanceStat();

    [InspectableField, JsonProperty("heat"), Key(3)]  
    public PerformanceStat Heat = new PerformanceStat();
    
    public IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new Thruster(context, this, entity, item);
    }
}

public class Thruster : IAnalogBehavior
{
    public Entity Entity { get; }
    public Gear Item { get; }
    public GameContext Context { get; }

    public IBehaviorData Data => _data;
    
    private ThrusterData _data;
    
    private float _thrust;

    public Thruster(GameContext context, ThrusterData data, Entity entity, Gear item)
    {
        Context = context;
        _data = data;
        Entity = entity;
        Item = item;
    }
    
    public void SetAxis(float value)
    {
        _thrust = saturate(value);
    }

    public void Initialize()
    {
    }

    public void Update(float delta)
    {
        ((Ship) Entity).Velocity += _thrust * Context.Evaluate(_data.Thrust, Item, Entity) * delta;
        Entity.AddHeat(_thrust * Context.Evaluate(_data.Heat, Item, Entity) * delta);
        Entity.VisibilitySources[this] = _thrust * Context.Evaluate(_data.Visibility, Item, Entity);
    }
}