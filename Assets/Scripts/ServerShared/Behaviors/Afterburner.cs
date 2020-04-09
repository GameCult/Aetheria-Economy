
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class AfterburnerData : IBehaviorData
{
    [InspectableField, JsonProperty("thrust"), Key(0)]  
    public PerformanceStat ThrustModifier = new PerformanceStat();

    [InspectableField, JsonProperty("speed"), Key(1)]  
    public PerformanceStat SpeedModifier = new PerformanceStat();

    [InspectableField, JsonProperty("torque"), Key(2)]  
    public PerformanceStat TorqueModifier = new PerformanceStat();
    
    public IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new Afterburner(context, this, entity, item);
    }
}

public class Afterburner : IActivatedBehavior
{
    private List<Dictionary<IBehavior,float>> _modifiers = new List<Dictionary<IBehavior, float>>();
    private AfterburnerData _data;
    private Thruster _thruster;

    public Entity Entity { get; }
    public Gear Item { get; }
    public GameContext Context { get; }

    public IBehaviorData Data => _data;

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
    }

    public void Update(float delta)
    {
    }
    
    public void Activate()
    {
        if (_thruster == null) return;
        
        var thrustMod = ((ThrusterData) _thruster.Data).Thrust.GetScaleModifiers(Entity);
        thrustMod.Add(this,Context.Evaluate(_data.ThrustModifier,Item, Entity));
        _modifiers.Add(thrustMod);

        Entity.AxisOverrides[_thruster] = 1;
        
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
        Entity.AxisOverrides.Remove(_thruster);
        foreach (var mod in _modifiers)
        {
            mod.Remove(this);
        }
        _modifiers.Clear();
    }
}