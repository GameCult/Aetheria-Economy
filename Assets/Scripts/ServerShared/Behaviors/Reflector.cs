using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, JsonObject(MemberSerialization.OptIn), RuntimeInspectable]
public class ReflectorData : BehaviorData
{
    [InspectableField, JsonProperty("crossSection"), Key(1), RuntimeInspectable]  
    public PerformanceStat CrossSection = new PerformanceStat();

    // [InspectableAnimationCurve, JsonProperty("visibility"), Key(1)]  
    // public float4[] VisibilityCurve;
    
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new Reflector(context, this, entity, item);
    }
}

public class Reflector : IBehavior
{
    public Entity Entity { get; }
    public EquippedItem Item { get; }
    public ItemManager Context { get; }

    public BehaviorData Data => _data;
    
    private ReflectorData _data;

    public Reflector(ItemManager context, ReflectorData data, Entity entity, EquippedItem item)
    {
        Context = context;
        _data = data;
        Entity = entity;
        Item = item;
    }

    public bool Update(float delta)
    {
        // TODO: Light system!
        return true;
    }
}