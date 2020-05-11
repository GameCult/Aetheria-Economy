
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class AfterburnerData : BehaviorData
{
    [InspectableField, JsonProperty("thrust"), Key(1)]  
    public PerformanceStat ThrustModifier = new PerformanceStat();

    [InspectableField, JsonProperty("speed"), Key(2)]  
    public PerformanceStat SpeedModifier = new PerformanceStat();

    [InspectableField, JsonProperty("torque"), Key(3)]  
    public PerformanceStat TorqueModifier = new PerformanceStat();
    
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new Afterburner(context, this, entity, item);
    }
}

public class Afterburner : IActivatedBehavior
{
    private List<Dictionary<IBehavior,float>> _modifiers = new List<Dictionary<IBehavior, float>>();
    private AfterburnerData _data;
    private Thruster _thruster;
    private int _thrustAxis;

    public Entity Entity { get; }
    public Gear Item { get; }
    public GameContext Context { get; }

    public BehaviorData Data => _data;

    public Afterburner(GameContext context, AfterburnerData data, Entity entity, Gear item)
    {
        Context = context;
        _data = data;
        Entity = entity;
        Item = item;
    }

    public void Initialize()
    {
        _thruster = Entity.GetBehaviors<Thruster>().FirstOrDefault();
        _thrustAxis = Entity.GetAxis<Thruster>();
    }

    public bool Update(float delta)
    {
        return true;
    }
    
    public bool Activate()
    {
        if (_thruster == null) return false;
        
        var thrustMod = ((ThrusterData) _thruster.Data).Thrust.GetScaleModifiers(Entity);
        thrustMod.Add(this,Context.Evaluate(_data.ThrustModifier,Item, Entity));
        _modifiers.Add(thrustMod);

        Entity.AxisOverrides[_thrustAxis] = 1;
        
        return true;
        
        // var speedMod = (Ship.Hull.ItemData as HullData).TopSpeed.GetScaleModifiers(Ship);
        // _modifiers.Add(speedMod);
        // speedMod.Add(this,Context.Evaluate(_data.SpeedModifier,Item, Ship));
        //
        // var torqueMod = (Ship.GetEquipped(HardpointType.Thruster).ItemData as ThrusterData).Torque.GetScaleModifiers(Ship);
        // _modifiers.Add(torqueMod);
        // torqueMod.Add(this,Context.Evaluate(_data.TorqueModifier,Item, Ship));
    }

    public void Deactivate()
    {
        Entity.AxisOverrides.Remove(_thrustAxis);
        foreach (var mod in _modifiers)
        {
            mod.Remove(this);
        }
        _modifiers.Clear();
    }

    public void Remove()
    {
    }
}