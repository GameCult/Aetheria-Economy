/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, JsonObject(MemberSerialization.OptIn), RuntimeInspectable]
public class VelocityLimitData : BehaviorData
{
    [InspectableField, JsonProperty("topSpeed"), Key(1), RuntimeInspectable]  
    public PerformanceStat TopSpeed = new PerformanceStat();
    
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new VelocityLimit(context, this, entity, item);
    }
}

[Order(100)]
public class VelocityLimit : IBehavior
{
    public Entity Entity { get; }
    public EquippedItem Item { get; }
    public ItemManager Context { get; }
    
    public float Limit { get; private set; }

    public BehaviorData Data => _data;
    
    private VelocityLimitData _data;

    public VelocityLimit(ItemManager context, VelocityLimitData data, Entity entity, EquippedItem item)
    {
        Context = context;
        _data = data;
        Entity = entity;
        Item = item;
        Limit = Item.Evaluate(_data.TopSpeed);
    }

    public void Initialize()
    {
    }

    public bool Execute(float delta)
    {
        Limit = Item.Evaluate(_data.TopSpeed);
        if (length(Entity.Velocity) > Limit)
            Entity.Velocity = normalize(Entity.Velocity) * Limit;
        return true;
    }

    public void Remove()
    {
    }
}