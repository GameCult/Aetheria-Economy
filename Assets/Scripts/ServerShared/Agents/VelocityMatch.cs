using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public class VelocityMatch : AgentBehavior
{
    public event Action OnMatch;
    public float2 TargetVelocity;
    private Thruster _thrust;
    private Turning _turning;
    private const float TargetThreshold = .1f;
    private int _thrustAxis;
    private int _turningAxis;

    private GameContext _context;

    public VelocityMatch(GameContext context, Entity entity, ControllerData controllerData) : base(context, entity, controllerData)
    {
        _context = context;
        _thrust = Entity.GetBehaviors<Thruster>().FirstOrDefault();
        _turning = Entity.GetBehaviors<Turning>().FirstOrDefault();
        _thrustAxis = Entity.GetAxis<Thruster>();
        _turningAxis = Entity.GetAxis<Turning>();
    }

    public void Clear()
    {
        OnMatch = null;
    }
    
    public override void Update(float delta)
    {
        if (_thrust != null && _turning != null)
        {
            var deltaV = TargetVelocity - Entity.Velocity;
            if(length(deltaV) < TargetThreshold)
                OnMatch?.Invoke();
            Entity.Axes[_turningAxis].Value = TurningInput(deltaV);
            Entity.Axes[_thrustAxis].Value = ThrustInput(deltaV);
        }
    }
    
    public float2 MatchDistanceTime
    {
        get
        {
            var velocity = length(Entity.Velocity);
            var deltaV = TargetVelocity - Entity.Velocity;
            
            var stoppingTime = length(deltaV) / (_thrust.Thrust / Entity.Mass);
            var stoppingDistance = stoppingTime * (velocity / 2);
            
            var angleDiff = Entity.Direction.AngleDiff(deltaV);
            var turnaroundTime = angleDiff / (_turning.Torque / Entity.Mass);
            var turnaroundDistance = turnaroundTime * velocity;
            
            return float2(stoppingDistance + turnaroundDistance, stoppingTime + turnaroundTime);
        }
    }
}
