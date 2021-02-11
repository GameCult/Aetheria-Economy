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
    [InspectableField, JsonProperty("count"), Key(17)]
    public PerformanceStat Count = new PerformanceStat();

    [InspectableField, JsonProperty("burstTime"), Key(18)]
    public PerformanceStat BurstTime = new PerformanceStat();
    
    [InspectableField, JsonProperty("cooldown"), Key(19), RuntimeInspectable]
    public PerformanceStat Cooldown = new PerformanceStat();
    
    [InspectablePrefab, JsonProperty("ammoInterval"), Key(20)]  
    public bool SingleAmmoBurst;

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
    private int _ammo = 0;
    protected bool _coolingDown;
    
    public float BurstCount { get; protected set; }
    public float BurstTime { get; protected set; }
    public float Cooldown { get; protected set; }

    public override int Ammo
    {
        get => _ammo;
    }
    public virtual float Progress => saturate(_cooldown);

    public event Action OnReloadBegin;
    public event Action OnReloadComplete;
    public event Action OnCooldownComplete;
    public event Action OnFire;

    public override void ResetEvents()
    {
        OnReloadBegin = null;
        OnReloadComplete = null;
        OnCooldownComplete = null;
        OnFire = null;
    }

    public InstantWeapon(ItemManager context, InstantWeaponData data, Entity entity, EquippedItem item) : base(context,data,entity,item)
    {
        _data = data;
    }

    protected void Trigger()
    {
        // If 1 ammo is consumed per burst, perform ammo consumption here
        // UseAmmo returns false when triggering reload; cancel firing if that is the case
        if(_data.SingleAmmoBurst && !UseAmmo()) return;
        
        _burstRemaining = (int) BurstCount;
        _burstInterval = BurstTime / _burstRemaining;
        _burstTimer = 0;
        _cooldown = 1;
        _coolingDown = true;
    }

    protected override void UpdateStats()
    {
        base.UpdateStats();
        BurstCount = Context.Evaluate(_data.Count, Item.EquippableItem, Entity);
        BurstTime = Context.Evaluate(_data.BurstTime, Item.EquippableItem, Entity);
        Cooldown = Context.Evaluate(_data.Cooldown, Item.EquippableItem, Entity);

        Damage /= (int) BurstCount;
        Heat /= (int) BurstCount;
        Energy /= (int) BurstCount;
    }

    private bool UseAmmo()
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

        return true;
    }

    public override bool Execute(float delta)
    {
        base.Execute(delta);
        if (_coolingDown)
        {
            _cooldown -= delta / (_ammo == 0 ? _data.ReloadTime : Cooldown);
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
            // If 1 ammo is consumed per shot in the burst, perform ammo consumption here
            // UseAmmo returns false when triggering reload; cancel firing if that is the case
            if (!_data.SingleAmmoBurst && !UseAmmo()) return false;
            
            _burstRemaining--;
            _burstTimer -= _burstInterval;
            OnFire?.Invoke();
            Item.AddHeat(Heat);
            Entity.VisibilitySources[this] = Visibility;
            Entity.Energy -= Energy;
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