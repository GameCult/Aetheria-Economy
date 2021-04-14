/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn), Order(-10), RuntimeInspectable]
public class CooldownData : BehaviorData
{
    [Inspectable, JsonProperty("cooldown"), Key(1), RuntimeInspectable]
    public PerformanceStat Cooldown = new PerformanceStat();
    
    public override IBehavior CreateInstance(EquippedItem item)
    {
        return new Cooldown(this, item);
    }
}

public class Cooldown : IBehavior, IAlwaysUpdatedBehavior, IProgressBehavior
{
    private CooldownData _data;
    public BehaviorData Data => _data;

    private EquippedItem Item { get; }

    private float _cooldown; // Normalized

    public float Progress => saturate(_cooldown);

    public Cooldown(CooldownData data, EquippedItem item)
    {
        _data = data;
        Item = item;
    }

    public bool Execute(float dt)
    {
        if (_cooldown < 0)
        {
            _cooldown = 1;
            return true;
        }

        return false;

    }

    public void Update(float delta)
    {
        _cooldown -= delta / Item.Evaluate(_data.Cooldown);
    }
}