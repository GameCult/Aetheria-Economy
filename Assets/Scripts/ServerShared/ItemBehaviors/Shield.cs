using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class ShieldBehaviorData : IItemBehaviorData
{
    [InspectableField, JsonProperty("efficiency"), Key(0)]  
    public PerformanceStat Efficiency = new PerformanceStat();

    [InspectableField, JsonProperty("shielding"), Key(1)]  
    public PerformanceStat Shielding = new PerformanceStat();
    
    public IItemBehavior CreateInstance(GameContext context, Ship ship, Gear item)
    {
        return new ShieldBehavior(context, this, ship, item);
    }
}

public class ShieldBehavior : IItemBehavior
{
    public Ship Ship { get; }
    public Gear Item { get; }
    public GameContext Context { get; }

    public IItemBehaviorData Data => _data;
    
    private ShieldBehaviorData _data;

    public ShieldBehavior(GameContext context, ShieldBehaviorData data, Ship ship, Gear item)
    {
        Context = context;
        _data = data;
        Ship = ship;
        Item = item;
        
        // TODO: Detect hits on Ship, keep a list of callbacks that modify the hit by reference
    }

    public void FixedUpdate(float delta)
    {
    }

    public void Initialize()
    {
    }

    public void Update(float delta)
    {
    }
}