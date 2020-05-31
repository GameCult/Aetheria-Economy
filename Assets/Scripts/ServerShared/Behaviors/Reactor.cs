using MessagePack;
using Newtonsoft.Json;

[MessagePackObject, JsonObject(MemberSerialization.OptIn), RuntimeInspectable]
public class ReactorData : BehaviorData
{
    [InspectableField, JsonProperty("charge"), Key(1), RuntimeInspectable]  
    public PerformanceStat Charge = new PerformanceStat();

    [InspectableField, JsonProperty("capacitance"), Key(2), RuntimeInspectable]  
    public PerformanceStat Capacitance = new PerformanceStat();

    [InspectableField, JsonProperty("efficiency"), Key(3), RuntimeInspectable]  
    public PerformanceStat Efficiency = new PerformanceStat();

    [InspectableField, JsonProperty("overload"), Key(4), RuntimeInspectable]  
    public PerformanceStat OverloadEfficiency = new PerformanceStat();

    [InspectableField, JsonProperty("underload"), Key(5), RuntimeInspectable]  
    public PerformanceStat ThrottlingFactor = new PerformanceStat();
    
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new Reactor(context, this, entity, item);
    }
}

public class Reactor : IBehavior
{
    private ReactorData _data;

    public Entity Entity { get; }
    public Gear Item { get; }
    public GameContext Context { get; }

    public BehaviorData Data => _data;
    public float Capacitance;

    public Reactor(GameContext context, ReactorData data, Entity entity, Gear item)
    {
        Context = context;
        _data = data;
        Entity = entity;
        Item = item;
    }

    public bool Update(float delta)
    {
        Capacitance = Context.Evaluate(_data.Capacitance, Item, Entity);
        var charge = Context.Evaluate(_data.Charge, Item, Entity) * delta;
        var efficiency = Context.Evaluate(_data.Efficiency, Item, Entity);

        Entity.AddHeat(charge / efficiency);
        Entity.Energy += charge;

        if (Entity.Energy > Capacitance)
        {
            Entity.AddHeat(-(Entity.Energy - Capacitance) / efficiency * (1 - 1 / Context.Evaluate(_data.ThrottlingFactor, Item, Entity)));
            Entity.Energy = Capacitance;
        }

        if (Entity.Energy < 0)
        {
            Entity.AddHeat( -Entity.Energy / Context.Evaluate(_data.OverloadEfficiency, Item, Entity));
            Entity.Energy = 0;
        }
        return true;
    }
}