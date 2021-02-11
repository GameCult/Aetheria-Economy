/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;

[Union(0, typeof(InstantWeaponData)),
 Union(1, typeof(LauncherData)),
 Union(2, typeof(ConstantWeaponData)), 
 Union(3, typeof(ChargedWeaponData)), 
 RuntimeInspectable]
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

    [InspectableField, JsonProperty("minRange"), Key(5), RuntimeInspectable]
    public PerformanceStat MinRange = new PerformanceStat();

    [InspectableField, JsonProperty("range"), Key(6), RuntimeInspectable]
    public PerformanceStat Range = new PerformanceStat();

    [InspectableAnimationCurve, JsonProperty("damageRange"), Key(7), RuntimeInspectable]
    public float4[] DamageCurve;
    
    [InspectablePrefab, JsonProperty("effect"), Key(8)]  
    public string EffectPrefab;
    
    [InspectablePrefab, JsonProperty("energy"), Key(9), RuntimeInspectable]  
    public PerformanceStat Energy = new PerformanceStat();
    
    [InspectablePrefab, JsonProperty("heat"), Key(10), RuntimeInspectable]  
    public PerformanceStat Heat = new PerformanceStat();
    
    [InspectablePrefab, JsonProperty("visibility"), Key(11), RuntimeInspectable]  
    public PerformanceStat Visibility = new PerformanceStat();
    
    [InspectableDatabaseLink(typeof(SimpleCommodityData)), JsonProperty("ammo"), Key(12), RuntimeInspectable]  
    public Guid AmmoType;

    [InspectablePrefab, JsonProperty("magSize"), Key(13)]
    public int MagazineSize;
    
    [InspectablePrefab, JsonProperty("reloadTime"), Key(14)]  
    public float ReloadTime = 1;
    
    [InspectablePrefab, JsonProperty("spread"), Key(15)]  
    public PerformanceStat Spread = new PerformanceStat();

    [InspectableField, JsonProperty("velocity"), Key(16)]
    public PerformanceStat Velocity = new PerformanceStat();
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
    
    public float Damage { get; protected set; }
    public float Penetration { get; protected set; }
    public float DamageSpread { get; protected set; }
    public float MinRange { get; protected set; }
    public float Range { get; protected set; }
    public float Energy { get; protected set; }
    public float Heat { get; protected set; }
    public float Visibility { get; protected set; }
    public float Spread { get; protected set; }
    public float Velocity { get; protected set; }

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

    public virtual void ResetEvents(){}

    protected virtual void UpdateStats()
    {
        Damage = Context.Evaluate(_data.Damage, Item.EquippableItem, Entity);
        Penetration = Context.Evaluate(_data.Penetration, Item.EquippableItem, Entity);
        DamageSpread = Context.Evaluate(_data.DamageSpread, Item.EquippableItem, Entity);
        MinRange = Context.Evaluate(_data.MinRange, Item.EquippableItem, Entity);
        Range = Context.Evaluate(_data.Range, Item.EquippableItem, Entity);
        Energy = Context.Evaluate(_data.Energy, Item.EquippableItem, Entity);
        Heat = Context.Evaluate(_data.Heat, Item.EquippableItem, Entity);
        Visibility = Context.Evaluate(_data.Visibility, Item.EquippableItem, Entity);
        Spread = Context.Evaluate(_data.Spread, Item.EquippableItem, Entity);
        Velocity = Context.Evaluate(_data.Velocity, Item.EquippableItem, Entity);
    }

    public virtual bool Execute(float delta)
    {
        UpdateStats();
        return true;
    }

    public virtual void Activate()
    {
        _firing = true;
    }

    public virtual void Deactivate()
    {
        _firing = false;
    }
}
