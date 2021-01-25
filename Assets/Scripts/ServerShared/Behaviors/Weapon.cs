/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;

[Union(0, typeof(ProjectileWeaponData)),
 Union(1, typeof(LauncherData)),
 Union(2, typeof(ConstantWeaponData)), RuntimeInspectable]
public abstract class WeaponData : BehaviorData
{
    [InspectableField, JsonProperty("damageType"), Key(1), RuntimeInspectable]
    public DamageType DamageType;

    [InspectableField, JsonProperty("damage"), Key(2), RuntimeInspectable]
    public PerformanceStat Damage = new PerformanceStat();

    [RangedFloatInspectable(0,1), JsonProperty("penetration"), Key(3), RuntimeInspectable]
    public PerformanceStat Penetration = new PerformanceStat();

    [RangedFloatInspectable(0,1), JsonProperty("damageSpread"), Key(4)]
    public PerformanceStat DamageSpread = new PerformanceStat();

    [InspectableField, JsonProperty("range"), Key(5), RuntimeInspectable]
    public PerformanceStat Range = new PerformanceStat();
    
    [InspectablePrefab, JsonProperty("effect"), Key(6)]  
    public string EffectPrefab;
    
    [InspectablePrefab, JsonProperty("energy"), Key(7), RuntimeInspectable]  
    public PerformanceStat Energy = new PerformanceStat();
    
    [InspectablePrefab, JsonProperty("heat"), Key(8), RuntimeInspectable]  
    public PerformanceStat Heat = new PerformanceStat();
    
    [InspectablePrefab, JsonProperty("visibility"), Key(9), RuntimeInspectable]  
    public PerformanceStat Visibility = new PerformanceStat();
    
    [InspectableDatabaseLink(typeof(SimpleCommodityData)), JsonProperty("ammo"), Key(10), RuntimeInspectable]  
    public Guid AmmoType;

    [InspectablePrefab, JsonProperty("magSize"), Key(11)]
    public int MagazineSize;
    
    [InspectablePrefab, JsonProperty("reloadTime"), Key(12)]  
    public float ReloadTime = 1;
}

public abstract class Weapon : IActivatedBehavior
{
    private WeaponData _data;
    
    public Entity Entity { get; }
    public EquippedItem Item { get; }
    public ItemManager Context { get; }

    public abstract int Ammo { get; }
    public WeaponData WeaponData => _data;
    public BehaviorData Data => _data;

    protected bool _firing;

    public bool Firing
    {
        get => _firing;
    }


    public Weapon(ItemManager context, WeaponData data, Entity entity, EquippedItem item)
    {
        Context = context;
        Entity = entity;
        Item = item;
        _data = data;
    }

    public abstract bool Execute(float delta);

    public virtual void Activate()
    {
        _firing = true;
    }

    public virtual void Deactivate()
    {
        _firing = false;
    }
}
