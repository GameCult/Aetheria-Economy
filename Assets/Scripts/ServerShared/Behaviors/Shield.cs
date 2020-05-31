using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, JsonObject(MemberSerialization.OptIn), RuntimeInspectable]
public class ShieldData : BehaviorData
{
    [InspectableField, JsonProperty("efficiency"), Key(1), RuntimeInspectable]  
    public PerformanceStat Efficiency = new PerformanceStat();

    [InspectableField, JsonProperty("shielding"), Key(2), RuntimeInspectable]  
    public PerformanceStat Shielding = new PerformanceStat();
    
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new Shield(context, this, entity, item);
    }
}

public class Shield : IBehavior
{
    public Entity Entity { get; }
    public Gear Item { get; }
    public GameContext Context { get; }

    public BehaviorData Data => _data;
    
    private ShieldData _data;

    public Shield(GameContext context, ShieldData data, Entity entity, Gear item)
    {
        Context = context;
        _data = data;
        Entity = entity;
        Item = item;
        
        // TODO: Detect hits on Ship, keep a list of callbacks that modify the hit by reference
    }

    public bool Update(float delta)
    {
        return true;
    }
}