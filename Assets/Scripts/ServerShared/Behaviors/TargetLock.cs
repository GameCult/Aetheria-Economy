/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, JsonObject(MemberSerialization.OptIn), Order(-11), RuntimeInspectable]
public class TargetLockData : BehaviorData
{
    [InspectableField, JsonProperty("speed"), Key(1), RuntimeInspectable]
    public PerformanceStat LockSpeed = new PerformanceStat();

    [InspectableField, JsonProperty("sensorImpact"), Key(2)]
    public PerformanceStat SensorImpact = new PerformanceStat();

    [InspectableField, JsonProperty("threshold"), Key(3), RuntimeInspectable]
    public PerformanceStat LockAngle = new PerformanceStat();

    [InspectableField, JsonProperty("directionImpact"), Key(4)]
    public PerformanceStat DirectionImpact = new PerformanceStat();

    [InspectableField, JsonProperty("decay"), Key(5)]
    public PerformanceStat Decay = new PerformanceStat();

    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new TargetLock(context, this, entity, item);
    }
}

public class TargetLock : IBehavior, IAlwaysUpdatedBehavior
{
    private TargetLockData _data;

    public Entity Entity { get; }
    public EquippedItem Item { get; }
    public ItemManager Context { get; }

    public BehaviorData Data => _data;

    public float Lock { get; private set; }

    public float Speed { get; private set; }
    public float SensorImpact { get; private set; }
    public float Threshold { get; private set; }
    public float DirectionImpact { get; private set; }
    public float Decay { get; private set; }

    private Entity _target;

    public TargetLock(ItemManager context, TargetLockData data, Entity entity, EquippedItem item)
    {
        Context = context;
        _data = data;
        Entity = entity;
        Item = item;
    }

    public bool Execute(float delta)
    {
        // TODO: Hook into sensory systems to influence lock speed
        return Lock > .99f;
    }

    public void Update(float delta)
    {
        if (_target != Entity.Target.Value)
        {
            Lock = 0;
            _target = Entity.Target.Value;
        }
        if (Entity.Target.Value != null)
        {
            Speed = Context.Evaluate(_data.LockSpeed, Item.EquippableItem, Entity);
            SensorImpact = Context.Evaluate(_data.SensorImpact, Item.EquippableItem, Entity);
            Threshold = Context.Evaluate(_data.LockAngle, Item.EquippableItem, Entity);
            DirectionImpact = Context.Evaluate(_data.DirectionImpact, Item.EquippableItem, Entity);
            Decay = Context.Evaluate(_data.Decay, Item.EquippableItem, Entity);

            var degrees = acos(dot(normalize(Entity.Target.Value.Position - Entity.Position), normalize(Entity.LookDirection))) * 57.2958f;
            if (degrees < Threshold)
            {
                var lerp = 1 - unlerp(0, 90, degrees);
                Lock = saturate(Lock + pow(lerp, DirectionImpact) * delta * Speed);
                return;
            }
        }
        Lock = saturate(Lock - Decay * delta);
    }
}