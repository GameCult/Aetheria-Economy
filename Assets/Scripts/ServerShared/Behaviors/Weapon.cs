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

    // private void Fire()
    // {
        // Hardpoint.Temperature += _projectileWeapon.Heat.Evaluate(Hardpoint) / Hardpoint.HeatCapacity;
        // var inst = GameObject.Instantiate(_projectileWeapon.BulletPrefab).transform;
        // Physics.IgnoreCollision(Hardpoint.Ship.Ship.GetComponent<Collider>(), inst.GetComponent<Collider>());
        // var bullet = inst.GetComponent<Bullet>();
        // bullet.Target = Hardpoint.Ship.Ship.Target;
        // bullet.Source = Hardpoint.Ship.Ship.Hitpoints;
        // bullet.Lifetime = _projectileWeapon.Range.Evaluate(Hardpoint) / _projectileWeapon.Velocity.Evaluate(Hardpoint);
        // bullet.Damage = _projectileWeapon.Damage.Evaluate(Hardpoint);
        // inst.position = Hardpoint.Proxy.position;
        // inst.rotation = Hardpoint.Proxy.rotation;
        // inst.GetComponent<Rigidbody>().velocity = _projectileWeapon.Velocity.Evaluate(Hardpoint) * Vector3.RotateTowards(-inst.forward, Hardpoint.Ship.Ship.Direction, _projectileWeapon.Deflection * Mathf.Deg2Rad, 1);
        // Hardpoint.Ship.Ship.AudioSource.PlayOneShot(_projectileWeapon.Sounds.RandomElement());
//        Debug.Log($"Firing bullet {b}");
    // }

    public bool Update(float delta)
    {
        _burstTime = Context.Evaluate(_data.BurstTime, Item.EquippableItem, Entity) / _burstCount;
        if (_burstTime < .01f)
        {
            for(int i=0; i<_data.BurstCount; i++)
                OnFire?.Invoke();
        }
        else
        {
            _burstCount = _data.BurstCount;
            Fire();
        }
        return true;
    }

    private void Fire()
    {
        _burstCount--;
        _burstTimer = _burstTime;
        OnFire?.Invoke();
    }

    public void AlwaysUpdate(float delta)
    {
        if(_burstCount > 0)
        {
            _burstTimer -= delta;
            if(_burstTimer < 0)
                Fire();
        }
    }
}