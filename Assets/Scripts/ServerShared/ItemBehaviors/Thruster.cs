using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class ThrusterBehaviorData : IItemBehaviorData
{
    [InspectableField, JsonProperty("thrust"), Key(0)]  
    public PerformanceStat Thrust = new PerformanceStat();

    [InspectableField, JsonProperty("torque"), Key(1)]  
    public PerformanceStat Torque = new PerformanceStat();

    [InspectableField, JsonProperty("visibility"), Key(2)]  
    public PerformanceStat Visibility = new PerformanceStat();

    [InspectableField, JsonProperty("heat"), Key(3)]  
    public PerformanceStat Heat = new PerformanceStat();
    
    public IItemBehavior CreateInstance(GameContext context, Ship ship, Gear item)
    {
        return new ThrusterBehavior(context, this, ship, item);
    }
}

public class ThrusterBehavior : IAnalogItemBehavior
{
    public Ship Ship { get; }
    public Gear Item { get; }
    public GameContext Context { get; }

    public IItemBehaviorData Data => _data;
    
    private ThrusterBehaviorData _data;
    
    private float _thrust;

    public ThrusterBehavior(GameContext context, ThrusterBehaviorData data, Ship ship, Gear item)
    {
        Context = context;
        _data = data;
        Ship = ship;
        Item = item;
    }
    
    public void SetAxis(float value)
    {
        _thrust = saturate(value);
    }

    public void FixedUpdate(float delta)
    {

    }

    public void Initialize()
    {
    }

    public void Update(float delta)
    {
        Ship.Velocity += _thrust * Context.Evaluate(_data.Thrust, Item, Ship) * delta;
        Ship.AddHeat(_thrust * Context.Evaluate(_data.Heat, Item, Ship) * delta);
        Ship.VisibilitySources[this] = _thrust * Context.Evaluate(_data.Visibility, Item, Ship);
    }
}