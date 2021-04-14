/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn), RuntimeInspectable]
public class VelocityLimitData : BehaviorData
{
    [Inspectable, JsonProperty("topSpeed"), Key(1), RuntimeInspectable]  
    public PerformanceStat TopSpeed = new PerformanceStat();
    
    public override IBehavior CreateInstance(EquippedItem item)
    {
        return new VelocityLimit(this, item);
    }
}

[Order(100)]
public class VelocityLimit : IBehavior
{
    public EquippedItem Item { get; }
    
    public float Limit { get; private set; }

    public BehaviorData Data => _data;
    
    private VelocityLimitData _data;

    public VelocityLimit(VelocityLimitData data, EquippedItem item)
    {
        _data = data;
        Item = item;
        Limit = Item.Evaluate(_data.TopSpeed);
    }

    public bool Execute(float dt)
    {
        Limit = Item.Evaluate(_data.TopSpeed);
        if (length(Item.Entity.Velocity) > Limit)
            Item.Entity.Velocity = normalize(Item.Entity.Velocity) * Limit;
        return true;
    }
}