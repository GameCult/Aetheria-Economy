using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class ReflectorData : BehaviorData
{
    [InspectableField, JsonProperty("crossSection"), Key(1)]  
    public PerformanceStat CrossSection = new PerformanceStat();

    // [InspectableAnimationCurve, JsonProperty("visibility"), Key(1)]  
    // public float4[] VisibilityCurve;
    
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new Reflector(context, this, entity, item);
    }
}

public class Reflector : IBehavior
{
    public Entity Entity { get; }
    public Gear Item { get; }
    public GameContext Context { get; }

    public BehaviorData Data => _data;
    
    private ReflectorData _data;

    public Reflector(GameContext context, ReflectorData data, Entity entity, Gear item)
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
        return true;
    }

    public void Remove()
    {
    }
}