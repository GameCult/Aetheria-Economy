/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;
using UniRx;
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

public abstract class ControllerBase<T> : IBehavior, IController<T>, IInitializableBehavior where T : AgentTask
{
    protected Entity HomeEntity => (Entity as Ship)?.HomeEntity;
    public bool Available => _spaceworthy && Task == null;
    //public abstract TaskType TaskType { get; }
    public Zone Zone => Entity.Zone;
    public BehaviorData Data => _controllerData;
    
    protected ItemManager ItemManager { get; }
    protected Entity Entity { get; }
    
    protected Locomotion Locomotion;
    protected VelocityMatch VelocityMatch;
    protected Func<float2> TargetPosition;
    protected Func<float2> TargetVelocity;
    protected bool MatchVelocity;
    // protected List<ZoneDefinition> Path;
    protected T Task;
    protected bool Moving;
    protected bool Waiting;
    
    private ControllerData _controllerData;
    private MovementPhase _movementPhase;
    private Action _onFinishMoving;
    private bool _spaceworthy;
    private float _waitTime;
    private Action _onFinishWaiting;

    public ControllerBase(ItemManager itemManager, ControllerData data, Entity entity)
    {
        ItemManager = itemManager;
        Entity = entity;
        _controllerData = data;
        CheckSpaceworthiness();
        entity.Equipment.ObserveAdd().Subscribe(added => CheckSpaceworthiness());
    }

    private void CheckSpaceworthiness()
    {
        _spaceworthy = Entity.GetBehavior<Thruster>() != null && Entity.GetBehavior<Reactor>() != null;
        if(_spaceworthy)
            CreateBehaviors();
    }

    public void CreateBehaviors()
    {
        Locomotion = new Locomotion(ItemManager, Entity, _controllerData);
        VelocityMatch = new VelocityMatch(ItemManager, Entity, _controllerData);
    }
    
    public void Initialize()
    {
        CreateBehaviors();
        // _context.CorporationControllers[_entity.Corporation].Add(this);
    }
    
    public void AssignTask(T task)
    {
        Task = task;
        Moving = false;
    }

    public bool Execute(float delta)
    {
        if (Available)
        {
            // No task is assigned and we're at home, do nothing!
            if (Entity.Parent == HomeEntity)
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
            if(Entity.Parent!=null)
                Entity.RemoveParent();

            var targetPosition = TargetPosition();
            var distance = length(targetPosition - Entity.Position.xz);
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
        if (Task == null)
            return;
        
        Task = null;
    }

    public void GoHome(Action onFinish = null)
    {
        MoveTo(HomeEntity, true, () =>
        {
            Entity.SetParent(HomeEntity);
            onFinish?.Invoke();
        });
        // else MoveTo(homeEntity.Zone.Data.ID, () => MoveTo(homeEntity, true, () =>
        // {
        //     _entity.SetParent(homeEntity);
        //     onFinish?.Invoke();
        // }));
    }

    public void Wait(float time, Action onFinish = null)
    {
        _waitTime = time;
        Waiting = true;
        _onFinishWaiting = onFinish;
    }

    public void MoveTo(Entity entity, bool matchVelocity = true, Action onFinish = null)
    {
        TargetPosition = () => entity.Position.xz;
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

    // public void MoveTo(Guid zone, Action onFinish = null)
    // {
    //     var path = _context.FindPath(_context.GalaxyZones[Zone.Data.ID], _context.GalaxyZones[zone]);
    //
    //     void OnNextZone()
    //     {
    //         path.RemoveAt(0);
    //         if (path.Count > 0)
    //         {
    //             MoveTo(Zone.WormholePosition(Path[0]), false, OnNextZone);
    //         }
    //         else
    //         {
    //             onFinish?.Invoke();
    //         }
    //     }
    //     
    //     OnNextZone();
    // }
}

public enum MovementPhase
{
    Locomotion,
    Slowdown
}