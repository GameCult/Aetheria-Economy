/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn), Order(-22)]
public class ThermotoggleData : BehaviorData
{
    [TemperatureInspectable, JsonProperty("targetTemp"), Key(1)]
    public float TargetTemperature;
    
    [InspectableField, JsonProperty("highPass"), Key(2)]
    public bool HighPass;
    
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new Thermotoggle(context, this, entity, item);
    }
}

public class Thermotoggle : IBehavior
{
    public float TargetTemperature;
    private ThermotoggleData _data;

    private Entity Entity { get; }
    private EquippedItem Item { get; }
    private ItemManager Context { get; }

    public BehaviorData Data => _data;

    public Thermotoggle(ItemManager context, ThermotoggleData data, Entity entity, EquippedItem item)
    {
        _data = data;
        Entity = entity;
        Item = item;
        Context = context;
        TargetTemperature = data.TargetTemperature;
    }

    public bool Execute(float delta)
    {
        return Item.Temperature < TargetTemperature ^ _data.HighPass;
    }
}