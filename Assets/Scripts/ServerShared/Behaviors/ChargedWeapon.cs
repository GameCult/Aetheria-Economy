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
    [InspectableField, JsonProperty("chargeTime"), Key(21), RuntimeInspectable]
    public PerformanceStat ChargeTime = new PerformanceStat();
    
    [InspectableField, JsonProperty("chargeEnergy"), Key(22), RuntimeInspectable]
    public PerformanceStat ChargeEnergy = new PerformanceStat();
    
    [InspectableField, JsonProperty("chargeHeat"), Key(23), RuntimeInspectable]
    public PerformanceStat ChargeHeat = new PerformanceStat();

    [InspectableField, JsonProperty("canFireEarly"), Key(24)]
    public bool CanFireEarly;

    [InspectableField, JsonProperty("failureCharge"), Key(25)]
    public float FailureCharge;

    [InspectableField, JsonProperty("failureDamage"), Key(26)]
    public float FailureDamage = 1;

    [InspectableField, JsonProperty("chargeDamage"), Key(27)]
    public float ChargeFiringDamageMultiplier = 1;

    [InspectableField, JsonProperty("chargeSpread"), Key(28)]
    public float ChargeFiringSpreadMultiplier = 1;

    [InspectableField, JsonProperty("chargeBurstCount"), Key(29)]
    public float ChargeFiringBurstCountMultiplier = 1;

    [InspectableField, JsonProperty("chargeVisibility"), Key(30)]
    public float ChargeFiringVisibilityMultiplier = 1;

    [InspectableField, JsonProperty("chargeVelocity"), Key(31)]
    public float ChargeFiringVelocityMultiplier = 1;

    [InspectableField, JsonProperty("chargeHeatMul"), Key(32)]
    public float ChargeFiringHeatMultiplier = 1;
    
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
    
    public override float DamagePerSecond => Damage * _data.ChargeFiringDamageMultiplier / (Cooldown + ChargeTime);
    public override float RangeDamagePerSecond(float range)
    {
        return Damage *
               _data.ChargeFiringDamageMultiplier *
               _data.DamageCurve.Evaluate(saturate(unlerp(MinRange, Range, range))) /
               (Cooldown + ChargeTime);
    }

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
        ChargeTime = Item.Evaluate(_data.ChargeTime);
        ChargeEnergy = Item.Evaluate(_data.ChargeEnergy);
        ChargeHeat = Item.Evaluate(_data.ChargeHeat);
        Damage *= lerp(1, _data.ChargeFiringDamageMultiplier, saturate(_charge));
        Heat *= lerp(1, _data.ChargeFiringHeatMultiplier, saturate(_charge));
        Spread *= lerp(1, _data.ChargeFiringSpreadMultiplier, saturate(_charge));
        BurstCount *= lerp(1, _data.ChargeFiringBurstCountMultiplier, saturate(_charge));
        Visibility *= lerp(1, _data.ChargeFiringVisibilityMultiplier, saturate(_charge));
        Velocity *= lerp(1, _data.ChargeFiringVelocityMultiplier, saturate(_charge));
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
        if(!_charging && !_coolingDown)
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

