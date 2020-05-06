using MessagePack;
using Newtonsoft.Json;

[MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class ReactorData : BehaviorData
{
    [InspectableField, JsonProperty("charge"), Key(0)]  
    public PerformanceStat Charge = new PerformanceStat();

    [InspectableField, JsonProperty("capacitance"), Key(1)]  
    public PerformanceStat Capacitance = new PerformanceStat();

    [InspectableField, JsonProperty("efficiency"), Key(2)]  
    public PerformanceStat Efficiency = new PerformanceStat();

    [InspectableField, JsonProperty("overload"), Key(3)]  
    public PerformanceStat OverloadEfficiency = new PerformanceStat();

    [InspectableField, JsonProperty("underload"), Key(4)]  
    public PerformanceStat UnderloadRecovery = new PerformanceStat();
    
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

    public Reactor(GameContext context, ReactorData data, Entity entity, Gear item)
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
        var cap = Context.Evaluate(_data.Capacitance, Item, Entity);
        var charge = Context.Evaluate(_data.Charge, Item, Entity) * delta;
        var efficiency = Context.Evaluate(_data.Efficiency, Item, Entity);

        Entity.AddHeat(charge / efficiency);
        Entity.Energy += charge;

        if (Entity.Energy > cap)
        {
            Entity.AddHeat(-(Entity.Energy - cap) / efficiency * (1 - 1 / Context.Evaluate(_data.UnderloadRecovery, Item, Entity)));
            Entity.Energy = cap;
        }

        if (Entity.Energy < 0)
        {
            Entity.AddHeat( -Entity.Energy / Context.Evaluate(_data.OverloadEfficiency, Item, Entity));
            Entity.Energy = 0;
        }
    }
}