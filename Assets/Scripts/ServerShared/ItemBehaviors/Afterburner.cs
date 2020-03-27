
using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;

[RethinkTable("Items")]
[InspectableField]
[MessagePackObject]
[JsonObject(MemberSerialization.OptIn)]
public class AfterburnerBehaviorData : IItemBehaviorData
{
    [InspectableField] [JsonProperty("thrust")] [Key(0)]
    public PerformanceStat ThrustModifier;

    [InspectableField] [JsonProperty("speed")] [Key(1)]
    public PerformanceStat SpeedModifier;

    [InspectableField] [JsonProperty("torque")] [Key(2)]
    public PerformanceStat TorqueModifier;
    
    public IItemBehavior CreateInstance(GameContext context, Ship ship, Gear item)
    {
        return new AfterburnerBehavior(context, this, ship, item);
    }
}

public class AfterburnerBehavior : IActivatedItemBehavior
{
    private List<Dictionary<IItemBehavior,float>> _modifiers = new List<Dictionary<IItemBehavior, float>>();
    private AfterburnerBehaviorData _data;

    public Ship Ship { get; }
    public Gear Item { get; }
    public GameContext Context { get; }

    public IItemBehaviorData Data => _data;

    public AfterburnerBehavior(GameContext context, AfterburnerBehaviorData data, Ship ship, Gear item)
    {
        Context = context;
        _data = data;
        Ship = ship;
        Item = item;
    }

    public void Update(float delta)
    {
    }

    public void FixedUpdate(float delta)
    {
    }
    
    public void Activate()
    {
        var thrustMod = Ship.GetEquipped<ThrusterData>().Thrust.GetScaleModifiers(Ship);
        _modifiers.Add(thrustMod);
        thrustMod.Add(this,Context.Evaluate(_data.ThrustModifier,Item, Ship));
        
        var speedMod = (Ship.Hull.ItemData as HullData).TopSpeed.GetScaleModifiers(Ship);
        _modifiers.Add(speedMod);
        speedMod.Add(this,Context.Evaluate(_data.SpeedModifier,Item, Ship));
        
        var torqueMod = (Ship.GetEquipped(HardpointType.Thruster).ItemData as ThrusterData).Torque.GetScaleModifiers(Ship);
        _modifiers.Add(torqueMod);
        torqueMod.Add(this,Context.Evaluate(_data.TorqueModifier,Item, Ship));
        
        Ship.ForceThrust = true;
    }

    public void Deactivate()
    {
        Ship.ForceThrust = false;
        foreach (var mod in _modifiers)
        {
            mod.Remove(this);
        }
        _modifiers.Clear();
    }
}