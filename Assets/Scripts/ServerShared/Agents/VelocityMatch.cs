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
    private Guid _entity;
    private IAnalogBehavior _thrust;
    private IAnalogBehavior _turning;
    private const float TurningSensitivity = 1;
    private const float ThrustSensitivity = 1;
    private const float TargetThreshold = .1f;

    private GameContext _context;

    public VelocityMatch(GameContext context, Guid entity)
    {
        _context = context;
        _entity = entity;
        var entityObject = _context.Cache.Get<Entity>(_entity);
        _thrust = entityObject.Axes.Keys.FirstOrDefault(x => x is Thruster);
        _turning = entityObject.Axes.Keys.FirstOrDefault(x => x is Turning);
    }
    
    public override void Update(float delta)
    {
        var entity = _context.Cache.Get<Entity>(_entity);
        if (TargetOrbit != Guid.Empty && _thrust != null && _turning != null)
        {
            var deltaV = _context.GetOrbitVelocity(TargetOrbit) - entity.Velocity;
            if(length(deltaV) < TargetThreshold)
                OnMatch?.Invoke();
            var angleDiff = entity.Direction.AngleDiff(deltaV);
            entity.Axes[_turning] = clamp(-angleDiff * TurningSensitivity, -1, 1);
            entity.Axes[_thrust] = saturate(length(pow(saturate(dot(normalize(entity.Direction),normalize(deltaV))), 8)*length(deltaV))*ThrustSensitivity);
        }
    }
    
    public float2 MatchDistanceTime
    {
        get
        {
            var entity = _context.Cache.Get<Entity>(_entity);
            var velocity = length(entity.Velocity);
            var deltaV = _context.GetOrbitVelocity(TargetOrbit) - entity.Velocity;
            
            var stoppingTime = length(deltaV) / (_context.Evaluate((_thrust.Data as ThrusterData).Thrust, _thrust.Item, entity) / entity.Mass);
            var stoppingDistance = stoppingTime * (velocity / 2);
            
            var angleDiff = entity.Direction.AngleDiff(deltaV);
            var turnaroundTime = angleDiff / (_context.Evaluate((_turning.Data as TurningData).Torque, _turning.Item, entity) / entity.Mass);
            var turnaroundDistance = turnaroundTime * velocity;
            
            return float2(stoppingDistance + turnaroundDistance, stoppingTime + turnaroundTime);
        }
    }
}
