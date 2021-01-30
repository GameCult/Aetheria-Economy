using System;
using System.Collections;
using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn), RuntimeInspectable]
public class InstantWeaponData : WeaponData
{
    [InspectableField, JsonProperty("burstCount"), Key(13)]
    public int BurstCount;

    [InspectableField, JsonProperty("burstTime"), Key(14)]
    public PerformanceStat BurstTime = new PerformanceStat();
    
    [InspectableField, JsonProperty("cooldown"), Key(15), RuntimeInspectable]
    public PerformanceStat Cooldown = new PerformanceStat();

    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new InstantWeapon(context, this, entity, item);
    }
}

public class InstantWeapon : Weapon, IProgressBehavior
{
    private InstantWeaponData _data;

    protected int _burstRemaining;
    private float _burstTimer;
    private float _burstInterval;
    protected float _cooldown; // Normalized
    private int _ammo = 1;
    protected bool _coolingDown;

    public override int Ammo
    {
        get => _ammo;
    }
    public virtual float Progress => saturate(_cooldown);

    public event Action OnReloadBegin;
    public event Action OnReloadComplete;
    public event Action OnCooldownComplete;
    public event Action OnFire;

    public InstantWeapon(ItemManager context, InstantWeaponData data, Entity entity, EquippedItem item) : base(context,data,entity,item)
    {
        _data = data;
    }

    protected void Trigger()
    {
        _burstRemaining = _data.BurstCount;
        _burstInterval = Context.Evaluate(_data.BurstTime, Item.EquippableItem, Entity) / _burstRemaining;
        _burstTimer = 0;
        _cooldown = 1;
        _coolingDown = true;
    }

    public override bool Execute(float delta)
    {
        if (_coolingDown)
        {
            _cooldown -= delta / (_ammo == 0 ? _data.ReloadTime : Context.Evaluate(_data.Cooldown, Item.EquippableItem, Entity));
            if (_cooldown < 0)
            {
                _coolingDown = false;
                if (_ammo == 0)
                {
                    _ammo = _data.MagazineSize;
                    OnReloadComplete?.Invoke();
                }
                else
                    OnCooldownComplete?.Invoke();
            }
        }
        
        _burstTimer += delta;
        while (_burstRemaining > 0 && _burstTimer > 0)
        {
            if (_data.AmmoType != Guid.Empty)
            {
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
                            OnReloadBegin?.Invoke();
                            _cooldown = 1;
                            _coolingDown = true;
                            _firing = false;
                        }
                    }
                    _burstRemaining = 0;
                    return false;
                }
            }
            
            _burstRemaining--;
            _burstTimer -= _burstInterval;
            OnFire?.Invoke();
            Item.AddHeat(Context.Evaluate(_data.Heat, Item.EquippableItem, Entity));
            Entity.VisibilitySources[this] = Context.Evaluate(_data.Visibility, Item.EquippableItem, Entity);
            Entity.Energy -= Context.Evaluate(_data.Energy, Item.EquippableItem, Entity);
        }
        return true;
    }

    public override void Activate()
    {
        if(!_coolingDown)
            Trigger();
        base.Activate();
    }
}