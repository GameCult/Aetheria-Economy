/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using MessagePack;
using Newtonsoft.Json;

[MessagePackObject, JsonObject(MemberSerialization.OptIn), RuntimeInspectable]
public class ReactorData : BehaviorData
{
    [InspectableField, JsonProperty("charge"), Key(1), RuntimeInspectable]  
    public PerformanceStat Charge = new PerformanceStat();

    [InspectableField, JsonProperty("capacitance"), Key(2), RuntimeInspectable]  
    public PerformanceStat Capacitance = new PerformanceStat();

    [InspectableField, JsonProperty("efficiency"), Key(3), RuntimeInspectable]  
    public PerformanceStat Efficiency = new PerformanceStat();

    [InspectableField, JsonProperty("overload"), Key(4), RuntimeInspectable]  
    public PerformanceStat OverloadEfficiency = new PerformanceStat();

    [InspectableField, JsonProperty("underload"), Key(5), RuntimeInspectable]  
    public PerformanceStat ThrottlingFactor = new PerformanceStat();
    
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new Reactor(context, this, entity, item);
    }
}

public class Reactor : IBehavior
{
    private ReactorData _data;

    public Entity Entity { get; }
    public EquippedItem Item { get; }
    public ItemManager Context { get; }

    public BehaviorData Data => _data;
    public float Capacitance;

    public Reactor(ItemManager context, ReactorData data, Entity entity, EquippedItem item)
    {
        Context = context;
        _data = data;
        Entity = entity;
        Item = item;
    }

    public bool Update(float delta)
    {
        Capacitance = Context.Evaluate(_data.Capacitance, Item.EquippableItem, Entity);
        var charge = Context.Evaluate(_data.Charge, Item.EquippableItem, Entity) * delta;
        var efficiency = Context.Evaluate(_data.Efficiency, Item.EquippableItem, Entity);

        Item.Temperature += (charge / efficiency) / Context.GetThermalMass(Item.EquippableItem);
        Entity.Energy += charge;

        if (Entity.Energy > Capacitance)
        {
            Item.Temperature -= (Entity.Energy - Capacitance) / efficiency * (1 - 1 / Context.Evaluate(_data.ThrottlingFactor, Item.EquippableItem, Entity)) / Context.GetThermalMass(Item.EquippableItem);
            Entity.Energy = Capacitance;
        }

        if (Entity.Energy < 0)
        {
            Item.Temperature += -Entity.Energy / Context.Evaluate(_data.OverloadEfficiency, Item.EquippableItem, Entity) / Context.GetThermalMass(Item.EquippableItem);
            Entity.Energy = 0;
        }
        return true;
    }
}