using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public class Locomotion : AgentBehavior
{
    public Entity Objective;
    private Entity _entity;
    private IAnalogBehavior _thrust;
    private IAnalogBehavior _turning;
    private const float Sensitivity = 1;

    public Locomotion(Entity entity)
    {
        _entity = entity;
        _thrust = entity.Axes.Keys.FirstOrDefault(x => x is Thruster);
        _turning = entity.Axes.Keys.FirstOrDefault(x => x is Turning);
    }

    public override void Update(float delta)
    {
        if (Objective != null && _thrust != null && _turning != null)
        {
            float2 diff = Objective.Position - _entity.Position;
            var directionAngle = atan2(diff.y, diff.x) / PI;
            var currentAngle = atan2(_entity.Direction.y, _entity.Direction.x) / PI;
            var d1 = directionAngle - currentAngle;
            var d2 = directionAngle - 2 - currentAngle;
            var d3 = directionAngle + 2 - currentAngle;
            var angleDiff = abs(d1) < abs(d2) ? abs(d1) < abs(d3) ? d1 : d3 : abs(d2) < abs(d3) ? d2 : d3;
            _entity.Axes[_turning] = clamp(-angleDiff * Sensitivity, -1, 1);
            _entity.Axes[_thrust] = 1;
        }
    }
}
