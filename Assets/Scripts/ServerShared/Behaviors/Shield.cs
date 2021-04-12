/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn), RuntimeInspectable]
public class ShieldData : BehaviorData
{
    [Inspectable, JsonProperty("efficiency"), Key(1), RuntimeInspectable]  
    public PerformanceStat Efficiency = new PerformanceStat();

    [Inspectable, JsonProperty("energy"), Key(2), RuntimeInspectable]  
    public PerformanceStat EnergyUsage = new PerformanceStat();
    
    public override IBehavior CreateInstance(EquippedItem item)
    {
        return new Shield(this, item);
    }
}

public class Shield : IBehavior
{
    public EquippedItem Item { get; }
    
    public float Efficiency { get; private set; }
    public float EnergyUsage { get; private set; }

    public BehaviorData Data => _data;
    
    private ShieldData _data;

    public Shield(ShieldData data, EquippedItem item)
    {
        _data = data;
        Item = item;
    }

    public bool Execute(float delta)
    {
        Efficiency = Item.Evaluate(_data.Efficiency);
        EnergyUsage = Item.Evaluate(_data.EnergyUsage);
        return true;
    }

    public bool CanTakeHit(DamageType type, float damage)
    {
        return Item.Entity.CanConsumeEnergy(damage * EnergyUsage);
    }

    public void TakeHit(DamageType type, float damage)
    {
        Item.Entity.TryConsumeEnergy(damage * EnergyUsage);
        Item.AddHeat(damage / Efficiency);
    }
}