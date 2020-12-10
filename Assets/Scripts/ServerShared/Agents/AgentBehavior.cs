using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using static Unity.Mathematics.math;

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
