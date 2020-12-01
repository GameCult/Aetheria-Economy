using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, JsonObject(MemberSerialization.OptIn), EntityTypeRestriction(HullType.Ship), Order(-100)]
public class TowingControllerData : ControllerData
{
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new TowingController(context, this, entity, item);
    }
}

public class TowingController : ControllerBase, IBehavior, IPersistentBehavior
{
    public override TaskType TaskType => TaskType.Tow;
    public BehaviorData Data => _data;
    
    private TowingControllerData _data;
    private GameContext _context;
    private Entity _entity;
    private Gear _item;
    private bool _taskStarted;
    
    public TowingController(GameContext context, TowingControllerData data, Entity entity, Gear item) : base(context, data, entity)
    {
        _context = context;
        _data = data;
        _entity = entity;
        _item = item;
    }

    public new bool Update(float delta)
    {
        var towingTask = _context.Cache.Get<StationTowing>(Task);
        if (towingTask != null)
        {
            if (!_taskStarted)
            {
                MoveTo(towingTask.Zone, OnZoneArrival);
                _taskStarted = true;
            }
        }
        return base.Update(delta);
    }
    
    void OnZoneArrival()
    {
        var towingTask = _context.Cache.Get<StationTowing>(Task);
        
        _entity.SetMessage("Entering pickup phase.");
        
        MoveTo(_context.Cache.Get<Entity>(towingTask.Station), true, OnPickup);
    }
    
    void OnPickup()
    {
        var towingTask = _context.Cache.Get<StationTowing>(Task);
        var target = Zone.Entities[towingTask.Station] as OrbitalEntity;
        
        target.SetParent(_entity);
        
        _entity.SetMessage("Entering delivery phase.");

        MoveTo(() =>
        {
            var orbitParent = Zone.GetOrbitPosition(towingTask.OrbitParent);
            var parentToUs = _entity.Position - orbitParent;
            return orbitParent + normalize(parentToUs) * towingTask.OrbitDistance;
        }, () => Zone.GetOrbitVelocity(towingTask.OrbitParent), OnDelivery);
    }
    
    void OnDelivery()
    {
        var towingTask = _context.Cache.Get<StationTowing>(Task);
        var target = Zone.Entities[towingTask.Station] as OrbitalEntity;
        
        target.RemoveParent();
        
        _entity.SetMessage("Target delivered. Returning Home.");

        var orbit = Zone.CreateOrbit(towingTask.OrbitParent, _entity.Position);
        target.OrbitData = orbit.ID;

        FinishTask();
        _taskStarted = false;
    }

    public PersistentBehaviorData Store()
    {
        return new TowingControllerPersistence
        {
            Task = Task
        };
    }

    public void Restore(PersistentBehaviorData data)
    {
        var towingControllerPersistence = data as TowingControllerPersistence;
        Task = towingControllerPersistence.Task;
    }
}

public class TowingControllerPersistence : PersistentBehaviorData
{
    [JsonProperty("task"), Key(0)]
    public Guid Task;
}