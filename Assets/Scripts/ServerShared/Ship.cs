/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;

[MessagePackObject]
public class Ship : Entity
{
    
    // [Key("bindings")]   public Dictionary<KeyCode,Guid>    Bindings = new Dictionary<KeyCode,Guid>();
    //[IgnoreMember] public int HullHardpointCount;

    // [IgnoreMember] public Dictionary<Targetable, float> Contacts = new Dictionary<Targetable, float>();
    // [IgnoreMember] public Targetable Target;
    public Entity HomeEntity;
    public float2 MovementDirection;
    
    private Thruster[] _allThrusters;
    private Thruster[] _forwardThrusters;
    private Thruster[] _reverseThrusters;
    private Thruster[] _rightThrusters;
    private Thruster[] _leftThrusters;
    private Thruster[] _clockwiseThrusters;
    private Thruster[] _counterClockwiseThrusters;
    
    public quaternion Rotation { get; private set; }

    protected override void OnActivate()
    {
        _allThrusters = GetBehaviors<Thruster>().ToArray();
        _forwardThrusters = _allThrusters.Where(x => x.Item.EquippableItem.Rotation == ItemRotation.Reversed).ToArray();
        _reverseThrusters = _allThrusters.Where(x => x.Item.EquippableItem.Rotation == ItemRotation.None).ToArray();
        _rightThrusters = _allThrusters.Where(x => x.Item.EquippableItem.Rotation == ItemRotation.CounterClockwise).ToArray();
        _leftThrusters = _allThrusters.Where(x => x.Item.EquippableItem.Rotation == ItemRotation.Clockwise).ToArray();
        _counterClockwiseThrusters = _allThrusters.Where(x => x.Torque < -ItemManager.GameplaySettings.TorqueFloor).ToArray();
        _clockwiseThrusters = _allThrusters.Where(x => x.Torque > ItemManager.GameplaySettings.TorqueFloor).ToArray();
    }

    public Ship(ItemManager itemManager, Zone zone, EquippableItem hull) : base(itemManager, zone, hull)
    {
    }

    public override void Update(float delta)
    {
        Position.xz += Velocity * delta;
        if (_active)
        {
            foreach (var thruster in _allThrusters) thruster.Axis = 0;
            foreach (var thruster in _rightThrusters) thruster.Axis += MovementDirection.x;
            foreach (var thruster in _leftThrusters) thruster.Axis += -MovementDirection.x;
            foreach (var thruster in _forwardThrusters) thruster.Axis += MovementDirection.y;
            foreach (var thruster in _reverseThrusters) thruster.Axis += -MovementDirection.y;

            var look = normalize(LookDirection.xz);
            var deltaRot = dot(look, normalize(Direction).Rotate(ItemRotation.Clockwise));
            if (abs(deltaRot) < .01f)
            {
                deltaRot = 0;
                Direction = lerp(Direction, look, min(delta, 1));
            }
            deltaRot = pow(abs(deltaRot), .75f) * sign(deltaRot);
        
            foreach (var thruster in _clockwiseThrusters) thruster.Axis += deltaRot;
            foreach (var thruster in _counterClockwiseThrusters) thruster.Axis += -deltaRot;
        }
        var normal = Zone.GetNormal(Position.xz);
        var force = new float2(normal.x, normal.z);
        var forceMagnitude = lengthsq(force);
        if (forceMagnitude > .001f)
        {
            var fa = 1 / (1 - forceMagnitude) - 1;
            Velocity += normalize(force) * Zone.Settings.GravityStrength * fa;
        }
        var shipRight = Direction.Rotate(ItemRotation.Clockwise);
        var forward = cross(float3(shipRight.x, 0, shipRight.y), normal);
        Rotation = quaternion.LookRotation(forward, normal);
        
        base.Update(delta);
    }
}

