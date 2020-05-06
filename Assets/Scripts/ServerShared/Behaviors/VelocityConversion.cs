using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class VelocityConversionData : BehaviorData
{
    [InspectableField, JsonProperty("traction"), Key(0)]  
    public PerformanceStat Traction = new PerformanceStat();
    
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new VelocityConversion(context, this, entity, item);
    }
}

public class VelocityConversion : IBehavior
{
    public Entity Entity { get; }
    public Gear Item { get; }
    public GameContext Context { get; }

    public BehaviorData Data => _data;
    
    private VelocityConversionData _data;

    public VelocityConversion(GameContext context, VelocityConversionData data, Entity entity, Gear item)
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