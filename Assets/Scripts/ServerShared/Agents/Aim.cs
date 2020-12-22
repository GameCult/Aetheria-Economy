/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

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

    public Aim(ItemManager context, Entity entity, ControllerData controllerData) : base(context, entity, controllerData)
    {
        _velocityLimit = Entity.GetBehaviors<VelocityLimit>().FirstOrDefault();
        _thrust = Entity.GetBehavior<Thruster>();
        _turning = Entity.GetBehavior<Turning>();
    }
    public override void Update(float delta)
    {
        if (_thrust != null && _turning != null)
        {
            float2 diff = Objective - Entity.Position;
            _turning.Axis = TurningInput(diff);
            _thrust.Axis = 0;
        }
    }
}
