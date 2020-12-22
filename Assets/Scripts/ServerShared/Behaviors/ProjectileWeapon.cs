/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using MessagePack;
using Newtonsoft.Json;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class ProjectileWeaponData : WeaponData
{
    [InspectablePrefab, JsonProperty("bullet"), Key(6)]  
    public string BulletPrefab;

    [InspectableField, JsonProperty("spread"), Key(7)]  
    public PerformanceStat Spread = new PerformanceStat();

    [InspectableField, JsonProperty("bulletInherit"), Key(8)]  
    public float Inherit;

    [InspectableField, JsonProperty("bulletVelocity"), Key(9)]  
    public PerformanceStat Velocity = new PerformanceStat();
    
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new ProjectileWeapon(context, this, entity, item);
    }
}

public class ProjectileWeapon : IBehavior
{
    private ProjectileWeaponData _data;
    private Entity Entity { get; }
    private EquippedItem Item { get; }
    private ItemManager Context { get; }
    
    public BehaviorData Data => _data;

    public ProjectileWeapon(ItemManager context, ProjectileWeaponData c, Entity entity, EquippedItem item)
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
        return true;
    }
}