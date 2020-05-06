
using MessagePack;
using Newtonsoft.Json;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class SensorData : BehaviorData
{
    [InspectableField, JsonProperty("radiance"), Key(0)]  
    public PerformanceStat Radiance = new PerformanceStat();

    [InspectableField, JsonProperty("masking"), Key(1)]  
    public PerformanceStat RadianceMasking = new PerformanceStat();

    [InspectableField, JsonProperty("sensitivity"), Key(2)]  
    public PerformanceStat Sensitivity = new PerformanceStat();

    [InspectableField, JsonProperty("range"), Key(3)]  
    public PerformanceStat Range = new PerformanceStat();
    
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new Sensor(context, this, entity, item);
    }
}

public class Sensor : IBehavior
{
    private SensorData _data;

    public Entity Entity { get; }
    public Gear Item { get; }
    public GameContext Context { get; }

    public BehaviorData Data => _data;

    public Sensor(GameContext context, SensorData data, Entity entity, Gear item)
    {
        _data = data;
        Entity = entity;
        Item = item;
        Context = context;
    }

    public void Initialize()
    {
    }

    public void Update(float delta)
    {
        // var ship = Hardpoint.Ship.Ship.transform;
        // var contacts =
        //     Physics.OverlapSphere(ship.position, _data.Range.Evaluate(Hardpoint)).Where(c=>c.attachedRigidbody?.GetComponent<Targetable>()!=null).Select(c=>c.attachedRigidbody.GetComponent<Targetable>());
        // foreach (var contact in contacts)
        // {
        //     var diff = (contact.transform.position - ship.position).Flatland();
        //     var angle = acos(dot(ship.forward.Flatland().normalized,
        //                     diff.normalized)) / (float)PI;
        //     var sens = _data.Sensitivity.Evaluate(Hardpoint) *
        //                Hardpoint.Ship.HullData.VisibilityCurve.Evaluate(saturate(angle));
        //     var vis = contact.Visibility / diff.sqrMagnitude;
        //     if(vis > 1/sens)
        //         Hardpoint.Ship.Contacts[contact] = Time.time;
        // }
        //
        // Hardpoint.Ship.VisibilitySources[this] = _data.Radiance.Evaluate(Hardpoint) / _data.RadianceMasking.Evaluate(Hardpoint);
        // TODO: Handle Active Detection / Visibility From Reflected Radiance
    }
}