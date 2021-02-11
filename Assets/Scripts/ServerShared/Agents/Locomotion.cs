/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public class Locomotion : AgentBehavior
{
    public float2 Objective;
    private const float TangentSensitivity = 2;
    
    private VelocityLimit _velocityLimit;
    private Thruster[] _thrusters;
    private float _thrust;

    public Locomotion(ItemManager context, Entity entity, ControllerData controllerData) : base(context, entity, controllerData)
    {
        _velocityLimit = Entity.GetBehavior<VelocityLimit>();
        _thrusters = entity.GetBehaviors<Thruster>()
            .Where(t => t.Item.EquippableItem.Rotation == ItemRotation.Reversed)
            .ToArray();
        _thrust = _thrusters.Sum(t => t.Thrust);
    }

    public override void Update(float delta)
    {
        var diff = Objective - Entity.Position.xz;
        
        // We want to go top speed in the direction of our target
        var topSpeed = _velocityLimit?.Limit ?? 100;
        float2 desiredVelocity = normalize(diff) * topSpeed;
        
        var accelerationTime = length(desiredVelocity - Entity.Velocity) / (_thrust / Entity.Mass);
        var accelerationDistance = accelerationTime * length((desiredVelocity + Entity.Velocity) / 2);

        // We do not have enough space to get to full speed, use approximate maneuvering
        // if (accelerationDistance > length(diff))
        // {
        //     float2 tangential = Entity.Velocity - dot(normalize(diff), normalize(Entity.Velocity)) * Entity.Velocity;
        //     float2 adjustedDirection = normalize(diff) - tangential * TangentSensitivity;
        //     
        //     _turning.Axis = TurningInput(adjustedDirection);
        //     _thrust.Axis = 1;
        // }
        // // We have plenty of space, use exact maneuvering
        // else
        // {
        //     var deltaV = desiredVelocity - Entity.Velocity;
        //     _turning.Axis = TurningInput(deltaV);
        //     _thrust.Axis = ThrustInput(deltaV);
        // }
    }
}
