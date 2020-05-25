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

public class TowingController : IBehavior, IPersistentBehavior, IController, IInitializableBehavior
{
    public TaskType TaskType => TaskType.Tow;
    public bool Available => _towingTask != Guid.Empty;
    public Guid Zone => _entity.Zone;
    public BehaviorData Data => _data;
    
    private TowingControllerData _data;
    private GameContext _context;
    private Entity _entity;
    private Gear _item;
    private Guid _towingTask;
    private List<SimplifiedZoneData> _path;
    private TowingPhase _towingPhase = TowingPhase.Pickup;
    private MovementPhase _movementPhase = MovementPhase.Locomotion;
    private Locomotion _locomotion;
    private VelocityMatch _velocityMatch;
    
    public TowingController(GameContext context, TowingControllerData data, Entity entity, Gear item)
    {
        _context = context;
        _data = data;
        _entity = entity;
        _item = item;
    }
    
    public void Initialize()
    {
        _locomotion = new Locomotion(_context, _entity, _data);
        _velocityMatch = new VelocityMatch(_context, _entity, _data);
    }

    public bool Update(float delta)
    {
        var towingTask = _context.Cache.Get<StationTowing>(_towingTask);
        if (towingTask == null)
            return false;
        
        if (_path.Any())
        {
            _locomotion.Update(delta);

            if (length(_entity.Position - _locomotion.Objective) < _context.GlobalData.WarpDistance)
            {
                _context.Warp(_entity, _path[0].ZoneID);
                _path.RemoveAt(0);
                if(_path.Any())
                    SetWormholeDestination();
            }
        }
        else
        {
            var destination = float2(0);
            var velocity = float2(0);
            if (_towingPhase == TowingPhase.Pickup)
            {
                destination = _context.ZoneEntities[_entity.Zone][towingTask.Station].Position;
                velocity = _context.ZoneEntities[_entity.Zone][towingTask.Station].Velocity;
            }
            else
            {
                var orbitParent = _context.GetOrbitPosition(towingTask.OrbitParent);
                var parentToUs = _entity.Position - orbitParent;
                var nearestPointInOrbit = orbitParent + normalize(parentToUs) * towingTask.OrbitDistance;
                destination = nearestPointInOrbit;
                velocity = _context.GetOrbitVelocity(towingTask.OrbitParent);
            }

            _velocityMatch.TargetVelocity = velocity;
            if (_movementPhase == MovementPhase.Locomotion)
            {
                var matchDistanceTime = _velocityMatch.MatchDistanceTime;
                _locomotion.Objective = destination + velocity * matchDistanceTime.y;
                _locomotion.Update(delta);
                
                var distance = length(destination - _entity.Position);
                if (distance < matchDistanceTime.x)
                {
                    _movementPhase = MovementPhase.Slowdown;
                    _velocityMatch.Clear();
                    if (_towingPhase == TowingPhase.Pickup)
                        _velocityMatch.OnMatch += () =>
                        {
                            var target = _context.ZoneEntities[_entity.Zone][towingTask.Station];
                            _context.SetParent(target, _entity);
                            _towingPhase = TowingPhase.Delivery;
                            _movementPhase = MovementPhase.Locomotion;
                        };
                    else
                        _velocityMatch.OnMatch += () =>
                        {
                            var target = _context.ZoneEntities[_entity.Zone][towingTask.Station] as OrbitalEntity;
                            _context.RemoveParent(target);
                            
                            var orbit = _context.CreateOrbit(towingTask.OrbitParent, _entity.Position);
                            target.OrbitData = orbit.ID;
                        };
                }
            }
            else
            {
                _velocityMatch.Update(delta);
            }
        }
        return true;
    }

    public PersistentBehaviorData Store()
    {
        return new TowingControllerPersistence
        {
            TowingTask = _towingTask
        };
    }

    public void Restore(PersistentBehaviorData data)
    {
        var towingControllerPersistence = data as TowingControllerPersistence;
        _towingTask = towingControllerPersistence.TowingTask;
    }

    public void AssignTask(Guid task, List<SimplifiedZoneData> path)
    {
        _towingTask = task;
        _path = path;
        _path.RemoveAt(0);
        if(_path.Any())
            SetWormholeDestination();
    }

    private void SetWormholeDestination()
    {
        _locomotion.Objective = _context.WormholePosition(_entity.Zone, _path[0].ZoneID);
    }
}

public class TowingControllerPersistence : PersistentBehaviorData
{
    [JsonProperty("towingTask"), Key(0)]
    public Guid TowingTask;
}

public enum TowingPhase
{
    Pickup,
    Delivery
}

public enum MovementPhase
{
    Locomotion,
    Slowdown
}