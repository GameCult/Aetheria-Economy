using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public class Locomotion : AgentBehavior
{
    public float2 Objective;
    private const float Sensitivity = 1;
    private const float TangentSensitivity = 2;
    
    private GameContext _context;
    private Guid _entity;
    private IAnalogBehavior _thrust;
    private IAnalogBehavior _turning;

    public Locomotion(GameContext context, Guid entity)
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
        if (_thrust != null && _turning != null)
        {
            float2 diff = Objective - entity.Position;
            float2 tangential = entity.Velocity - dot(normalize(diff), normalize(entity.Velocity)) * entity.Velocity;
            float angleDiff = entity.Direction.AngleDiff(normalize(diff) - tangential * TangentSensitivity);
            entity.Axes[_turning] = clamp(-angleDiff * Sensitivity, -1, 1);
            entity.Axes[_thrust] = 1;
        }
    }
}
