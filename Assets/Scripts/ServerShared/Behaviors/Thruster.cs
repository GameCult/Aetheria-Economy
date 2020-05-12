using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, JsonObject(MemberSerialization.OptIn), EntityTypeRestriction(HullType.Ship)]
public class ThrusterData : BehaviorData
{
    [InspectableField, JsonProperty("thrust"), Key(1)]  
    public PerformanceStat Thrust = new PerformanceStat();

    [InspectableField, JsonProperty("visibility"), Key(2)]  
    public PerformanceStat Visibility = new PerformanceStat();

    [InspectableField, JsonProperty("heat"), Key(3)]  
    public PerformanceStat Heat = new PerformanceStat();
    
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new Thruster(context, this, entity, item);
    }
}

public class Thruster : IAnalogBehavior
{
    public Entity Entity { get; }
    public Gear Item { get; }
    public GameContext Context { get; }
    
    public float Thrust { get; private set; }

    public BehaviorData Data => _data;
    
    private ThrusterData _data;
    
    private float _input;

    public Thruster(GameContext context, ThrusterData data, Entity entity, Gear item)
    {
        Context = context;
        _data = data;
        Entity = entity;
        Item = item;
    }
    
    public void SetAxis(float value)
    {
        _input = saturate(value);
    }

    public bool Update(float delta)
    {
        Thrust = Context.Evaluate(_data.Thrust, Item, Entity);
        Entity.Velocity += Entity.Direction * _input * Thrust / Entity.Mass * delta;
        Entity.AddHeat(_input * Context.Evaluate(_data.Heat, Item, Entity) * delta);
        Entity.VisibilitySources[this] = _input * Context.Evaluate(_data.Visibility, Item, Entity);
        return true;
    }
}