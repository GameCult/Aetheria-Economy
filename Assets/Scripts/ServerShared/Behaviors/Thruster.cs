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
    
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new Thruster(context, this, entity, item);
    }
}

public class Thruster : IAnalogBehavior
{
    public Entity Entity { get; }
    public EquippedItem Item { get; }
    public ItemManager Context { get; }
    
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

    public Thruster(ItemManager context, ThrusterData data, Entity entity, EquippedItem item)
    {
        Context = context;
        _data = data;
        Entity = entity;
        Item = item;
        var hullData = context.GetData(entity.Hull) as HullData;
        var hullCenter = hullData.Shape.CenterOfMass;
        var itemData = context.GetData(item.EquippableItem);
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
            Entity.Velocity -= Entity.Direction.Rotate(Item.EquippableItem.Rotation) * _input * Thrust / Entity.Mass * delta;
            Entity.Direction = mul(Entity.Direction,
                Unity.Mathematics.float2x2.Rotate(_input * Torque * Thrust * Context.GameplaySettings.TorqueMultiplier / Entity.Mass * delta));
            Item.AddHeat(_input * Item.Evaluate(_data.Heat) * delta);
            var vis = _input * Item.Evaluate(_data.Visibility);
            if (!Entity.VisibilitySources.ContainsKey(this) || vis > Entity.VisibilitySources[this])
                Entity.VisibilitySources[this] = vis;
            return true;
        }
        return false;
    }
}