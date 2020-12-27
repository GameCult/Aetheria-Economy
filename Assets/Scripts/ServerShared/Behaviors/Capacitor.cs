/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, JsonObject(MemberSerialization.OptIn), RuntimeInspectable]
public class CapacitorData : BehaviorData
{
    [InspectableField, JsonProperty("capacity"), Key(1), RuntimeInspectable]  
    public PerformanceStat Capacity = new PerformanceStat();
    
    [InspectableField, JsonProperty("efficiency"), Key(2), RuntimeInspectable]  
    public PerformanceStat Efficiency = new PerformanceStat();
    
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new Capacitor(context, this, entity, item);
    }
}

public class Capacitor : IBehavior
{
    private CapacitorData _data;

    public Entity Entity { get; }
    public EquippedItem Item { get; }
    public ItemManager Context { get; }

    public BehaviorData Data => _data;
    
    public float Charge { get; private set; }
    public float Capacity { get; private set; }
    public float Efficiency { get; private set; } = 1;

    public void AddCharge(float charge)
    {
        Charge = clamp(Charge + charge, 0, Capacity);
        Item.AddHeat(abs(charge) * (1-Efficiency));
    }

    public Capacitor(ItemManager context, CapacitorData data, Entity entity, EquippedItem item)
    {
        Context = context;
        _data = data;
        Entity = entity;
        Item = item;
    }

    public bool Update(float delta)
    {
        Capacity = Context.Evaluate(_data.Capacity, Item.EquippableItem, Entity);
        Efficiency = Context.Evaluate(_data.Efficiency, Item.EquippableItem, Entity);
        return true;
    }
}