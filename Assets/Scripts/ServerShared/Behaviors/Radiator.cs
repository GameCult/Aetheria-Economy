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
    
    [InspectableField, JsonProperty("pumpedHeat"), Key(2), RuntimeInspectable]  
    public PerformanceStat PumpedHeat = new PerformanceStat();
    
    [TemperatureInspectable, JsonProperty("temperatureFloor"), Key(3), RuntimeInspectable]  
    public float TemperatureFloor;
    
    [InspectableField, JsonProperty("wasteHeat"), Key(4), RuntimeInspectable]  
    public PerformanceStat WasteHeat = new PerformanceStat();
    
    [InspectableField, JsonProperty("energyUsage"), Key(5), RuntimeInspectable]  
    public PerformanceStat EnergyUsage = new PerformanceStat();
    
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new Radiator(context, this, entity, item);
    }
}

public class Radiator : IBehavior, IAlwaysUpdatedBehavior, IInitializableBehavior
{
    public Entity Entity { get; }
    public EquippedItem Item { get; }
    public ItemManager Context { get; }
    public float Temperature { get; private set; }

    public BehaviorData Data => _data;

    public float Emissivity { get; private set; }
    public float PumpedHeat { get; private set; }
    public float WasteHeat { get; private set; }
    public float EnergyUsage { get; private set; }
    
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
        PumpedHeat = Context.Evaluate(_data.PumpedHeat, Item.EquippableItem, Entity);
        WasteHeat = Context.Evaluate(_data.WasteHeat, Item.EquippableItem, Entity);
        EnergyUsage = Context.Evaluate(_data.EnergyUsage, Item.EquippableItem, Entity);

        var itemTemperature = Item.Temperature;
        var tempRatio = max(Temperature / itemTemperature, 1);
        
        // Temperature ratio would cause more waste heat than pump capacity, stop executing
        if (tempRatio > PumpedHeat / WasteHeat) return true;
        
        var pumpedHeat = PumpedHeat * max(itemTemperature - _data.TemperatureFloor, 0);
        
        // Radiator temperature is below temperature floor, stop executing
        if (pumpedHeat < 0.01f) return true;
        
        var wasteHeat = WasteHeat * tempRatio;
        Entity.Energy -= EnergyUsage * tempRatio * delta;
        
        Item.AddHeat((wasteHeat - pumpedHeat) * delta);
        Temperature += pumpedHeat / Context.GetThermalMass(Item.EquippableItem) * delta;

        return true;
    }

    public void Update(float delta)
    {
        Emissivity = Context.Evaluate(_data.Emissivity, Item.EquippableItem, Entity);
        var rad = pow(Temperature, Context.GameplaySettings.HeatRadiationExponent) * Context.GameplaySettings.HeatRadiationMultiplier * Emissivity;
        Temperature -= rad * delta;
        Entity.VisibilitySources[this] = rad;
    }

    public void Initialize()
    {
        Temperature = Item.Temperature;
    }
}