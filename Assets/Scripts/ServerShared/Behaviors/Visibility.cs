/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;

[Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn), RuntimeInspectable]
public class VisibilityData : BehaviorData
{
    [Inspectable, JsonProperty("visibility"), Key(1), RuntimeInspectable]  
    public PerformanceStat Visibility = new PerformanceStat();

    [Inspectable, JsonProperty("visibilityDecay"), Key(2)]  
    public PerformanceStat VisibilityDecay = new PerformanceStat();
    
    public override IBehavior CreateInstance(EquippedItem item)
    {
        return new Visibility(this, item);
    }
}

public class Visibility : IBehavior
{
    private VisibilityData _data;

    private EquippedItem Item { get; }

    public BehaviorData Data => _data;

    private float _cooldown; // Normalized

    public Visibility(VisibilityData data, EquippedItem item)
    {
        _data = data;
        Item = item;
    }

    public bool Execute(float dt)
    {
        Item.Entity.VisibilitySources[this] = Item.Evaluate(_data.Visibility);
        return true;
    }
}