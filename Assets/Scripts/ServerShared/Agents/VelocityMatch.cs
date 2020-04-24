using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public class VelocityMatch : AgentBehavior
{
    public event Action OnMatch;
    public Entity Objective;
    private Entity _entity;
    private IAnalogBehavior _thrust;
    private IAnalogBehavior _turning;
    private const float TurningSensitivity = 1;
    private const float ThrustSensitivity = 1;
    private const float TargetThreshold = .1f;

    public VelocityMatch(Entity entity)
    {
        _entity = entity;
        _thrust = entity.Axes.Keys.FirstOrDefault(x => x is Thruster);
        _turning = entity.Axes.Keys.FirstOrDefault(x => x is Turning);
    }
    
    public override void Update(float delta)
    {
        if (Objective != null && _thrust != null && _turning != null)
        {
            var deltaV = Objective.Velocity - _entity.Velocity;
            if(length(deltaV) < TargetThreshold)
                OnMatch?.Invoke();
            var angleDiff = _entity.Direction.AngleDiff(deltaV);
            _entity.Axes[_turning] = clamp(-angleDiff * TurningSensitivity, -1, 1);
            _entity.Axes[_thrust] = saturate(length(pow(saturate(dot(normalize(_entity.Direction),normalize(deltaV))), 8)*length(deltaV))*ThrustSensitivity);
        }
    }
}
