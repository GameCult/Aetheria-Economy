﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn), RuntimeInspectable]
public class VisibilityData : BehaviorData
{
    [InspectableField, JsonProperty("visibility"), Key(1), RuntimeInspectable]  
    public PerformanceStat Visibility = new PerformanceStat();

    [InspectableField, JsonProperty("visibilityDecay"), Key(2)]  
    public PerformanceStat VisibilityDecay = new PerformanceStat();
    
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new Visibility(context, this, entity, item);
    }
}

public class Visibility : IBehavior, IAlwaysUpdatedBehavior
{
    private VisibilityData _data;

    private Entity Entity { get; }
    private EquippedItem Item { get; }
    private ItemManager Context { get; }

    public BehaviorData Data => _data;

    private float _cooldown; // Normalized

    public Visibility(ItemManager context, VisibilityData data, Entity entity, EquippedItem item)
    {
        _data = data;
        Entity = entity;
        Item = item;
        Context = context;
    }

    public bool Execute(float delta)
    {
        Entity.VisibilitySources[this] = Item.Evaluate(_data.Visibility);
        return true;
    }

    public void Update(float delta)
    {
        // TODO: Time independent decay?
        if(Entity.VisibilitySources.ContainsKey(this))
        {
            Entity.VisibilitySources[this] *= Item.Evaluate(_data.VisibilityDecay);
        }
    }
}