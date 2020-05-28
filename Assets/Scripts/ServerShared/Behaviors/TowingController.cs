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
                MoveTo(towingTask.Zone, () =>
                {
                    _context.Log($"Towing Controller {_entity.Name} has entered pickup phase.");
                    MoveTo(_context.Cache.Get<Entity>(towingTask.Station), true, () =>
                    {
                        var target = _context.ZoneEntities[_entity.Zone][towingTask.Station] as OrbitalEntity;
                        _context.SetParent(target, _entity);
                        _context.Log($"Towing Controller {_entity.Name} has entered delivery phase.");
                        MoveTo(() =>
                        {
                            var orbitParent = _context.GetOrbitPosition(towingTask.OrbitParent);
                            var parentToUs = _entity.Position - orbitParent;
                            return orbitParent + normalize(parentToUs) * towingTask.OrbitDistance;
                        }, () => _context.GetOrbitVelocity(towingTask.OrbitParent), () =>
                        {
                            _context.RemoveParent(target);
                            _context.Log($"Towing Controller {_entity.Name} has delivered the target. Returning Home.");
                            
                            var orbit = _context.CreateOrbit(towingTask.OrbitParent, _entity.Position);
                            target.OrbitData = orbit.ID;
                            
                            FinishTask();
                            _taskStarted = false;
                        });
                    });
                });
                _taskStarted = true;
            }
        }
        return base.Update(delta);
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