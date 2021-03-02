/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;
using UniRx;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, JsonObject(MemberSerialization.OptIn), RuntimeInspectable]
public class ReactorData : BehaviorData
{
    [InspectableField, JsonProperty("charge"), Key(1), RuntimeInspectable]  
    public PerformanceStat Charge = new PerformanceStat();

    [InspectableField, JsonProperty("efficiency"), Key(2), RuntimeInspectable]  
    public PerformanceStat Efficiency = new PerformanceStat();

    [InspectableField, JsonProperty("overload"), Key(3), RuntimeInspectable]  
    public PerformanceStat OverloadEfficiency = new PerformanceStat();

    [InspectableField, JsonProperty("underload"), Key(4), RuntimeInspectable]  
    public PerformanceStat ThrottlingFactor = new PerformanceStat();
    
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new Reactor(context, this, entity, item);
    }
}

public class Reactor : IBehavior, IOrderedBehavior
{
    private ReactorData _data;

    public Entity Entity { get; }
    public EquippedItem Item { get; }
    public ItemManager Context { get; }
    
    public float Draw { get; private set; }

    public int Order => 100;

    public BehaviorData Data => _data;

    private List<Capacitor> _capacitors;

    public Reactor(ItemManager context, ReactorData data, Entity entity, EquippedItem item)
    {
        Context = context;
        _data = data;
        Entity = entity;
        Item = item;
        _capacitors = entity.GetBehaviors<Capacitor>().ToList();
        entity.Equipment.ObserveAdd().Subscribe(onAdd =>
        {
            var capacitor = onAdd.Value.GetBehavior<Capacitor>();
            if (capacitor != null) _capacitors.Add(capacitor);
        });
        entity.Equipment.ObserveRemove().Subscribe(onRemove =>
        {
            var capacitor = onRemove.Value.GetBehavior<Capacitor>();
            if (capacitor != null) _capacitors.Remove(capacitor);
        });
        
    }

    public void ConsumeEnergy(float energy)
    {
        Draw += energy;
    }

    public bool Execute(float delta)
    {
        var charge = Context.Evaluate(_data.Charge, Item.EquippableItem, Entity) * delta;
        var efficiency = Context.Evaluate(_data.Efficiency, Item.EquippableItem, Entity);

        // This behavior executes last, so any components drawing power have already done so
        
        // Subtract the baseline charge from draw
        Draw -= charge;
        
        // Generate heat using baseline efficiency
        var heat = charge / efficiency;

        // We have an energy deficit, have to overload the reactor
        if (Draw > .01f)
        {
            var overloadEfficiency = Context.Evaluate(_data.OverloadEfficiency, Item.EquippableItem, Entity);
            
            // Generate heat using overload efficiency, usually much less efficient!
            heat += Draw / overloadEfficiency;
            
            // Overload power will always neutralize the energy deficit
            Draw = 0;
        }

        // We have an energy surplus, try to store energy in our capacitors
        if (Draw < -.01f)
        {
            int nonFullCapacitorCount;
            do
            {
                var chargeToAdd = -Draw;
                nonFullCapacitorCount = _capacitors.Count(c => c.Charge < c.Capacity - .01f);
                foreach (var capacitor in _capacitors)
                {
                    if (capacitor.Charge < capacitor.Capacity - .01f)
                    {
                        var chargeAdded = min(chargeToAdd / nonFullCapacitorCount, capacitor.Capacity - capacitor.Charge);
                        capacitor.AddCharge(chargeAdded);
                        Draw += chargeAdded;
                    }
                }
            } while (nonFullCapacitorCount > 0 && Draw < -.01f);
        }

        // We still have an energy surplus, try to throttle the reactor to reduce heat generation
        if (Draw < -.01f)
        {
            heat -= Draw / efficiency * (1 - 1 / Context.Evaluate(_data.ThrottlingFactor, Item.EquippableItem, Entity));
            Draw = 0;
        }
        
        Item.AddHeat(heat);
        return true;
    }
}