/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn), Order(-10), RuntimeInspectable]
public class CooldownData : BehaviorData
{
    [InspectableField, JsonProperty("cooldown"), Key(1), RuntimeInspectable]
    public PerformanceStat Cooldown = new PerformanceStat();
    
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new Cooldown(context, this, entity, item);
    }
}

public class Cooldown : IBehavior, IAlwaysUpdatedBehavior, IProgressBehavior
{
    private CooldownData _data;
    public BehaviorData Data => _data;

    private Entity Entity { get; }
    private EquippedItem Item { get; }
    private ItemManager Context { get; }

    private float _cooldown; // Normalized

    public float Progress => saturate(_cooldown);

    public Cooldown(ItemManager context, CooldownData data, Entity entity, EquippedItem item)
    {
        _data = data;
        Entity = entity;
        Item = item;
        Context = context;
    }

    public bool Execute(float delta)
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
        _cooldown -= delta / Context.Evaluate(_data.Cooldown, Item);
    }
}