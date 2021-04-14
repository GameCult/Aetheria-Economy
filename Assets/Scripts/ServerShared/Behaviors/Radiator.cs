/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn), RuntimeInspectable]
public class RadiatorData : BehaviorData
{
    [Inspectable, JsonProperty("emissivity"), Key(1), RuntimeInspectable]  
    public PerformanceStat Emissivity = new PerformanceStat();
    
    [Inspectable, JsonProperty("pumpedHeat"), Key(2), RuntimeInspectable]  
    public PerformanceStat PumpedHeat = new PerformanceStat();
    
    [InspectableTemperature, JsonProperty("temperatureFloor"), Key(3), RuntimeInspectable]  
    public float TemperatureFloor;
    
    [Inspectable, JsonProperty("wasteHeat"), Key(4), RuntimeInspectable]  
    public PerformanceStat WasteHeat = new PerformanceStat();
    
    [Inspectable, JsonProperty("energyUsage"), Key(5), RuntimeInspectable]  
    public PerformanceStat EnergyUsage = new PerformanceStat();
    
    public override IBehavior CreateInstance(EquippedItem item)
    {
        return new Radiator(this, item);
    }
}

public class Radiator : IBehavior, IAlwaysUpdatedBehavior, IInitializableBehavior
{
    public EquippedItem Item { get; }
    public float Temperature { get; private set; }

    public BehaviorData Data => _data;

    public float Emissivity { get; private set; }
    public float PumpedHeat { get; private set; }
    public float WasteHeat { get; private set; }
    public float EnergyUsage { get; private set; }
    
    private RadiatorData _data;

    public Radiator(RadiatorData data, EquippedItem item)
    {
        _data = data;
        Item = item;
    }

    public bool Execute(float dt)
    {
        PumpedHeat = Item.Evaluate(_data.PumpedHeat);
        WasteHeat = Item.Evaluate(_data.WasteHeat);
        EnergyUsage = Item.Evaluate(_data.EnergyUsage);

        var itemTemperature = Item.Temperature;
        var tempRatio = max(Temperature / itemTemperature, 1);
        
        // Temperature ratio would cause more waste heat than pump capacity, stop executing
        if (tempRatio > PumpedHeat / WasteHeat) return true;

        if (!Item.Entity.TryConsumeEnergy(EnergyUsage * tempRatio * dt)) return false;
        
        var pumpedHeat = PumpedHeat * max(itemTemperature - _data.TemperatureFloor, 0);
        
        // Radiator temperature is below temperature floor, stop executing
        if (pumpedHeat < 0.01f) return true;
        
        var wasteHeat = WasteHeat * tempRatio;
        
        Item.AddHeat((wasteHeat - pumpedHeat) * dt);
        Temperature += pumpedHeat / Item.ItemManager.GetThermalMass(Item.EquippableItem) * dt;

        return true;
    }

    public void Update(float delta)
    {
        Emissivity = Item.Evaluate(_data.Emissivity);
        var rad = pow(Temperature, Item.ItemManager.GameplaySettings.HeatRadiationExponent) * Item.ItemManager.GameplaySettings.HeatRadiationMultiplier * Emissivity;
        Temperature -= rad * delta;
        Item.Entity.VisibilitySources[this] = rad;
    }

    public void Initialize()
    {
        Temperature = Item.Temperature;
    }
}