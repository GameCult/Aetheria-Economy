/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn), Order(1000), RuntimeInspectable]
public class WearData : BehaviorData
{
    [InspectableTemperature, JsonProperty("perSecond"), Key(1)]
    public bool PerSecond = true;
    
    public override IBehavior CreateInstance(EquippedItem item)
    {
        return new Wear(this, item);
    }
}

public class Wear : IBehavior
{
    private WearData _data;

    public EquippedItem Item { get; }

    public BehaviorData Data => _data;

    public Wear(WearData data, EquippedItem item)
    {
        _data = data;
        Item = item;
    }

    public bool Execute(float delta)
    {
        if (_data.PerSecond)
            Item.EquippableItem.Durability -= Item.Wear * delta;
        else Item.EquippableItem.Durability -= Item.Wear;
        return true;
    }
}