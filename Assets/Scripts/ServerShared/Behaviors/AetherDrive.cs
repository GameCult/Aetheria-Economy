/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn), EntityTypeRestriction(HullType.Ship), RuntimeInspectable]
public class AetherDriveData : BehaviorData
{
    [Inspectable, JsonProperty("diameters"), Key(1)]
    public float3 RotorDiameter;
    
    [Inspectable, JsonProperty("masses"), Key(2)]
    public float3 RotorMass;
    
    [Inspectable, JsonProperty("rpm"), Key(3), RuntimeInspectable]
    public PerformanceStat MaximumRpm;
    
    [Inspectable, JsonProperty("couplingLambdas"), Key(4)]
    public float3 CouplingLambda;
    
    [Inspectable, JsonProperty("couplingEfficiency"), Key(5), RuntimeInspectable]
    public PerformanceStat CouplingEfficiency;
    
    [Inspectable, JsonProperty("torque"), Key(6), RuntimeInspectable]
    public PerformanceStat Torque;
    
    [Inspectable, JsonProperty("torqueProfile"), Key(7), RuntimeInspectable]
    public BezierCurve TorqueProfile;
    
    [Inspectable, JsonProperty("draw"), Key(8), RuntimeInspectable]
    public PerformanceStat EnergyDraw;
    
    [Inspectable, JsonProperty("passiveCoupling"), Key(9), RuntimeInspectable]
    public PerformanceStat PassiveCoupling;
    
    public override IBehavior CreateInstance(EquippedItem item)
    {
        return new AetherDrive(this, item);
    }
}

public class AetherDrive : IBehavior
{
    private AetherDriveData _data;
    private float3 _axis;

    public EquippedItem Item { get; }
    public float3 Thrust { get; private set; }
    public float3 Rpm { get; private set; }
    public float MaximumRpm { get; private set; }

    public BehaviorData Data => _data;
    public AetherDriveData DriveData => _data;

    public float3 Axis
    {
        get => _axis;
        set => _axis = clamp(value, -1, 1);
    }

    public AetherDrive(AetherDriveData data, EquippedItem item)
    {
        _data = data;
        Item = item;
    }

    public bool Execute(float dt)
    {
        var rotorSpeed = Rpm * _data.RotorDiameter / 100;
        
        var forward = normalize(Item.Entity.Direction);
        var right = forward.Rotate(ItemRotation.Clockwise);
            
        var speed = float2(dot(Item.Entity.Velocity, forward), dot(Item.Entity.Velocity, right));
        var couplingEfficiency = Item.Evaluate(_data.CouplingEfficiency);
        var efficiency = float3(saturate(1 - speed / max(rotorSpeed.xy, 1) * sign(_axis.xy)) * couplingEfficiency, 1);

        Thrust = (Rpm - AetheriaMath.Decay(Rpm, _data.CouplingLambda, dt)) * _data.RotorMass * efficiency;

        var couplingLambda = _data.CouplingLambda * max(abs(_axis), Item.Evaluate(_data.PassiveCoupling));
        var previousRpm = Rpm;
        Rpm = AetheriaMath.Decay(Rpm, couplingLambda, dt);
        var rpmLoss = previousRpm - Rpm;
        var force = rpmLoss * _data.RotorMass * efficiency;

        var heat = rpmLoss * _data.RotorMass * (1 - couplingEfficiency);
        Item.AddHeat((heat.x + heat.y + heat.z)*Item.ItemManager.GameplaySettings.AetherHeatMultiplier);
        
        Item.Entity.Velocity += forward * (_axis.x * force.x / Item.Entity.Mass);
        Item.Entity.Velocity += right * (_axis.y * force.y / Item.Entity.Mass);
        Item.Entity.Direction = mul(Item.Entity.Direction,
            Unity.Mathematics.float2x2.Rotate(force.z * _axis.z * Item.ItemManager.GameplaySettings.AetherTorqueMultiplier / Item.Entity.Mass));

        if(float.IsNaN(Item.Entity.Velocity.x))
            Item.ItemManager.Log("FUCK FUCK FUCK FUCK");
        
        MaximumRpm = Item.Evaluate(_data.MaximumRpm);
        var torqueProfile = float3(
            _data.TorqueProfile.Evaluate(Rpm.x / MaximumRpm),
            _data.TorqueProfile.Evaluate(Rpm.y / MaximumRpm),
            _data.TorqueProfile.Evaluate(Rpm.z / MaximumRpm));
        var potentialTorque = Item.Evaluate(_data.Torque) * torqueProfile;
        var potentialRpmDelta = potentialTorque / length(_data.RotorMass) * dt;
        var actualRpmDelta = min(MaximumRpm - Rpm, potentialRpmDelta);
        var torqueRatio = actualRpmDelta / potentialRpmDelta;
        var draw = torqueRatio * Item.Evaluate(_data.EnergyDraw) / 3;
        
        if (Item.Entity.TryConsumeEnergy((draw.x + draw.y + draw.z)*dt))
        {
            Rpm += actualRpmDelta;
            return true;
        }
        
        
        return false;
    }
}