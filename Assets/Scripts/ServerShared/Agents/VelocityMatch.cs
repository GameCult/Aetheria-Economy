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
    private Thruster _thrust;
    private Turning _turning;
    private const float TargetThreshold = .1f;

    private GameContext _context;

    public VelocityMatch(GameContext context, Entity entity, ControllerData controllerData) : base(context, entity, controllerData)
    {
        _context = context;
        _thrust = Entity.Axes.Keys.FirstOrDefault(x => x is Thruster) as Thruster;
        _turning = Entity.Axes.Keys.FirstOrDefault(x => x is Turning) as Turning;
    }
    
    public override void Update(float delta)
    {
        if (TargetOrbit != Guid.Empty && _thrust != null && _turning != null)
        {
            var deltaV = _context.GetOrbitVelocity(TargetOrbit) - Entity.Velocity;
            if(length(deltaV) < TargetThreshold)
                OnMatch?.Invoke();
            Entity.Axes[_turning] = TurningInput(deltaV);
            Entity.Axes[_thrust] = ThrustInput(deltaV);
        }
    }
    
    public float2 MatchDistanceTime
    {
        get
        {
            var velocity = length(Entity.Velocity);
            var deltaV = _context.GetOrbitVelocity(TargetOrbit) - Entity.Velocity;
            
            var stoppingTime = length(deltaV) / (_thrust.Thrust / Entity.Mass);
            var stoppingDistance = stoppingTime * (velocity / 2);
            
            var angleDiff = Entity.Direction.AngleDiff(deltaV);
            var turnaroundTime = angleDiff / (_turning.Torque / Entity.Mass);
            var turnaroundDistance = turnaroundTime * velocity;
            
            return float2(stoppingDistance + turnaroundDistance, stoppingTime + turnaroundTime);
        }
    }
}
