/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;

[Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn), RuntimeInspectable]
public class RadianceData : BehaviorData
{
    [Inspectable, JsonProperty("radiance"), Key(1), RuntimeInspectable]
    public PerformanceStat Radiance = new PerformanceStat();
    
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new Radiance(context, this, entity, item);
    }
}

public class Radiance : IBehavior
{
    private RadianceData _data;

    private Entity Entity { get; }
    private EquippedItem Item { get; }
    private ItemManager Context { get; }

    public BehaviorData Data => _data;

    public Radiance(ItemManager context, RadianceData data, Entity entity, EquippedItem item)
    {
        _data = data;
        Entity = entity;
        Item = item;
        Context = context;
    }

    public bool Execute(float delta)
    {
        // TODO: Light system!
        return true;
    }
}