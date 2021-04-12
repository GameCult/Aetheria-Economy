/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;

[Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn), Order(-20), RuntimeInspectable]
public class EnergyDrawData : BehaviorData
{
    [Inspectable, JsonProperty("draw"), Key(1), RuntimeInspectable]
    public PerformanceStat EnergyDraw = new PerformanceStat();
    
    [Inspectable, JsonProperty("perSecond"), Key(2)]
    public bool PerSecond;
    
    public override IBehavior CreateInstance(EquippedItem item)
    {
        return new EnergyDraw(this, item);
    }
}

public class EnergyDraw : IBehavior
{
    private EnergyDrawData _data;

    private EquippedItem Item { get; }

    public BehaviorData Data => _data;

    public EnergyDraw(EnergyDrawData data, EquippedItem item)
    {
        _data = data;
        Item = item;
    }

    public bool Execute(float delta)
    {
        return Item.Entity.TryConsumeEnergy(Item.Evaluate(_data.EnergyDraw) * (_data.PerSecond ? delta : 1));
    }
}