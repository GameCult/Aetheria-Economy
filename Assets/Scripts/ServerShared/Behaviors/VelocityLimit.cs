using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, JsonObject(MemberSerialization.OptIn), RuntimeInspectable]
public class VelocityLimitData : BehaviorData
{
    [InspectableField, JsonProperty("topSpeed"), Key(1), RuntimeInspectable]  
    public PerformanceStat TopSpeed = new PerformanceStat();
    
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new VelocityLimit(context, this, entity, item);
    }
}

[Order(100)]
public class VelocityLimit : IBehavior
{
    public Entity Entity { get; }
    public Gear Item { get; }
    public GameContext Context { get; }
    
    public float Limit { get; private set; }

    public BehaviorData Data => _data;
    
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

    public bool Update(float delta)
    {
        Limit = Context.Evaluate(_data.TopSpeed, Item, Entity);
        if (length(Entity.Velocity) > Limit)
            Entity.Velocity = normalize(Entity.Velocity) * Limit;
        return true;
    }

    public void Remove()
    {
    }
}