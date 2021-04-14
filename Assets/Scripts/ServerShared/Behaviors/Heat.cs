/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;

[Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn), Order(10)]
public class HeatData : BehaviorData
{
    [Inspectable, JsonProperty("heat"), Key(1), RuntimeInspectable]
    public PerformanceStat Heat = new PerformanceStat();
    
    [Inspectable, JsonProperty("perSecond"), Key(2)]
    public bool PerSecond;
    
    public override IBehavior CreateInstance(EquippedItem item)
    {
        return new Heat(this, item);
    }
}

public class Heat : IBehavior
{
    private HeatData _data;

    private EquippedItem Item { get; }

    public BehaviorData Data => _data;

    public Heat(HeatData data, EquippedItem item)
    {
        _data = data;
        Item = item;
    }

    public bool Execute(float dt)
    {
        Item.AddHeat(Item.Evaluate(_data.Heat) * (_data.PerSecond ? dt : 1));

        return true;
    }
}