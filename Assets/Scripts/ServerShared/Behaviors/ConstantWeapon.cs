/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class ConstantWeaponData : WeaponData
{
    [InspectablePrefab, JsonProperty("ammoInterval"), Key(17)]  
    public float AmmoInterval = 1;
    
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new ConstantWeapon(context, this, entity, item);
    }
}

public class ConstantWeapon : Weapon, IProgressBehavior
{
    private ConstantWeaponData _data;
    private int _ammo = 1;
    private float _ammoInterval;
    private float _reload;
    private bool _reloading;
    
    public override int Ammo
    {
        get => _ammo;
    }
    
    public float Progress
    {
        get { return saturate(_reload); }
    }
    
    public override float DamagePerSecond => Damage;
    public override float RangeDamagePerSecond(float range)
    {
        return Damage * _data.DamageCurve.Evaluate(saturate(unlerp(MinRange, Range, range)));
    }

    public event Action OnReloadBegin;
    public event Action OnReloadComplete;
    public event Action OnStartFiring;
    public event Action OnStopFiring;
    
    public override void ResetEvents()
    {
        OnReloadBegin = null;
        OnReloadComplete = null;
        OnStartFiring = null;
        OnStopFiring = null;
    }

    public ConstantWeapon(ItemManager context, ConstantWeaponData data, Entity entity, EquippedItem item) : base(context, data, entity, item)
    {
        _data = data;
    }

    public override bool Execute(float delta)
    {
        base.Execute(delta);
        if (_firing)
        {
            if (_data.AmmoType != Guid.Empty)
            {
                if (_reloading)
                {
                    _reload -= delta / _data.ReloadTime;
                    if (_reload < 0)
                    {
                        _reloading = false;
                        OnReloadComplete?.Invoke();
                    }
                    return false;
                }
                
                _ammoInterval -= delta / _data.AmmoInterval;
                if (_ammoInterval < 0)
                {
                    _ammoInterval = 1;
                    if (_data.MagazineSize > 1 && _ammo > 0) _ammo--;
                    else
                    {
                        var cargo = Entity.FindItemInCargo(_data.AmmoType);
                        if (cargo != null)
                        {
                            var item = cargo.ItemsOfType[_data.AmmoType][0];
                            if (item is SimpleCommodity simpleCommodity)
                                cargo.Remove(simpleCommodity, 1);
                            
                            if(_data.MagazineSize > 1)
                            {
                                _reloading = true;
                                _reload = 1;
                                OnReloadBegin?.Invoke();

                                _firing = false;
                                OnStopFiring?.Invoke();
                            }
                        }
                        return false;
                    }
                }
            }
            
            Item.AddHeat(Context.Evaluate(_data.Heat, Item.EquippableItem, Entity) * delta);
            Entity.VisibilitySources[this] = Context.Evaluate(_data.Visibility, Item.EquippableItem, Entity);
            Entity.Energy -= Context.Evaluate(_data.Energy, Item.EquippableItem, Entity) * delta;
        }
        return true;
    }

    public override void Activate()
    {
        if(!_firing && !_reloading)
        {
            _firing = true;
            OnStartFiring?.Invoke();
        }
    }

    public override void Deactivate()
    {
        if (_firing)
        {
            _firing = false;
            OnStopFiring?.Invoke();
        }
    }
}