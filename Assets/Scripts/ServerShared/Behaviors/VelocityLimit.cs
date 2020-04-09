using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class VelocityLimitData : IBehaviorData
{
    [InspectableField, JsonProperty("topSpeed"), Key(0)]  
    public PerformanceStat TopSpeed = new PerformanceStat();
    
    public IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new VelocityLimit(context, this, entity, item);
    }
}

public class VelocityLimit : IBehavior
{
    public Entity Entity { get; }
    public Gear Item { get; }
    public GameContext Context { get; }

    public IBehaviorData Data => _data;
    
    private VelocityLimitData _data;

    public VelocityLimit(GameContext context, VelocityLimitData data, Entity entity, Gear item)
    {
        Context = context;
        _data = data;
        Entity = entity;
        Item = item;
    }

    public void Initialize()
    {
    }

    public void Update(float delta)
    {
    }
}