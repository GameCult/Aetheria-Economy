using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public class Aim : AgentBehavior
{
    public float2 Objective;
    
    private Thruster _thrust;
    private Turning _turning;
    private VelocityLimit _velocityLimit;
    private int _thrustAxis;
    private int _turningAxis;

    public Aim(GameContext context, Entity entity, ControllerData controllerData) : base(context, entity, controllerData)
    {
        _velocityLimit = Entity.GetBehaviors<VelocityLimit>().FirstOrDefault();
        _thrust = Entity.GetBehaviors<Thruster>().FirstOrDefault();
        _turning = Entity.GetBehaviors<Turning>().FirstOrDefault();
        _thrustAxis = Entity.GetAxis<Thruster>();
        _turningAxis = Entity.GetAxis<Turning>();
    }
    public override void Update(float delta)
    {
        if (_thrust != null && _turning != null)
        {
            float2 diff = Objective - Entity.Position;
            Entity.Axes[_turningAxis].Value = TurningInput(diff);
            Entity.Axes[_thrustAxis].Value = 0;
        }
    }
}
