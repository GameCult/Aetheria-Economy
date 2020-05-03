using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public class Locomotion : AgentBehavior
{
    public float2 Objective;
    private Entity _entity;
    private IAnalogBehavior _thrust;
    private IAnalogBehavior _turning;
    private const float Sensitivity = 1;
    private const float TangentSensitivity = 2;

    public Locomotion(Entity entity)
    {
        _entity = entity;
        _thrust = entity.Axes.Keys.FirstOrDefault(x => x is Thruster);
        _turning = entity.Axes.Keys.FirstOrDefault(x => x is Turning);
    }

    public override void Update(float delta)
    {
        if (_thrust != null && _turning != null)
        {
            float2 diff = Objective - _entity.Position;
            float2 tangential = _entity.Velocity - dot(normalize(diff), normalize(_entity.Velocity)) * _entity.Velocity;
            float angleDiff = _entity.Direction.AngleDiff(normalize(diff) - tangential * TangentSensitivity);
            _entity.Axes[_turning] = clamp(-angleDiff * Sensitivity, -1, 1);
            _entity.Axes[_thrust] = 1;
        }
    }
}
