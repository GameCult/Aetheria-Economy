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
    public bool IsPlayerShip;

    private HashSet<EquippedItem> _thrusterItems;
    private Thruster[] _allThrusters;
    private HashSet<Thruster> _forwardThrusters;
    private HashSet<Thruster> _reverseThrusters;
    private HashSet<Thruster> _rightThrusters;
    private HashSet<Thruster> _leftThrusters;
    private HashSet<Thruster> _clockwiseThrusters;
    private HashSet<Thruster> _counterClockwiseThrusters;

    private bool _exitingWormhole = false;
    private bool _enteringWormhole = false;
    private float _wormholeAnimationProgress;
    private float2 _wormholeEntryPosition;
    private float2 _wormholeEntryDirection;
    private float2 _wormholePosition;
    private float2 _wormholeExitVelocity;

    public float ForwardThrust { get; private set; }
    public float ReverseThrust { get; private set; }
    public float LeftStrafeThrust { get; private set; }
    public float RightStrafeThrust { get; private set; }
    public float ClockwiseTorque { get; private set; }
    public float CounterClockwiseTorque { get; private set; }
    public float LeftStrafeTotalTorque { get; private set; }
    private Thruster[] LeftStrafeTorqueThrusters;
    public float RightStrafeTotalTorque { get; private set; }
    private Thruster[] RightStrafeTorqueThrusters;

    public float TurnTime(float2 direction)
    {
        var angleDiff = Direction.Angle(normalize(direction));
        var clockwise = dot(direction, Direction.Rotate(ItemRotation.Clockwise)) > 0;
        return angleDiff / ((clockwise ? ClockwiseTorque : CounterClockwiseTorque) / Mass);
    }

    public event Action OnExitedWormhole;
    public event Action OnEnteredWormhole;
    
    public quaternion Rotation { get; private set; }

    public void ExitWormhole(float2 wormholePosition, float2 exitVelocity)
    {
        _exitingWormhole = true;
        _wormholeAnimationProgress = 0;
        _wormholePosition = wormholePosition;
        _wormholeExitVelocity = exitVelocity;
        Direction = normalize(exitVelocity);
    }

    public void EnterWormhole(float2 wormholePosition)
    {
        Target.Value = null;
        _wormholeAnimationProgress = 0;
        _enteringWormhole = true;
        _wormholeEntryPosition = Position.xz;
        _wormholeEntryDirection = normalize(_wormholeEntryPosition-wormholePosition);
        _wormholePosition = wormholePosition;
    }

    public override void Activate()
    {
        base.Activate();
        
        _allThrusters = GetBehaviors<Thruster>().ToArray();
        _thrusterItems = new HashSet<EquippedItem>(_allThrusters.Select(x=>x.Item));
        
        _forwardThrusters = new HashSet<Thruster>(_allThrusters
            .Where(x => x.Item.EquippableItem.Rotation == ItemRotation.Reversed));
        RecalculateForwardThrust();
        foreach (var thruster in _forwardThrusters) 
            thruster.Item.Online.Skip(1).Subscribe(b => RecalculateForwardThrust());

        _reverseThrusters = new HashSet<Thruster>(_allThrusters
            .Where(x => x.Item.EquippableItem.Rotation == ItemRotation.None));
        RecalculateReverseThrust();
        foreach (var thruster in _reverseThrusters) 
            thruster.Item.Online.Skip(1).Subscribe(b => RecalculateReverseThrust());
        
        _rightThrusters = new HashSet<Thruster>(_allThrusters
            .Where(x => x.Item.EquippableItem.Rotation == ItemRotation.CounterClockwise));
        RecalculateRightStrafeThrust();
        foreach (var thruster in _rightThrusters) 
            thruster.Item.Online.Skip(1).Subscribe(b => RecalculateRightStrafeThrust());
        
        _leftThrusters = new HashSet<Thruster>(_allThrusters
            .Where(x => x.Item.EquippableItem.Rotation == ItemRotation.Clockwise));
        RecalculateLeftStrafeThrust();
        foreach (var thruster in _leftThrusters) 
            thruster.Item.Online.Skip(1).Subscribe(b => RecalculateLeftStrafeThrust());
        
        _counterClockwiseThrusters = new HashSet<Thruster>(_allThrusters
            .Where(x => x.Torque < -ItemManager.GameplaySettings.TorqueFloor));
        RecalculateCounterClockwiseTorque();
        foreach (var thruster in _counterClockwiseThrusters) 
            thruster.Item.Online.Skip(1).Subscribe(b => RecalculateCounterClockwiseTorque());
        
        _clockwiseThrusters = new HashSet<Thruster>(_allThrusters
            .Where(x => x.Torque > ItemManager.GameplaySettings.TorqueFloor));
        RecalculateClockwiseTorque();
        foreach (var thruster in _clockwiseThrusters) 
            thruster.Item.Online.Skip(1).Subscribe(b => RecalculateClockwiseTorque());
    }

    public Ship(ItemManager itemManager, Zone zone, EquippableItem hull, EntitySettings settings) : base(itemManager, zone, hull, settings)
    {
        void CheckForThruster(EquippedItem item)
        {
            if (_thrusterItems.Contains(item))
            {
                foreach (var behavior in item.Behaviors)
                {
                    if (behavior is Thruster thruster)
                    {
                        if (_forwardThrusters.Contains(thruster)) RecalculateForwardThrust();
                        if (_reverseThrusters.Contains(thruster)) RecalculateReverseThrust();
                        if (_rightThrusters.Contains(thruster)) RecalculateRightStrafeThrust();
                        if (_leftThrusters.Contains(thruster)) RecalculateLeftStrafeThrust();
                        if (_clockwiseThrusters.Contains(thruster)) RecalculateClockwiseTorque();
                        if (_counterClockwiseThrusters.Contains(thruster)) RecalculateCounterClockwiseTorque();
                    }
                }
            }
        }

        ItemDamage.Select(x => x.item).Subscribe(CheckForThruster);
    }

    #region ThrustCalculation
    
    private void RecalculateForwardThrust()
    {
        ForwardThrust = 0;
        foreach (var thruster in _forwardThrusters)
        {
            if(thruster.Item.Active.Value)
                ForwardThrust += thruster.Thrust;
        }
    }

    private void RecalculateReverseThrust()
    {
        ReverseThrust = 0;
        foreach (var thruster in _reverseThrusters)
        {
            if(thruster.Item.Active.Value)
                ReverseThrust += thruster.Thrust;
        }
    }

    private void RecalculateLeftStrafeThrust()
    {
        LeftStrafeThrust = 0;
        LeftStrafeTotalTorque = 0;
        foreach (var thruster in _leftThrusters)
        {
            if(thruster.Item.Active.Value)
            {
                LeftStrafeThrust += thruster.Thrust;
                LeftStrafeTotalTorque += thruster.Torque * thruster.Thrust;
            }
        }
        LeftStrafeTorqueThrusters = _leftThrusters
            .Where(x => abs(sign(x.Torque) - sign(LeftStrafeTotalTorque)) < .01f)
            .ToArray();
    }

    private void RecalculateRightStrafeThrust()
    {
        RightStrafeThrust = 0;
        RightStrafeTotalTorque = 0;
        foreach (var thruster in _rightThrusters)
        {
            if(thruster.Item.Active.Value)
            {
                RightStrafeThrust += thruster.Thrust;
                RightStrafeTotalTorque += thruster.Torque * thruster.Thrust;
            }
        }
        RightStrafeTorqueThrusters = _rightThrusters
            .Where(x => abs(sign(x.Torque) - sign(RightStrafeTotalTorque)) < .01f)
            .ToArray();
    }

    private void RecalculateClockwiseTorque()
    {
        ClockwiseTorque = 0;
        foreach (var thruster in _clockwiseThrusters)
        {
            if(thruster.Item.Active.Value)
                ClockwiseTorque += thruster.Torque;
        }
    }

    private void RecalculateCounterClockwiseTorque()
    {
        CounterClockwiseTorque = 0;
        foreach (var thruster in _counterClockwiseThrusters)
        {
            if(thruster.Item.Active.Value)
                CounterClockwiseTorque -= thruster.Torque;
        }
    }

    #endregion

    public override void Update(float delta)
    {
        foreach (var thruster in _allThrusters) thruster.Axis = 0;
        if (_active && !_exitingWormhole && !_enteringWormhole)
        {
            var rightThrusterTorqueCompensation = abs(RightStrafeTotalTorque) / RightStrafeTorqueThrusters.Length;
            foreach (var thruster in _rightThrusters)
            {
                var thrust = 0f;
                thrust += MovementDirection.x;
                if (RightStrafeTorqueThrusters.Contains(thruster))
                    thrust -= MovementDirection.x * (rightThrusterTorqueCompensation / (abs(thruster.Torque) * thruster.Thrust));
                thruster.Axis = thrust;
            }
            var leftThrusterTorqueCompensation = abs(LeftStrafeTotalTorque) / LeftStrafeTorqueThrusters.Length;
            foreach (var thruster in _leftThrusters)
            {
                var thrust = 0f;
                thrust += -MovementDirection.x;
                if (LeftStrafeTorqueThrusters.Contains(thruster))
                    thrust += MovementDirection.x * (leftThrusterTorqueCompensation / (abs(thruster.Torque) * thruster.Thrust));
                thruster.Axis = thrust;
            }
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
        
        Position.xz += Velocity * delta;
        
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

        if (_exitingWormhole)
        {
            _wormholeAnimationProgress += delta / ItemManager.GameplaySettings.WormholeAnimationDuration;
            if(_wormholeAnimationProgress < 1)
            {
                if (_wormholeAnimationProgress < ItemManager.GameplaySettings.WormholeExitCurveStart)
                {
                    Position.xz = _wormholePosition;
                    Rotation = quaternion.LookRotation(float3(0, 1, 0), float3(-Direction.x, 0, -Direction.y));
                }
                else
                {
                    var exitLerp = (_wormholeAnimationProgress - ItemManager.GameplaySettings.WormholeExitCurveStart) /
                                   (1 - ItemManager.GameplaySettings.WormholeExitCurveStart);
                    exitLerp = AetheriaMath.Smootherstep(exitLerp); // Square the interpolation variable to produce curve with zero slope at start
                    Position.xz = _wormholePosition + normalize(_wormholeExitVelocity) * exitLerp * ItemManager.GameplaySettings.WormholeExitRadius;
                    Rotation = quaternion.LookRotation(
                        lerp(float3(0, 1, 0), forward, exitLerp),
                        lerp(float3(-Direction.x, 0, -Direction.y), normal, exitLerp));
                }

                Position.y = Position.y - lerp(ItemManager.GameplaySettings.WormholeDepth, 0, _wormholeAnimationProgress);
            }
            else
            {
                _exitingWormhole = false;
                OnExitedWormhole?.Invoke();
                OnExitedWormhole = null;
                Velocity = _wormholeExitVelocity;
            }
        }

        if (_enteringWormhole)
        {
            _wormholeAnimationProgress += delta / ItemManager.GameplaySettings.WormholeAnimationDuration;
            if(_wormholeAnimationProgress < 1)
            {
                if (_wormholeAnimationProgress < 1 - ItemManager.GameplaySettings.WormholeExitCurveStart)
                {
                    var enterLerp = _wormholeAnimationProgress / (1 - ItemManager.GameplaySettings.WormholeExitCurveStart);
                    enterLerp = AetheriaMath.Smootherstep(enterLerp); // Square the interpolation variable to produce curve with zero slope at vertical
                    Position.xz = lerp(_wormholeEntryPosition, _wormholePosition, enterLerp);
                    Rotation = quaternion.LookRotation(
                        lerp(forward, float3(0, -1, 0), enterLerp),
                        lerp(normal, float3(-_wormholeEntryDirection.x, 0, -_wormholeEntryDirection.y), enterLerp));
                }
                else
                {
                    Position.xz = _wormholePosition;
                    Rotation = quaternion.LookRotation(float3(0, -1, 0), 
                        float3(-_wormholeEntryDirection.x, 0, -_wormholeEntryDirection.y));
                }

                Position.y = Position.y - lerp(0, ItemManager.GameplaySettings.WormholeDepth, _wormholeAnimationProgress);
            }
            else
            {
                _enteringWormhole = false;
                OnEnteredWormhole?.Invoke();
                OnEnteredWormhole = null;
            }
        }
    }
}

