/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;

[Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn), Order(-22)]
public class ThermotoggleData : BehaviorData
{
    [InspectableTemperature, JsonProperty("targetTemp"), Key(1)]
    public float TargetTemperature;
    
    [Inspectable, JsonProperty("highPass"), Key(2)]
    public bool HighPass;

    [Inspectable, JsonProperty("adjustable"), Key(3)]
    public bool Adjustable;
    
    public override IBehavior CreateInstance(EquippedItem item)
    {
        return new Thermotoggle(this, item);
    }
}

public class Thermotoggle : IBehavior
{
    public float TargetTemperature;
    private ThermotoggleData _data;

    private EquippedItem Item { get; }

    public BehaviorData Data => _data;
    public ThermotoggleData ThermotoggleData => _data;

    public Thermotoggle(ThermotoggleData data, EquippedItem item)
    {
        _data = data;
        Item = item;
        TargetTemperature = data.TargetTemperature;
    }

    public bool Execute(float dt)
    {
        return Item.Temperature < TargetTemperature ^ _data.HighPass;
    }
}