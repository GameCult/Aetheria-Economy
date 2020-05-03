using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public class VelocityMatch : AgentBehavior
{
    public event Action OnMatch;
    public Guid TargetOrbit;
    private Entity _entity;
    private IAnalogBehavior _thrust;
    private IAnalogBehavior _turning;
    private const float TurningSensitivity = 1;
    private const float ThrustSensitivity = 1;
    private const float TargetThreshold = .1f;
    
    public GameContext Context { get; }

    public VelocityMatch(GameContext context, Entity entity)
    {
        Context = context;
        _entity = entity;
        _thrust = entity.Axes.Keys.FirstOrDefault(x => x is Thruster);
        _turning = entity.Axes.Keys.FirstOrDefault(x => x is Turning);
    }
    
    public override void Update(float delta)
    {
        if (TargetOrbit != Guid.Empty && _thrust != null && _turning != null)
        {
            var deltaV = Context.GetOrbitVelocity(TargetOrbit) - _entity.Velocity;
            if(length(deltaV) < TargetThreshold)
                OnMatch?.Invoke();
            var angleDiff = _entity.Direction.AngleDiff(deltaV);
            _entity.Axes[_turning] = clamp(-angleDiff * TurningSensitivity, -1, 1);
            _entity.Axes[_thrust] = saturate(length(pow(saturate(dot(normalize(_entity.Direction),normalize(deltaV))), 8)*length(deltaV))*ThrustSensitivity);
        }
    }
    
    public float2 MatchDistanceTime
    {
        get
        {
            var velocity = length(_entity.Velocity);
            var deltaV = Context.GetOrbitVelocity(TargetOrbit) - _entity.Velocity;
            
            var stoppingTime = length(deltaV) / (_entity.Context.Evaluate((_thrust.Data as ThrusterData).Thrust, _thrust.Item, _entity) / _entity.Mass);
            var stoppingDistance = stoppingTime * (velocity / 2);
            
            var angleDiff = _entity.Direction.AngleDiff(deltaV);
            var turnaroundTime = angleDiff / (_entity.Context.Evaluate((_turning.Data as TurningData).Torque, _turning.Item, _entity) / _entity.Mass);
            var turnaroundDistance = turnaroundTime * velocity;
            
            return float2(stoppingDistance + turnaroundDistance, stoppingTime + turnaroundTime);
        }
    }
}
