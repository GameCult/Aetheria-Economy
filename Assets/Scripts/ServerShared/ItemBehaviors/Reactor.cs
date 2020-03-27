using MessagePack;
using Newtonsoft.Json;

[RethinkTable("Items")]
[MessagePackObject]
[JsonObject(MemberSerialization.OptIn)]
public class ReactorBehaviorData : IItemBehaviorData
{
    [InspectableField] [JsonProperty("charge")] [Key(0)]
    public PerformanceStat Charge;

    [InspectableField] [JsonProperty("capacitance")] [Key(1)]
    public PerformanceStat Capacitance;

    [InspectableField] [JsonProperty("efficiency")] [Key(2)]
    public PerformanceStat Efficiency;

    [InspectableField] [JsonProperty("overload")] [Key(3)]
    public PerformanceStat OverloadEfficiency;

    [InspectableField] [JsonProperty("underload")] [Key(4)]
    public PerformanceStat UnderloadRecovery;
    
    public IItemBehavior CreateInstance(GameContext context, Ship ship, Gear item)
    {
        return new ReactorBehavior(context, this, ship, item);
    }
}

public class ReactorBehavior : IItemBehavior
{
    private ReactorBehaviorData _data;

    public Ship Ship { get; }
    public Gear Item { get; }
    public GameContext Context { get; }

    public IItemBehaviorData Data => _data;

    public ReactorBehavior(GameContext context, ReactorBehaviorData data, Ship ship, Gear item)
    {
        Context = context;
        _data = data;
        Ship = ship;
        Item = item;
    }

    public void FixedUpdate(float delta)
    {
        var cap = Context.Evaluate(_data.Capacitance, Item, Ship);
        var charge = Context.Evaluate(_data.Charge, Item, Ship) * delta;
        var efficiency = Context.Evaluate(_data.Efficiency, Item, Ship);

        Ship.AddHeat(charge / efficiency);
        Ship.Charge += charge;

        if (Ship.Charge > cap)
        {
            Ship.AddHeat(-(Ship.Charge - cap) / efficiency * (1 - 1 / Context.Evaluate(_data.UnderloadRecovery, Item, Ship)));
            Ship.Charge = cap;
        }

        if (Ship.Charge < 0)
        {
            Ship.AddHeat( -Ship.Charge / Context.Evaluate(_data.OverloadEfficiency, Item, Ship));
            Ship.Charge = 0;
        }

    }

    public void Update(float delta)
    {
    }
}