/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, JsonObject(MemberSerialization.OptIn), RuntimeInspectable]
public class RadiatorData : BehaviorData
{
    [InspectableField, JsonProperty("emissivity"), Key(1), RuntimeInspectable]  
    public PerformanceStat Emissivity = new PerformanceStat();
    
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new Radiator(context, this, entity, item);
    }
}

public class Radiator : IBehavior
{
    public Entity Entity { get; }
    public EquippedItem Item { get; }
    public ItemManager Context { get; }

    public BehaviorData Data => _data;

    public float Emissivity;
    
    private RadiatorData _data;

    public Radiator(ItemManager context, RadiatorData data, Entity entity, EquippedItem item)
    {
        Context = context;
        _data = data;
        Entity = entity;
        Item = item;
    }

    public bool Execute(float delta)
    {
        Emissivity = Context.Evaluate(_data.Emissivity, Item.EquippableItem, Entity);
        var rad = pow(Item.Temperature, Context.GameplaySettings.HeatRadiationExponent) * Context.GameplaySettings.HeatRadiationMultiplier * Emissivity;
        Item.AddHeat(-rad * delta * Context.GetThermalMass(Item.EquippableItem));
        Entity.VisibilitySources[this] = rad;
        return true;
    }
}