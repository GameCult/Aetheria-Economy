using System;
using System.Collections;
using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;

[Union(0, typeof(TowingControllerData))]
public abstract class ControllerData : BehaviorData
{
    [InspectableField, JsonProperty("thrustSensitivity"), Key(1)]
    public float ThrustSensitivity = 1;
    
    [InspectableField, JsonProperty("thrustSpecificity"), Key(2)]
    public float ThrustSpecificity = 8;
    
    [InspectableField, JsonProperty("turningSensitivity"), Key(3)]
    public float TurningSensitivity = 1;
    
    [InspectableField, JsonProperty("tangentSensitivity"), Key(4)]
    public float TangentSensitivity = 4;
    
    [InspectableField, JsonProperty("targetDistance"), Key(5)]  
    public float TargetDistance = 10;
}

public abstract class ControllerBase : IBehavior, IController, IInitializableBehavior
{
    protected Guid HomeEntity => (_entity as Ship)?.HomeEntity ?? Guid.Empty;
    public bool Available => _spaceworthy && Task == Guid.Empty;
    public abstract TaskType TaskType { get; }
    public Zone Zone => _entity.Zone;
    public BehaviorData Data => _controllerData;
    
    protected Locomotion Locomotion;
    protected VelocityMatch VelocityMatch;
    protected Aim Aim;
    protected Func<float2> TargetPosition;
    protected Func<float2> TargetVelocity;
    protected bool MatchVelocity;
    protected List<ZoneDefinition> Path;
    protected Guid Task;
    protected bool Moving;
    protected bool Waiting;
    
    private GameContext _context;
    private Entity _entity;
    private ControllerData _controllerData;
    private MovementPhase _movementPhase;
    private Action _onFinishMoving;
    private bool _spaceworthy;
    private float _waitTime;
    private Action _onFinishWaiting;

    public ControllerBase(GameContext context, ControllerData data, Entity entity)
    {
        _context = context;
        _controllerData = data;
        _entity = entity;
        entity.GearEvent.OnChanged += () =>
        {
            _spaceworthy = entity.GetBehavior<Thruster>() != null && entity.GetBehavior<Reactor>() != null;
            if(_spaceworthy)
                CreateBehaviors();
        };
    }

    public void CreateBehaviors()
    {
        Locomotion = new Locomotion(_context, _entity, _controllerData);
        VelocityMatch = new VelocityMatch(_context, _entity, _controllerData);
        Aim = new Aim(_context, _entity, _controllerData);
    }
    
    public void Initialize()
    {
        CreateBehaviors();
        _context.CorporationControllers[_entity.Corporation].Add(this);
    }
    
    public void AssignTask(Guid task)
    {
        Task = task;
        Moving = false;
    }

    public bool Update(float delta)
    {
        if (Available)
        {
            // No task is assigned and we're at home, do nothing!
            if (_entity.Parent == HomeEntity)
                return false;
            
            // No task is assigned, but we're not home, go home!
            if (!Moving)
            {
                GoHome();
            }
        }
        
        if (Waiting)
        {
            _waitTime -= delta;
            if (_waitTime < 0)
            {
                Waiting = false;
                _onFinishWaiting?.Invoke();
            }

            return true;
        }

        if (Moving)
        {
            if(_entity.Parent!=Guid.Empty)
                _entity.RemoveParent();

            var targetPosition = TargetPosition();
            var distance = length(targetPosition - _entity.Position);
            if (MatchVelocity)
            {
                var targetVelocity = TargetVelocity();
                VelocityMatch.TargetVelocity = targetVelocity;
                if (_movementPhase == MovementPhase.Locomotion)
                {
                    var matchDistanceTime = VelocityMatch.MatchDistanceTime;
                    Locomotion.Objective = targetPosition + targetVelocity * matchDistanceTime.y;
                    Locomotion.Update(delta);
                        
                    if (distance < matchDistanceTime.x)
                    {
                        _movementPhase = MovementPhase.Slowdown;
                        //_context.Log($"Controller {_entity.Name} has entered slowdown phase.");
                        VelocityMatch.Clear();
                        VelocityMatch.OnMatch += () =>
                        {
                            Moving = false;
                            _onFinishMoving?.Invoke();
                        };
                    } 
                }
                else
                {
                    VelocityMatch.TargetVelocity = targetVelocity;
                    VelocityMatch.Update(delta);
                }
            }
            else
            {
                Locomotion.Objective = TargetPosition();
                Locomotion.Update(delta);
                if (distance < _controllerData.TargetDistance)
                {
                    Moving = false;
                    _onFinishMoving();
                }
            }
        }

        return true;
    }

    public void FinishTask()
    {
        if (Task == Guid.Empty)
            return;
        
        var task = _context.Cache.Get(Task);
        _context.Cache.Delete(task);
        _context.Cache.Get<Corporation>(_entity.Corporation).Tasks.Remove(task.ID);
        Task = Guid.Empty;
    }

    public void GoHome(Action onFinish = null)
    {
        var homeEntity = _context.Cache.Get<Entity>(HomeEntity);
        if (Zone == homeEntity.Zone) MoveTo(homeEntity, true, () =>
        {
            _entity.SetParent(homeEntity);
            onFinish?.Invoke();
        });
        else MoveTo(homeEntity.Zone.Data.ID, () => MoveTo(homeEntity, true, () =>
        {
            _entity.SetParent(homeEntity);
            onFinish?.Invoke();
        }));
    }

    public void Wait(float time, Action onFinish = null)
    {
        _waitTime = time;
        Waiting = true;
        _onFinishWaiting = onFinish;
    }

    public void MoveTo(Entity entity, bool matchVelocity = true, Action onFinish = null)
    {
        TargetPosition = () => entity.Position;
        TargetVelocity = () => entity.Velocity;
        MatchVelocity = matchVelocity;
        _movementPhase = MovementPhase.Locomotion;
        Moving = true;
        _onFinishMoving = onFinish;
    }

    public void MoveTo(float2 position, bool matchVelocity = true, Action onFinish = null)
    {
        TargetPosition = () => position;
        TargetVelocity = () => float2.zero;
        MatchVelocity = matchVelocity;
        _movementPhase = MovementPhase.Locomotion;
        Moving = true;
        _onFinishMoving = onFinish;
    }

    public void MoveTo(Func<float2> position, Func<float2> velocity, Action onFinish = null)
    {
        TargetPosition = position;
        TargetVelocity = velocity;
        MatchVelocity = true;
        _movementPhase = MovementPhase.Locomotion;
        Moving = true;
        _onFinishMoving = onFinish;
    }

    public void MoveTo(Guid zone, Action onFinish = null)
    {
        var path = _context.FindPath(_context.GalaxyZones[Zone.Data.ID], _context.GalaxyZones[zone]);

        void OnNextZone()
        {
            path.RemoveAt(0);
            if (path.Count > 0)
            {
                MoveTo(Zone.WormholePosition(Path[0]), false, OnNextZone);
            }
            else
            {
                onFinish?.Invoke();
            }
        }
        
        OnNextZone();
    }
}

public enum MovementPhase
{
    Locomotion,
    Slowdown
}