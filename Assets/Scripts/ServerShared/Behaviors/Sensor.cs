/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn), RuntimeInspectable]
public class SensorData : BehaviorData
{
    [Inspectable, JsonProperty("sensitivity"), Key(3), RuntimeInspectable]
    public PerformanceStat Sensitivity = new PerformanceStat();

    [InspectableAnimationCurve, JsonProperty("sensCurve"), Key(4), RuntimeInspectable]
    public BezierCurve SensitivityCurve;

    [Inspectable, JsonProperty("pingBoost"), Key(5), RuntimeInspectable]
    public PerformanceStat PingBoost;

    [Inspectable, JsonProperty("pingEnergy"), Key(6), RuntimeInspectable]
    public PerformanceStat PingEnergy;

    [Inspectable, JsonProperty("pingVisibility"), Key(7), RuntimeInspectable]
    public PerformanceStat PingVisibility;

    [Inspectable, JsonProperty("pingRange"), Key(8), RuntimeInspectable]
    public PerformanceStat PingRange;

    [Inspectable, JsonProperty("pingCooldown"), Key(9), RuntimeInspectable]
    public PerformanceStat PingCooldown;

    [Inspectable, JsonProperty("pingDuration"), Key(10)]
    public float PingDuration = 2;

    [Inspectable, JsonProperty("pingRadiusExponent"), Key(11)]
    public float PingRadiusExponent = .5f;
    
    public override IBehavior CreateInstance(EquippedItem item)
    {
        return new Sensor(this, item);
    }
}

public class Sensor : IBehavior, IEventBehavior
{
    private SensorData _data;
    private float _pingCooldown;
    private float _pingLerp;
    private bool _pinging;
    private float _pingRadius;
    private HashSet<Entity> _pingedEntities = new HashSet<Entity>();

    private EquippedItem Item { get; }

    public BehaviorData Data => _data;
    
    public float Cooldown
    {
        get => saturate(_pingCooldown);
    }
    
    public float PingRadius
    {
        get => _pingRadius;
    }

    public float PingBrightness => pow(1 - _pingLerp, _data.PingRadiusExponent);

    public event Action OnPingStart;
    public event Action OnPingEnd;

    public void ResetEvents()
    {
        OnPingStart = null;
        OnPingEnd = null;
    }

    public void Ping()
    {
        if(_pingCooldown < 0 && Item.Entity.TryConsumeEnergy(Item.Evaluate(_data.PingEnergy)))
        {
            Item.Entity.VisibilitySources[this] = Item.Evaluate(_data.PingVisibility);
            _pinging = true;
            _pingCooldown = 1;
            _pingLerp = 0;
            _pingRadius = 0;
            _pingedEntities.Clear();
            OnPingStart?.Invoke();
        }
    }

    public Sensor(SensorData data, EquippedItem item)
    {
        _data = data;
        Item = item;
    }

    public bool Execute(float delta)
    {
        if (_pinging)
        {
            _pingLerp += delta / _data.PingDuration;
            _pingRadius = lerp(0, Item.Evaluate(_data.PingRange), pow(_pingLerp, _data.PingRadiusExponent));
            if (_pingLerp > 1)
            {
                _pinging = false;
                OnPingEnd?.Invoke();
            }
        }

        _pingCooldown -= delta / Item.Evaluate(_data.PingCooldown);
        
        // TODO: Handle Active Detection / Visibility From Reflected Radiance
        var hardpoint = Item.Entity.Hardpoints[Item.Position.x, Item.Position.y];
        var forward = hardpoint != null && Item.Entity.HardpointTransforms.ContainsKey(hardpoint) ? 
            normalize(Item.Entity.HardpointTransforms[hardpoint].direction.xz) : 
            Item.Entity.Direction;
        foreach (var entity in Item.Entity.Zone.Entities)
        {
            if (entity == Item.Entity) continue;
            
            var diff = entity.Position.xz - Item.Entity.Position.xz;
            var angle = acos(dot(forward, normalize(diff)));
            var dist = length(diff);
            float previous, next;
            Item.Entity.EntityInfoGathered.TryGetValue(entity, out previous);
            if (!_pingedEntities.Contains(entity) && dist < _pingRadius)
            {
                _pingedEntities.Add(entity);
                next = saturate(
                    previous +
                    entity.Visibility *
                    Item.Evaluate(_data.Sensitivity) *
                    Item.Evaluate(_data.PingBoost) *
                    dist);
            }
            else
            {
                next = saturate(
                    previous +
                    entity.Visibility *
                    Item.Evaluate(_data.Sensitivity) *
                    _data.SensitivityCurve.Evaluate(angle / PI) *
                    delta / dist);
            }
            next *= 1 - Item.ItemManager.GameplaySettings.TargetInfoDecay * delta;
            //Context.Log($"{entity.Name} visibility {(int)(previous * 100)}% -> {(int)(next * 100)}%");
            Item.Entity.EntityInfoGathered[entity] = next;
        }
        return true;
    }
}