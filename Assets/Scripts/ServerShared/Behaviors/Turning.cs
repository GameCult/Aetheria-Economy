/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, JsonObject(MemberSerialization.OptIn), RuntimeInspectable]
public class TurningData : BehaviorData
{
    [InspectableField, JsonProperty("torque"), Key(1), RuntimeInspectable]  
    public PerformanceStat Torque = new PerformanceStat();

    [InspectableField, JsonProperty("visibility"), Key(2)]
    public PerformanceStat Visibility = new PerformanceStat();

    [InspectableField, JsonProperty("heat"), Key(3)]  
    public PerformanceStat Heat = new PerformanceStat();
    
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new Turning(context, this, entity, item);
    }
}

public class Turning : IAnalogBehavior
{
    public Entity Entity { get; }
    public EquippedItem Item { get; }
    public ItemManager Context { get; }
    
    public float Torque { get; private set; }

    public float Axis
    {
        get => _input;
        set => _input = clamp(value, -1, 1);
    }

    public BehaviorData Data => _data;
    
    private TurningData _data;
    
    private float _input;

    public Turning(ItemManager context, TurningData data, Entity entity, EquippedItem item)
    {
        Context = context;
        _data = data;
        Entity = entity;
        Item = item;
    }

    public void Initialize()
    {
    }

    public bool Update(float delta)
    {
        Torque = Context.Evaluate(_data.Torque, Item.EquippableItem, Entity);
        Entity.Direction = mul(Entity.Direction, Unity.Mathematics.float2x2.Rotate(_input * Torque / Entity.Mass * delta));
        Item.Temperature += (abs(_input) * Context.Evaluate(_data.Heat, Item.EquippableItem, Entity) * delta) / Context.GetThermalMass(Item.EquippableItem);
        Entity.VisibilitySources[this] = abs(_input) * Context.Evaluate(_data.Visibility, Item.EquippableItem, Entity);
        return true;
    }

    public void Remove()
    {
    }
}