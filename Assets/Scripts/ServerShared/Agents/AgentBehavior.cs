/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RethinkDb.Driver.Ast;
using UniRx;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;

public abstract class AgentBehavior
{
    public abstract void Update(float delta);

    protected float ThrustInput(float2 deltaV) =>
        saturate( // Input must be normalized
            pow( // Apply specificity as exponent to dot product to control how specific the direction needs to be
                saturate( // Discard negative dot product (we're facing away from desired direction)
                    dot(normalize(Entity.Direction), normalize(deltaV))), // Cosine of angle between direction and deltaV
                ControllerData.ThrustSpecificity)
            * length(deltaV) * ControllerData.ThrustSensitivity);
    
    protected float TurningInput(float2 deltaV) => clamp(-Entity.Direction.AngleDiff(deltaV) * ControllerData.TurningSensitivity, -1, 1);

    protected ControllerData ControllerData;
    protected ItemManager Context;
    protected Entity Entity;

    public AgentBehavior(ItemManager context, Entity entity, ControllerData controllerData)
    {
        Context = context;
        Entity = entity;
        ControllerData = controllerData;
    }
}

public class Agent
{
    private BaseState _rootState;
    private BaseState _currentState;
    private Dictionary<BaseState, List<StateTransition>> _stateTransitions = new Dictionary<BaseState, List<StateTransition>>();
    
    public AgentTask Task { get; set; }
    public Ship Ship { get; }
    public ItemManager ItemManager { get; }
    public GameplaySettings Settings { get; }

    public Agent(Ship ship)
    {
        Ship = ship;
        ItemManager = Ship.ItemManager;
        Settings = Ship.ItemManager.GameplaySettings;

        _rootState = _currentState = new BaseState(this);

        var patrolState = new PatrolOrbitsState(this);
        AddTransition(_rootState, patrolState, 
            () => Task is PatrolOrbitsTask, 
            () => patrolState.Task = Task as PatrolOrbitsTask);
        var patrolMoveState = new MoveToOrbitState(this);
        AddTransition(patrolState, patrolMoveState, 
            () => true, 
            () => patrolMoveState.Orbit = patrolState.CurrentTarget);
        AddTransition(patrolMoveState, patrolState, 
            () => patrolMoveState.Distance < 10, 
            () => patrolState.NextTarget());
    }

    public void AddTransition(BaseState source, BaseState targetState, Func<bool> condition, Action onTransition = null)
    {
        if(!_stateTransitions.ContainsKey(source))
            _stateTransitions[source] = new List<StateTransition>();
        _stateTransitions[source].Add(new StateTransition(targetState, condition, onTransition));
    }

    public void Transition(BaseState targetState, Action onTransition = null)
    {
        _currentState.OnExitState();
        _currentState = targetState;
        onTransition?.Invoke();
        _currentState.OnEnterState();
    }

    public void Update(float delta)
    {
        _currentState.Update(delta);
        foreach (var transition in _stateTransitions[_currentState])
            if (transition.Condition())
            {
                Transition(transition.TargetState, transition.OnTransition);
                break;
            }
    }

    public class StateTransition
    {
        public StateTransition(BaseState targetState, Func<bool> condition, Action onTransition)
        {
            TargetState = targetState;
            Condition = condition;
            OnTransition = onTransition;
        }

        public BaseState TargetState { get; }
        public Func<bool> Condition { get; }
        public Action OnTransition { get; }
    }
}

public class BaseState
{
    protected Agent _agent;

    public BaseState(Agent agent)
    {
        _agent = agent;
    }
    public virtual void Update(float delta){}
    public virtual void OnEnterState(){}
    public virtual void OnExitState(){}
}

public abstract class MoveToState : BaseState
{
    private const float FORWARD_DELTA_THRESHOLD = 20;
    private const float THRUST_DELTA_THRESHOLD = 1;
    private VelocityLimit _velocityLimit;
    protected abstract float2 TargetPosition { get; }
    public float Distance { get; private set; }

    protected MoveToState(Agent agent) : base(agent)
    {
        _velocityLimit = agent.Ship.GetBehavior<VelocityLimit>();
    }

    public override void Update(float delta)
    {
        var diff = TargetPosition - _agent.Ship.Position.xz;
        var dir = normalize(diff);
        Distance = length(diff);
        
        // We want to go top speed in the direction of our target
        var topSpeed = _velocityLimit?.Limit ?? 100;
        var desiredVelocity = dir * topSpeed;
        var deltaV = desiredVelocity - _agent.Ship.Velocity;
        var deltaVMag = length(deltaV);
        var deltaVDirection = normalize(deltaV);
        // If Delta V is above the threshold, direct the ship towards the delta and use only main thrusters
        if (deltaVMag > FORWARD_DELTA_THRESHOLD)
        {
            _agent.Ship.LookDirection = float3(deltaVDirection.x, 0, deltaVDirection.y);
            _agent.Ship.MovementDirection = float2(0, pow(dot(_agent.Ship.Direction, deltaVDirection), 2));
        }
        // If Delta V is low, direct the ship towards the target and use all thrusters
        else if(deltaVMag > THRUST_DELTA_THRESHOLD)
        {
            var right = _agent.Ship.Direction.Rotate(ItemRotation.Clockwise);
            _agent.Ship.LookDirection = float3(dir.x, 0, dir.y);
            _agent.Ship.MovementDirection = float2(dot(right, deltaVDirection), dot(_agent.Ship.Direction, deltaVDirection));
        }
        else
        {
            _agent.Ship.LookDirection = float3(dir.x, 0, dir.y);
            _agent.Ship.MovementDirection = float2.zero;
        }
    }
}

public class MoveToEntityState : MoveToState
{
    public Entity TargetEntity { get; set; }
    protected override float2 TargetPosition => TargetEntity?.Position.xz ?? float2.zero;

    public MoveToEntityState(Agent agent) : base(agent) { }
}

public class MoveToOrbitState : MoveToState
{
    public Guid Orbit { get; set; }
    public MoveToOrbitState(Agent agent) : base(agent) { }

    protected override float2 TargetPosition => _agent.Ship.Zone.GetOrbitPosition(Orbit);
}

public class PatrolOrbitsState : BaseState
{
    public PatrolOrbitsTask Task;
    public Guid CurrentTarget
    {
        get => Task.Circuit[_currentTargetIndex];
    }
    private int _currentTargetIndex;
    public void NextTarget()
    {
        _currentTargetIndex++;
        _currentTargetIndex %= Task.Circuit.Length;
    }
    public PatrolOrbitsState(Agent agent) : base(agent) { }
}