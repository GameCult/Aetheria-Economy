using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class TurningBehaviorData : IItemBehaviorData
{
    [InspectableField, JsonProperty("torque"), Key(0)]  
    public PerformanceStat Torque = new PerformanceStat();

    [InspectableField, JsonProperty("visibility"), Key(1)]
    public PerformanceStat Visibility = new PerformanceStat();

    [InspectableField, JsonProperty("heat"), Key(2)]  
    public PerformanceStat Heat = new PerformanceStat();
    
    public IItemBehavior CreateInstance(GameContext context, Ship ship, Gear item)
    {
        return new TurningBehavior(context, this, ship, item);
    }
}

public class TurningBehavior : IAnalogItemBehavior
{
    public Ship Ship { get; }
    public Gear Item { get; }
    public GameContext Context { get; }

    public IItemBehaviorData Data => _data;
    
    private TurningBehaviorData _data;
    
    private float _turning;

    public TurningBehavior(GameContext context, TurningBehaviorData data, Ship ship, Gear item)
    {
        Context = context;
        _data = data;
        Ship = ship;
        Item = item;
    }
    
    public void SetAxis(float value)
    {
        _turning = clamp(value, -1, 1);
    }

    public void FixedUpdate(float delta)
    {

    }

    public void Initialize()
    {
    }

    public void Update(float delta)
    {
        Ship.Direction = mul(Ship.Direction, Unity.Mathematics.float2x2.Rotate(_turning * Context.Evaluate(_data.Torque, Item, Ship) * delta));
        Ship.AddHeat(abs(_turning) * Context.Evaluate(_data.Heat, Item, Ship) * delta);
        Ship.VisibilitySources[this] = abs(_turning) * Context.Evaluate(_data.Visibility, Item, Ship);
    }
}