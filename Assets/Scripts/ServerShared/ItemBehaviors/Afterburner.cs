
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class AfterburnerBehaviorData : IItemBehaviorData
{
    [InspectableField, JsonProperty("thrust"), Key(0)]  
    public PerformanceStat ThrustModifier = new PerformanceStat();

    [InspectableField, JsonProperty("speed"), Key(1)]  
    public PerformanceStat SpeedModifier = new PerformanceStat();

    [InspectableField, JsonProperty("torque"), Key(2)]  
    public PerformanceStat TorqueModifier = new PerformanceStat();
    
    public IItemBehavior CreateInstance(GameContext context, Ship ship, Gear item)
    {
        return new AfterburnerBehavior(context, this, ship, item);
    }
}

public class AfterburnerBehavior : IActivatedItemBehavior
{
    private List<Dictionary<IItemBehavior,float>> _modifiers = new List<Dictionary<IItemBehavior, float>>();
    private AfterburnerBehaviorData _data;
    private ThrusterBehaviorData[] _thrusters;

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

    public void Initialize()
    {
        _thrusters = Ship.GetBehaviorData<ThrusterBehaviorData>().ToArray();
    }

    public void Update(float delta)
    {
    }

    public void FixedUpdate(float delta)
    {
    }
    
    public void Activate()
    {
        if (_thrusters.Length == 0) return;
        
        foreach (var thruster in _thrusters)
        {
            var thrustMod = thruster.Thrust.GetScaleModifiers(Ship);
            thrustMod.Add(this,Context.Evaluate(_data.ThrustModifier,Item, Ship));
            _modifiers.Add(thrustMod);
        }
        
        // var speedMod = (Ship.Hull.ItemData as HullData).TopSpeed.GetScaleModifiers(Ship);
        // _modifiers.Add(speedMod);
        // speedMod.Add(this,Context.Evaluate(_data.SpeedModifier,Item, Ship));
        //
        // var torqueMod = (Ship.GetEquipped(HardpointType.Thruster).ItemData as ThrusterData).Torque.GetScaleModifiers(Ship);
        // _modifiers.Add(torqueMod);
        // torqueMod.Add(this,Context.Evaluate(_data.TorqueModifier,Item, Ship));
        
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