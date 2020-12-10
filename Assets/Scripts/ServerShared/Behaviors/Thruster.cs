using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, JsonObject(MemberSerialization.OptIn), EntityTypeRestriction(HullType.Ship), RuntimeInspectable]
public class ThrusterData : BehaviorData
{
    [InspectableField, JsonProperty("thrust"), Key(1), RuntimeInspectable]  
    public PerformanceStat Thrust = new PerformanceStat();

    [InspectableField, JsonProperty("visibility"), Key(2)]  
    public PerformanceStat Visibility = new PerformanceStat();

    [InspectableField, JsonProperty("heat"), Key(3)]  
    public PerformanceStat Heat = new PerformanceStat();
    
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new Thruster(context, this, entity, item);
    }
}

public class Thruster : IAnalogBehavior
{
    public Entity Entity { get; }
    public EquippedItem Item { get; }
    public ItemManager Context { get; }
    
    public float Thrust { get; private set; }

    public float Axis
    {
        get => _input;
        set => _input = saturate(value);
    }

    public BehaviorData Data => _data;
    
    private ThrusterData _data;
    
    private float _input;

    public Thruster(ItemManager context, ThrusterData data, Entity entity, EquippedItem item)
    {
        Context = context;
        _data = data;
        Entity = entity;
        Item = item;
    }

    public bool Update(float delta)
    {
        Thrust = Context.Evaluate(_data.Thrust, Item.EquippableItem, Entity);
        Entity.Velocity += Entity.Direction * _input * Thrust / Entity.Mass * delta;
        Item.Temperature += (_input * Context.Evaluate(_data.Heat, Item.EquippableItem, Entity) * delta) / Context.GetThermalMass(Item.EquippableItem);
        Entity.VisibilitySources[this] = _input * Context.Evaluate(_data.Visibility, Item.EquippableItem, Entity);
        return true;
    }
}