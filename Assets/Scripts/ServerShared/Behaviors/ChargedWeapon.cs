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
    [InspectableField, JsonProperty("chargeTime"), Key(16), RuntimeInspectable]
    public PerformanceStat ChargeTime = new PerformanceStat();
    
    [InspectableField, JsonProperty("chargeEnergy"), Key(17), RuntimeInspectable]
    public PerformanceStat ChargeEnergy = new PerformanceStat();

    [InspectableField, JsonProperty("canFireEarly"), Key(18)]
    public bool CanFireEarly;

    [InspectableField, JsonProperty("failureCharge"), Key(19)]
    public float FailureCharge;

    [InspectableField, JsonProperty("failureDamage"), Key(20)]
    public float FailureDamage;
    
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

    public event Action OnStartCharging;
    public event Action OnStopCharging;
    public event Action OnCharged;
    public event Action OnFailed;

    public override float Progress => saturate(_charging ? _charge : _cooldown);

    public float Charge
    {
        get => _charge;
    }
    
    public ChargedWeapon(ItemManager context, ChargedWeaponData data, Entity entity, EquippedItem item) : base(context, data, entity, item)
    {
        _data = data;
    }

    public override bool Execute(float delta)
    {
        if (_charging)
        {
            _charge += delta / Context.Evaluate(_data.ChargeTime, Item.EquippableItem, Entity);
            if (!_charged && _charge > 1)
            {
                _charged = true;
                OnCharged?.Invoke();
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

