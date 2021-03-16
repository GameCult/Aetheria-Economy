/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn), Order(10), Inspectable]
public class HeatData : BehaviorData
{
    [InspectableField, JsonProperty("heat"), Key(1), RuntimeInspectable]
    public PerformanceStat Heat = new PerformanceStat();
    
    [InspectableField, JsonProperty("perSecond"), Key(2)]
    public bool PerSecond;
    
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new Heat(context, this, entity, item);
    }
}

public class Heat : IBehavior
{
    private HeatData _data;

    private Entity Entity { get; }
    private EquippedItem Item { get; }
    private ItemManager Context { get; }

    public BehaviorData Data => _data;

    public Heat(ItemManager context, HeatData data, Entity entity, EquippedItem item)
    {
        _data = data;
        Entity = entity;
        Item = item;
        Context = context;
    }

    public bool Execute(float delta)
    {
        Item.AddHeat(Item.Evaluate(_data.Heat) * (_data.PerSecond ? delta : 1));

        return true;
    }
}