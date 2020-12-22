/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, JsonObject(MemberSerialization.OptIn), EntityTypeRestriction(HullType.Ship), RuntimeInspectable]
public class ThrusterData : BehaviorData
{
    [InspectableField, JsonProperty("thrust"), Key(1), RuntimeInspectable]  
    public PerformanceStat Thrust = new PerformanceStat();

    [InspectableField, JsonProperty("visibility"), Key(2)]  
    public PerformanceStat Visibility = new PerformanceStat();

    [InspectableField, JsonProperty("heat"), Key(3)]  
    public PerformanceStat Heat = new PerformanceStat();

    [InspectablePrefab, JsonProperty("Particles"), Key(4)]
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
        Torque = -dot(toCenter, float2(1, 0).Rotate(item.EquippableItem.Rotation));
    }

    public bool Update(float delta)
    {
        Thrust = Context.Evaluate(_data.Thrust, Item.EquippableItem, Entity);
        Entity.Velocity -= Entity.Direction.Rotate(Item.EquippableItem.Rotation) * _input * Thrust / Entity.Mass * delta;
        Entity.Direction = mul(Entity.Direction, Unity.Mathematics.float2x2.Rotate(_input * Torque * Thrust / Entity.Mass / 100 * delta));
        Item.Temperature += (_input * Context.Evaluate(_data.Heat, Item.EquippableItem, Entity) * delta) / Context.GetThermalMass(Item.EquippableItem);
        Entity.VisibilitySources[this] = _input * Context.Evaluate(_data.Visibility, Item.EquippableItem, Entity);
        return true;
    }
}