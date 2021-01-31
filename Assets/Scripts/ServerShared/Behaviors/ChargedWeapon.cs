using System;
using System.Collections;
using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn), RuntimeInspectable]
public class ChargedWeaponData : InstantWeaponData
{
    [InspectableField, JsonProperty("chargeTime"), Key(18), RuntimeInspectable]
    public PerformanceStat ChargeTime = new PerformanceStat();
    
    [InspectableField, JsonProperty("chargeEnergy"), Key(19), RuntimeInspectable]
    public PerformanceStat ChargeEnergy = new PerformanceStat();
    
    [InspectableField, JsonProperty("chargeHeat"), Key(20), RuntimeInspectable]
    public PerformanceStat ChargeHeat = new PerformanceStat();

    [InspectableField, JsonProperty("canFireEarly"), Key(21)]
    public bool CanFireEarly;

    [InspectableField, JsonProperty("failureCharge"), Key(22)]
    public float FailureCharge;

    [InspectableField, JsonProperty("failureDamage"), Key(23)]
    public float FailureDamage = 1;

    [InspectableField, JsonProperty("chargeDamage"), Key(24)]
    public float ChargeDamage = 1;

    [InspectableField, JsonProperty("chargeSpread"), Key(25)]
    public float ChargeSpread = 1;

    [InspectableField, JsonProperty("chargeBurstCount"), Key(26)]
    public float ChargeBurstCount = 1;

    [InspectableField, JsonProperty("chargeVisibility"), Key(27)]
    public float ChargeVisibility = 1;

    [InspectableField, JsonProperty("chargeVelocity"), Key(28)]
    public float ChargeVelocity = 1;
    
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new ChargedWeapon(context, this, entity, item);
    }
}

public class ChargedWeapon : InstantWeapon
{
    private ChargedWeaponData _data;
    private bool _charging;
    private bool _charged;
    private float _charge;
    private float _progress;
    
    public float ChargeTime { get; protected set; }
    public float ChargeEnergy { get; protected set; }
    public float ChargeHeat { get; protected set; }

    public event Action OnStartCharging;
    public event Action OnStopCharging;
    public event Action OnCharged;
    public event Action OnFailed;

    public override void ResetEvents()
    {
        OnStartCharging = null;
        OnStopCharging = null;
        OnCharged = null;
        OnFailed = null;
    }

    public override float Progress => saturate(_charging ? _charge : _cooldown);

    public float Charge
    {
        get => _charge;
    }
    
    public ChargedWeapon(ItemManager context, ChargedWeaponData data, Entity entity, EquippedItem item) : base(context, data, entity, item)
    {
        _data = data;
    }

    protected override void UpdateStats()
    {
        base.UpdateStats();
        ChargeTime = Context.Evaluate(_data.ChargeTime, Item.EquippableItem, Entity);
        ChargeEnergy = Context.Evaluate(_data.ChargeEnergy, Item.EquippableItem, Entity);
        ChargeHeat = Context.Evaluate(_data.ChargeHeat, Item.EquippableItem, Entity);
        Damage *= lerp(1, _data.ChargeDamage, saturate(_charge));
        Spread *= lerp(1, _data.ChargeSpread, saturate(_charge));
        BurstCount *= lerp(1, _data.ChargeBurstCount, saturate(_charge));
        Visibility *= lerp(1, _data.ChargeVisibility, saturate(_charge));
        Velocity *= lerp(1, _data.ChargeVelocity, saturate(_charge));
    }

    public override bool Execute(float delta)
    {
        if (_charging)
        {
            _charge += delta / ChargeTime;
            if (!_charged)
            {
                Item.AddHeat(ChargeHeat * (delta / ChargeTime));
                if(_charge > 1)
                {
                    _charged = true;
                    OnCharged?.Invoke();
                }
            }
            if (_data.FailureCharge > 1 && _charge > _data.FailureCharge)
            {
                _charging = false;
                _cooldown = 1;
                _coolingDown = true;
                _charge = 0;
                OnFailed?.Invoke();
                Item.EquippableItem.Durability -= _data.FailureDamage;
            }
        }
        return base.Execute(delta);
    }

    public override void Activate()
    {
        if(!_coolingDown)
        {
            OnStartCharging?.Invoke();
            _charging = true;
            _charged = false;
        }
    }

    public override void Deactivate()
    {
        if (_charging)
        {
            if (_data.CanFireEarly || _charge > 1)
            {
                Trigger();
                _charge = 0;
            }
            OnStopCharging?.Invoke();
            _charging = false;
        }
    }
}

