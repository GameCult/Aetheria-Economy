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
    
    public float Surplus { get; private set; }

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
            var cap = onAdd.Value.Behaviors.FirstOrDefault(x => x is Capacitor);
            if (cap != null)
                _capacitors.Add((Capacitor) cap);
        });
    }

    public bool Update(float delta)
    {
        var charge = Context.Evaluate(_data.Charge, Item.EquippableItem, Entity) * delta;
        var efficiency = Context.Evaluate(_data.Efficiency, Item.EquippableItem, Entity);

        Item.AddHeat(charge / efficiency);
        Entity.Energy += charge;

        Surplus = Entity.Energy;
        
        // We have an energy deficit, try to get some energy out of our capacitors first
        if (Entity.Energy < 0)
        {
            Capacitor[] chargedCapacitors;
            do
            {
                var chargeToRemove = -Entity.Energy;
                chargedCapacitors = _capacitors.Where(x => x.Charge > 0.01f).ToArray();
                foreach (var cap in chargedCapacitors)
                {
                    var chargeRemoved = min(chargeToRemove / chargedCapacitors.Length, cap.Charge);
                    cap.AddCharge(-chargeRemoved);
                    Entity.Energy += chargeRemoved;
                }
            } while (chargedCapacitors.Length > 0 && Entity.Energy < -.01f);
        }
        
        // We still have an energy deficit, have to overload the reactor, run at reduced efficiency
        if (Entity.Energy < -.01f)
        {
            var overloadEfficiency = Context.Evaluate(_data.OverloadEfficiency, Item.EquippableItem, Entity);
            Item.AddHeat(-Entity.Energy / overloadEfficiency); 
            Entity.Energy = 0;
        }

        // We have an energy surplus, try to store energy in our capacitors
        if (Entity.Energy > .01f)
        {
            Capacitor[] capacitors;
            do
            {
                var chargeToAdd = Entity.Energy;
                capacitors = _capacitors.Where(x => x.Charge < x.Capacity - .01f).ToArray();
                foreach (var cap in capacitors)
                {
                    var chargeAdded = min(chargeToAdd / capacitors.Length, cap.Capacity - cap.Charge);
                    cap.AddCharge(chargeAdded);
                    Entity.Energy -= chargeAdded;
                }
            } while (capacitors.Length > 0 && Entity.Energy < -.01f);
        }

        // We still have an energy surplus, try to throttle the reactor to reduce heat generation
        if (Entity.Energy > .01f)
        {
            Item.AddHeat(-Entity.Energy / efficiency * (1 - 1 / Context.Evaluate(_data.ThrottlingFactor, Item.EquippableItem, Entity)));
            Entity.Energy = 0;
        }
        return true;
    }
}