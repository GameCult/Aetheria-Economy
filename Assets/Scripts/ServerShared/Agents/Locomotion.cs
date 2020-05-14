﻿using System;
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
    
    private Thruster _thrust;
    private Turning _turning;
    private VelocityLimit _velocityLimit;
    private int _thrustAxis;
    private int _turningAxis;

    public Locomotion(GameContext context, Entity entity, ControllerData controllerData) : base(context, entity, controllerData)
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
            
            // We want to go top speed in the direction of our target
            var topSpeed = _velocityLimit?.Limit ?? 1000;
            float2 desiredVelocity = normalize(diff) * topSpeed;
            
            var accelerationTime = length(desiredVelocity - Entity.Velocity) / (_thrust.Thrust / Entity.Mass);
            var accelerationDistance = accelerationTime * length((desiredVelocity + Entity.Velocity) / 2);

            // We do not have enough space to get to full speed, use approximate maneuvering
            if (accelerationDistance > length(diff))
            {
                float2 tangential = Entity.Velocity - dot(normalize(diff), normalize(Entity.Velocity)) * Entity.Velocity;
                float2 adjustedDirection = normalize(diff) - tangential * TangentSensitivity;
                Entity.Axes[_turningAxis].Value = TurningInput(adjustedDirection);
                Entity.Axes[_thrustAxis].Value = 1;
            }
            // We have plenty of space, use exact maneuvering
            else
            {
                var deltaV = desiredVelocity - Entity.Velocity;
                Entity.Axes[_turningAxis].Value = TurningInput(deltaV);
                Entity.Axes[_thrustAxis].Value = ThrustInput(deltaV);
            }
        }
    }
}