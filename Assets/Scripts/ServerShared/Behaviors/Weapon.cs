/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using MessagePack;
using Newtonsoft.Json;

[Union(0, typeof(ProjectileWeaponData)),
 Union(1, typeof(LauncherData)),
 Union(2, typeof(ConstantWeaponData))]
public abstract class WeaponData : BehaviorData
{
    [InspectableField, JsonProperty("damageType"), Key(1)]
    public DamageType DamageType;

    [InspectableField, JsonProperty("damage"), Key(2)]
    public PerformanceStat Damage = new PerformanceStat();

    [RangedFloatInspectable(0,1), JsonProperty("penetration"), Key(3)]
    public PerformanceStat Penetration = new PerformanceStat();

    [RangedFloatInspectable(0,1), JsonProperty("damageSpread"), Key(4)]
    public PerformanceStat DamageSpread = new PerformanceStat();

    [InspectableField, JsonProperty("range"), Key(5)]
    public PerformanceStat Range = new PerformanceStat();
    
    [InspectablePrefab, JsonProperty("effect"), Key(6)]  
    public string EffectPrefab;
}

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public abstract class InstantWeaponData : WeaponData
{
    [InspectableField, JsonProperty("burstCount"), Key(7)]
    public int BurstCount;

    [InspectableField, JsonProperty("burstTime"), Key(8)]
    public PerformanceStat BurstTime = new PerformanceStat();
}

public class InstantWeapon : IBehavior, IAlwaysUpdatedBehavior
{
    private InstantWeaponData _data;

    public int _burstCount;
    public float _burstTimer;
    
    private Entity Entity { get; }
    private EquippedItem Item { get; }
    private ItemManager Context { get; }
    
    public BehaviorData Data => _data;

    public event Action OnFire;

    private float _burstTime;

    public InstantWeapon(ItemManager context, InstantWeaponData c, Entity entity, EquippedItem item)
    {
        Context = context;
        _data = c;
        Entity = entity;
        Item = item;
    }

    public bool Execute(float delta)
    {
        _burstCount = _data.BurstCount;
        _burstTime = Context.Evaluate(_data.BurstTime, Item.EquippableItem, Entity) / _burstCount;
        _burstTimer = 0;
        Fire(delta);
        return true;
    }

    private void Fire(float delta)
    {
        _burstTimer -= delta;
        while (_burstTimer < 0 && _burstCount > 0)
        {
            _burstCount--;
            _burstTimer += _burstTime;
            OnFire?.Invoke();
        }
    }

    public void Update(float delta)
    {
        if(_burstCount > 0)
            Fire(delta);
    }
}