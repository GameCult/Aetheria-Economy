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
using UniRx;
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
    private HashSet<Thruster> _forwardThrusters;
    private HashSet<Thruster> _reverseThrusters;
    private HashSet<Thruster> _rightThrusters;
    private HashSet<Thruster> _leftThrusters;
    private HashSet<Thruster> _clockwiseThrusters;
    private HashSet<Thruster> _counterClockwiseThrusters;
    
    public float ForwardThrust { get; private set; }
    public float ReverseThrust { get; private set; }
    public float LeftStrafeThrust { get; private set; }
    public float RightStrafeThrust { get; private set; }
    public float ClockwiseTorque { get; private set; }
    public float CounterClockwiseTorque { get; private set; }

    public float TurnTime(float2 direction)
    {
        var angleDiff = Direction.Angle(normalize(direction));
        var clockwise = dot(direction, Direction.Rotate(ItemRotation.Clockwise)) > 0;
        return angleDiff / ((clockwise ? ClockwiseTorque : CounterClockwiseTorque) / Mass);
    }
    
    public quaternion Rotation { get; private set; }

    public override void Activate()
    {
        base.Activate();
        
        _allThrusters = GetBehaviors<Thruster>().ToArray();
        
        _forwardThrusters = new HashSet<Thruster>(_allThrusters
            .Where(x => x.Item.EquippableItem.Rotation == ItemRotation.Reversed));
        RecalculateForwardThrust();
        
        _reverseThrusters = new HashSet<Thruster>(_allThrusters
            .Where(x => x.Item.EquippableItem.Rotation == ItemRotation.None));
        RecalculateReverseThrust();
        
        _rightThrusters = new HashSet<Thruster>(_allThrusters
            .Where(x => x.Item.EquippableItem.Rotation == ItemRotation.CounterClockwise));
        RecalculateRightStrafeThrust();
        
        _leftThrusters = new HashSet<Thruster>(_allThrusters
            .Where(x => x.Item.EquippableItem.Rotation == ItemRotation.Clockwise));
        RecalculateLeftStrafeThrust();
        
        _counterClockwiseThrusters = new HashSet<Thruster>(_allThrusters
            .Where(x => x.Torque < -ItemManager.GameplaySettings.TorqueFloor));
        RecalculateCounterClockwiseTorque();
        
        _clockwiseThrusters = new HashSet<Thruster>(_allThrusters
            .Where(x => x.Torque > ItemManager.GameplaySettings.TorqueFloor));
        RecalculateClockwiseTorque();
    }

    public Ship(ItemManager itemManager, Zone zone, EquippableItem hull) : base(itemManager, zone, hull)
    {
        ItemDamage.Subscribe(x =>
        {
            if (Hardpoints[x.item.Position.x, x.item.Position.y]?.Type == HardpointType.Thruster)
            {
                foreach (var behavior in x.item.Behaviors)
                {
                    if (behavior is Thruster thruster)
                    {
                        if(_forwardThrusters.Contains(thruster)) RecalculateForwardThrust();
                        if(_reverseThrusters.Contains(thruster)) RecalculateReverseThrust();
                        if(_rightThrusters.Contains(thruster)) RecalculateRightStrafeThrust();
                        if(_leftThrusters.Contains(thruster)) RecalculateLeftStrafeThrust();
                        if(_clockwiseThrusters.Contains(thruster)) RecalculateClockwiseTorque();
                        if(_counterClockwiseThrusters.Contains(thruster)) RecalculateCounterClockwiseTorque();
                    }
                }
                if(x.item.EquippableItem.Rotation == ItemRotation.Reversed)
                    RecalculateForwardThrust();
            }
        });
    }

    private void RecalculateForwardThrust()
    {
        ForwardThrust = 0;
        foreach (var thruster in _forwardThrusters)
        {
            ForwardThrust += thruster.Thrust;
        }
    }

    private void RecalculateReverseThrust()
    {
        ReverseThrust = 0;
        foreach (var thruster in _reverseThrusters)
        {
            ReverseThrust += thruster.Thrust;
        }
    }

    private void RecalculateLeftStrafeThrust()
    {
        LeftStrafeThrust = 0;
        foreach (var thruster in _leftThrusters)
        {
            LeftStrafeThrust += thruster.Thrust;
        }
    }

    private void RecalculateRightStrafeThrust()
    {
        RightStrafeThrust = 0;
        foreach (var thruster in _rightThrusters)
        {
            RightStrafeThrust += thruster.Thrust;
        }
    }

    private void RecalculateClockwiseTorque()
    {
        ClockwiseTorque = 0;
        foreach (var thruster in _clockwiseThrusters)
        {
            ClockwiseTorque += thruster.Torque;
        }
    }

    private void RecalculateCounterClockwiseTorque()
    {
        CounterClockwiseTorque = 0;
        foreach (var thruster in _counterClockwiseThrusters)
        {
            CounterClockwiseTorque -= thruster.Torque;
        }
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

