/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn), EntityTypeRestriction(HullType.Ship), RuntimeInspectable]
public class ThrusterData : BehaviorData
{
    [Inspectable, JsonProperty("thrust"), Key(1), RuntimeInspectable]  
    public PerformanceStat Thrust = new PerformanceStat();

    [Inspectable, JsonProperty("visibility"), Key(2), RuntimeInspectable]  
    public PerformanceStat Visibility = new PerformanceStat();

    [Inspectable, JsonProperty("heat"), Key(3), RuntimeInspectable]  
    public PerformanceStat Heat = new PerformanceStat();

    [Inspectable, JsonProperty("energy"), Key(4), RuntimeInspectable]  
    public PerformanceStat EnergyUsage = new PerformanceStat();

    [InspectablePrefab, JsonProperty("Particles"), Key(5)]
    public string ParticlesPrefab;
    
    public override IBehavior CreateInstance(EquippedItem item)
    {
        return new Thruster(this, item);
    }
}

public class Thruster : IAnalogBehavior
{
    public EquippedItem Item { get; }
    
    public float Thrust { get; private set; }
    public float Torque { get; }

    public float Axis
    {
        get => _input;
        set => _input = saturate(value);
    }

    public BehaviorData Data => _data;
    
    private ThrusterData _data;
    
    private float _input;

    public Thruster(ThrusterData data, EquippedItem item)
    {
        _data = data;
        Item = item;
        var hullData = Item.ItemManager.GetData(Item.Entity.Hull) as HullData;
        var hullCenter = hullData.Shape.CenterOfMass;
        var itemData = Item.ItemManager.GetData(item.EquippableItem);
        var itemCenter = hullData.Shape.Inset(itemData.Shape, item.Position, item.EquippableItem.Rotation).CenterOfMass;
        var toCenter = hullCenter - itemCenter;
        Torque = -dot(normalize(toCenter), float2(1, 0).Rotate(item.EquippableItem.Rotation));
        Thrust = Item.Evaluate(_data.Thrust);
    }

    public bool Execute(float delta)
    {
        if(_input > .01f && Item.Entity.TryConsumeEnergy(_input * Item.Evaluate(_data.EnergyUsage)))
        {
            Thrust = Item.Evaluate(_data.Thrust);
            Item.Entity.Velocity -= Item.Entity.Direction.Rotate(Item.EquippableItem.Rotation) * _input * Thrust / Item.Entity.Mass * delta;
            Item.Entity.Direction = mul(Item.Entity.Direction,
                Unity.Mathematics.float2x2.Rotate(_input * Torque * Thrust * Item.ItemManager.GameplaySettings.TorqueMultiplier / Item.Entity.Mass * delta));
            Item.AddHeat(_input * Item.Evaluate(_data.Heat) * delta);
            var vis = _input * Item.Evaluate(_data.Visibility);
            if (!Item.Entity.VisibilitySources.ContainsKey(this) || vis > Item.Entity.VisibilitySources[this])
                Item.Entity.VisibilitySources[this] = vis;
            return true;
        }
        return false;
    }
}