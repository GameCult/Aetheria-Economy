using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using UniRx;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Audio;
using static Unity.Mathematics.math;
using static Unity.Mathematics.noise;
using Object = UnityEngine.Object;

public interface IItemBehavior
{
    void Update(float delta);
    void FixedUpdate(float delta);
    Ship Ship { get; }
    Gear Item { get; }
    IItemBehaviorData Data { get; }
}

public interface IActivatedItemBehavior : IItemBehavior
{
    void Activate();
    void Deactivate();
}

[Inspectable]
[Union(0, typeof(CannonBehaviorData))]
[Union(1, typeof(MissileBehaviorData))]
[Union(2, typeof(ReactorBehaviorData))]
// [Union(3, typeof(PlusUltraBehaviorData))]
[Union(4, typeof(AfterburnerBehaviorData))]
[Union(5, typeof(SensorBehaviorData))]
public interface IItemBehaviorData
{
    IItemBehavior CreateInstance(Ship ship, Gear item);
    // PerformanceStat[] DynamicStats { get; }
    //Type ItemType { get; }
}

[Inspectable]
[MessagePackObject]
public class SensorBehaviorData : IItemBehaviorData
{
    [Inspectable] [Key("radiance")]       public PerformanceStat       Radiance;
    [Inspectable] [Key("masking")]        public PerformanceStat       RadianceMasking;
    [Inspectable] [Key("sensitivity")]    public PerformanceStat       Sensitivity;
    [Inspectable] [Key("range")]          public PerformanceStat       Range;
    
    public IItemBehavior CreateInstance(Ship ship, Gear item)
    {
        return new SensorBehavior(this, ship, item);
    }
    
    [IgnoreMember] public PerformanceStat[] DynamicStats => new[] {Radiance, Sensitivity};
}

public class SensorBehavior : IItemBehavior
{
    private SensorBehaviorData _data;

    public Ship Ship { get; }
    public Gear Item { get; }
    
    public IItemBehaviorData Data => _data;

    public SensorBehavior(SensorBehaviorData data, Ship ship, Gear item)
    {
        _data = data;
        Ship = ship;
        Item = item;
    }

    public void Update(float delta)
    {
        // var ship = Hardpoint.Ship.Ship.transform;
        // var contacts =
        //     Physics.OverlapSphere(ship.position, _data.Range.Evaluate(Hardpoint)).Where(c=>c.attachedRigidbody?.GetComponent<Targetable>()!=null).Select(c=>c.attachedRigidbody.GetComponent<Targetable>());
        // foreach (var contact in contacts)
        // {
        //     var diff = (contact.transform.position - ship.position).Flatland();
        //     var angle = acos(dot(ship.forward.Flatland().normalized,
        //                     diff.normalized)) / (float)PI;
        //     var sens = _data.Sensitivity.Evaluate(Hardpoint) *
        //                Hardpoint.Ship.HullData.VisibilityCurve.Evaluate(saturate(angle));
        //     var vis = contact.Visibility / diff.sqrMagnitude;
        //     if(vis > 1/sens)
        //         Hardpoint.Ship.Contacts[contact] = Time.time;
        // }
        //
        // Hardpoint.Ship.VisibilitySources[this] = _data.Radiance.Evaluate(Hardpoint) / _data.RadianceMasking.Evaluate(Hardpoint);
        // TODO: Handle Active Detection / Visibility From Reflected Radiance
    }

    public void FixedUpdate(float delta)
    {
    }
}

[Inspectable]
[MessagePackObject]
public class AfterburnerBehaviorData : IItemBehaviorData
{
    [Inspectable] [Key("thrust")] public PerformanceStat ThrustModifier;
    [Inspectable] [Key("speed")]  public PerformanceStat SpeedModifier;
    [Inspectable] [Key("torque")]  public PerformanceStat TorqueModifier;
    public IItemBehavior CreateInstance(Ship ship, Gear item)
    {
        return new AfterburnerBehavior(this, ship, item);
    }
}

public class AfterburnerBehavior : IActivatedItemBehavior
{
    private List<Dictionary<IItemBehavior,float>> _modifiers = new List<Dictionary<IItemBehavior, float>>();
    private AfterburnerBehaviorData _data;

    public Ship Ship { get; }
    public Gear Item { get; }
    
    public IItemBehaviorData Data => _data;

    public AfterburnerBehavior(AfterburnerBehaviorData data, Ship ship, Gear item)
    {
        _data = data;
        Ship = ship;
        Item = item;
    }

    public void Update(float delta)
    {
    }

    public void FixedUpdate(float delta)
    {
    }
    
    public void Activate()
    {
        var thrustMod = (Ship.GetEquipped(HardpointType.Thruster).EquippedItemData as Thruster).Thrust.GetScaleModifiers(Ship);
        _modifiers.Add(thrustMod);
        thrustMod.Add(this,_data.ThrustModifier.Evaluate(Item, Ship));
        
        var speedMod = (Ship.Hull.EquippedItemData as Hull).TopSpeed.GetScaleModifiers(Ship);
        _modifiers.Add(speedMod);
        speedMod.Add(this,_data.SpeedModifier.Evaluate(Item, Ship));
        
        var torqueMod = (Ship.GetEquipped(HardpointType.Thruster).EquippedItemData as Thruster).Torque.GetScaleModifiers(Ship);
        _modifiers.Add(torqueMod);
        torqueMod.Add(this,_data.TorqueModifier.Evaluate(Item, Ship));
        
        Ship.ForceThrust = true;
    }

    public void Deactivate()
    {
        Ship.ForceThrust = false;
        foreach (var mod in _modifiers)
        {
            mod.Remove(this);
        }
        _modifiers.Clear();
    }
}

[Inspectable]
[MessagePackObject]
public class CannonBehaviorData : IItemBehaviorData
{
    [MessagePackFormatter(typeof(UnityGameObjectFormatter))]
    [Inspectable] [Key("bullet")]         public GameObject            BulletPrefab;
    // [MessagePackFormatter(typeof(ClipListFormatter))]
    // [Inspectable] [Key("sounds")]         public List<AudioClip>       Sounds;
    [Inspectable] [Key("damageType")]     public ProjectileType        DamageType;
    [Inspectable] [Key("burstCount")]     public int                   BurstCount;
    [Inspectable] [Key("burstTime")]      public PerformanceStat       BurstTime;
    [Inspectable] [Key("bulletRange")]    public PerformanceStat       Range;
    [Inspectable] [Key("spread")]         public PerformanceStat       Spread;
    [Inspectable] [Key("bulletInherit")]  public float                 Inherit;
    [Inspectable] [Key("deflection")]     public float                 Deflection;
    [Inspectable] [Key("visibility")]     public PerformanceStat       Visibility;
    [Inspectable] [Key("visibilityDecay")]public PerformanceStat       VisibilityDecay;
    [Inspectable] [Key("bulletVelocity")] public PerformanceStat       Velocity;
    [Inspectable] [Key("cooldown")]       public PerformanceStat       Cooldown;
    [Inspectable] [Key("damage")]         public PerformanceStat       Damage;
    [Inspectable] [Key("heat")]           public PerformanceStat       Heat;
    
    public IItemBehavior CreateInstance(Ship ship, Gear item)
    {
        return new CannonBehavior(this, ship, item);
    }

    [IgnoreMember] public PerformanceStat[] DynamicStats => new[] {BurstTime, Range, Spread, Velocity, Cooldown, Damage};
}

public class CannonBehavior : IActivatedItemBehavior
{
    private bool _firing;
    private float _cooldown; // normalized
    private CannonBehaviorData _cannon;
    public Ship Ship { get; }
    public Gear Item { get; }
    private float _firingVisibility;

    public CannonBehavior(CannonBehaviorData c, Ship ship, Gear item)
    {
        _cannon = c;
        Ship = ship;
        Item = item;
    }
    
    public void Activate()
    {
//        Debug.Log("Activating Cannon");
//         _firing = true;
//         Observable.EveryUpdate().TakeWhile(_ => _firing).Subscribe(_ =>
//         {
// //            Debug.Log($"Updating Observable {_cooldown}");
//             if (_cooldown < 0)
//             {
//                 _cooldown = 1;
//                 Fire(0);
//                 if(_cannon.BurstCount>1)
//                     Observable.Interval(TimeSpan.FromSeconds(_cannon.BurstTime.Evaluate(Hardpoint) / (_cannon.BurstCount-1))).Take(_cannon.BurstCount-1)
//                     .Subscribe(l => Fire(l+1));
//             }
//         });
    }

    private void Fire(long b)
    {
        // _firingVisibility += _cannon.Visibility.Evaluate(Hardpoint);
        // Hardpoint.Temperature += _cannon.Heat.Evaluate(Hardpoint) / Hardpoint.HeatCapacity;
        // var inst = GameObject.Instantiate(_cannon.BulletPrefab).transform;
        // Physics.IgnoreCollision(Hardpoint.Ship.Ship.GetComponent<Collider>(), inst.GetComponent<Collider>());
        // var bullet = inst.GetComponent<Bullet>();
        // bullet.Target = Hardpoint.Ship.Ship.Target;
        // bullet.Source = Hardpoint.Ship.Ship.Hitpoints;
        // bullet.Lifetime = _cannon.Range.Evaluate(Hardpoint) / _cannon.Velocity.Evaluate(Hardpoint);
        // bullet.Damage = _cannon.Damage.Evaluate(Hardpoint);
        // inst.position = Hardpoint.Proxy.position;
        // inst.rotation = Hardpoint.Proxy.rotation;
        // inst.GetComponent<Rigidbody>().velocity = _cannon.Velocity.Evaluate(Hardpoint) * Vector3.RotateTowards(-inst.forward, Hardpoint.Ship.Ship.Direction, _cannon.Deflection * Mathf.Deg2Rad, 1);
        // Hardpoint.Ship.Ship.AudioSource.PlayOneShot(_cannon.Sounds.RandomElement());
//        Debug.Log($"Firing bullet {b}");
    }

    public void Deactivate()
    {
        _firing = false;
    }

    public void Update(float delta)
    {
        _cooldown -= delta / _cannon.Cooldown.Evaluate(Item, Ship);

        if (_firingVisibility < 0.01f)
        {
            Ship.VisibilitySources.Remove(this);
        }
    }

    public void FixedUpdate(float delta)
    {
        _firingVisibility *= _cannon.VisibilityDecay.Evaluate(Item, Ship);
    }
    
    public IItemBehaviorData Data => _cannon;
}

[Inspectable]
[MessagePackObject]
public class MissileBehaviorData : IItemBehaviorData
{
    // [MessagePackFormatter(typeof(UnityGameObjectFormatter))]
    // [Inspectable] [Key("missile")]         public GameObject            MissilePrefab;
    // [MessagePackFormatter(typeof(ClipListFormatter))]
    // [Inspectable] [Key("sounds")]         public List<AudioClip>       Sounds;
    // [MessagePackFormatter(typeof(UnityAudioClipFormatter))]
    // [Inspectable] [Key("beepSound")]      public AudioClip             BeepSound;
    // [Inspectable] [Key("beepCount")]      public int                   Beeps;
    [Inspectable] [Key("damageType")]     public ProjectileType        DamageType;
    [Inspectable] [Key("burstCount")]     public int                   BurstCount;
    [Inspectable] [Key("burstTime")]      public PerformanceStat       BurstTime;
    [Inspectable] [Key("range")]          public PerformanceStat       Range;
    [Inspectable] [Key("guidance")]       public AnimationCurve        GuidanceCurve;
    [Inspectable] [Key("thrustCurve")]    public AnimationCurve        ThrustCurve;
    [Inspectable] [Key("liftCurve")]      public AnimationCurve        LiftCurve;
    [Inspectable] [Key("thrust")]         public PerformanceStat       Thrust;
    [Inspectable] [Key("deflection")]     public float                 Deflection;
    [Inspectable] [Key("lockOnBuffer")]   public float                 LockOnDegrees;
    [Inspectable] [Key("frequency")]      public float                 DodgeFrequency;
    [Inspectable] [Key("visibility")]     public PerformanceStat       Visibility;
    [Inspectable] [Key("launchSpeed")]    public PerformanceStat       LaunchSpeed;
    [Inspectable] [Key("missileSpeed")]   public PerformanceStat       MissileSpeed;
    [Inspectable] [Key("cooldown")]       public PerformanceStat       Cooldown;  
    [Inspectable] [Key("lockOnTime")]     public PerformanceStat       LockOnTime;
    [Inspectable] [Key("damage")]         public PerformanceStat       Damage;
    [Inspectable] [Key("heat")]           public PerformanceStat       Heat;

    
    public IItemBehavior CreateInstance(Ship ship, Gear item)
    {
        return new MissileBehavior(this, ship, item);
    }

    [IgnoreMember] public PerformanceStat[] DynamicStats => new[] {BurstTime, Range, Thrust, LaunchSpeed, MissileSpeed, Cooldown, Damage};
}

public class MissileBehavior : IActivatedItemBehavior
{
    private bool _locking;
    private float _lockingTimer;
    private float _cooldown; // normalized
    private bool _locked;
    
    private MissileBehaviorData _missile;
    public Ship Ship { get; }
    public Gear Item { get; }
    private float _firingVisibility;
    private AudioSource _audio;

    public MissileBehavior(MissileBehaviorData m, Ship ship, Gear item)
    {
        _missile  = m;
        Ship = ship;
        Item = item;
        // _audio = hp.Proxy.GetComponent<AudioSource>();
        // if(_audio==null)
        //     _audio = hp.Proxy.gameObject.AddComponent<AudioSource>();
        // _audio.clip = _missile.BeepSound;
        // _audio.loop = true;
        // _audio.volume = .5f;
    }
    
    public void Activate()
    {
        _locking = true;
        _lockingTimer = 1;
    }

    private void Fire(long b)
    {
        
        // _firingVisibility += _missile.Visibility.Evaluate(Hardpoint);
        // Hardpoint.Temperature += _missile.Heat.Evaluate(Hardpoint) / Hardpoint.HeatCapacity;
        // var inst = GameObject.Instantiate(_missile.MissilePrefab).transform;
        // var missile = inst.GetComponent<Bullet>();
        // missile.Target = Hardpoint.Ship.Ship.Target;
        // missile.Source = Hardpoint.Ship.Ship.Hitpoints;
        // missile.Lifetime = _missile.Range.Evaluate(Hardpoint) / _missile.MissileSpeed.Evaluate(Hardpoint);
        // missile.Damage = _missile.Damage.Evaluate(Hardpoint);
        // inst.position = Hardpoint.Proxy.position;
        // inst.rotation = Hardpoint.Proxy.rotation;
        // inst.GetComponent<Rigidbody>().velocity = _missile.LaunchSpeed.Evaluate(Hardpoint) * Hardpoint.Proxy.forward;
        // Physics.IgnoreCollision(Hardpoint.Ship.Ship.GetComponent<Collider>(), inst.GetComponent<Collider>());
        // var proj = inst.gameObject.AddComponent<GuidedProjectile>();
        // proj.GuidanceCurve = _missile.GuidanceCurve;
        // proj.ThrustCurve = _missile.ThrustCurve;
        // proj.LiftCurve = _missile.LiftCurve;
        // proj.TopSpeed = _missile.MissileSpeed.Evaluate(Hardpoint);
        // proj.Frequency = _missile.DodgeFrequency;
        // proj.Target = Hardpoint.Ship.Ship.Target;
        // proj.Source = Hardpoint.Ship.Ship.Hitpoints;
        // proj.Thrust = _missile.Thrust.Evaluate(Hardpoint);
        // _audio.PlayOneShot(_missile.Sounds.RandomElement());
//        Debug.Log($"Firing bullet {b}");
    }

    public void Deactivate()
    {
        // if (_lockingTimer < 0)
        // {
        //     Fire(0);
        //     if(_missile.BurstCount>1)
        //         Observable.Interval(TimeSpan.FromSeconds(_missile.BurstTime.Evaluate(Hardpoint) / (_missile.BurstCount-1))).Take(_missile.BurstCount-1)
        //             .Subscribe(l => Fire(l+1));
        //     _lockingTimer = _cooldown = 1;
        //     _locking = _locked = false;
        //     _audio.Stop();
        // }
        // _locking = false;
        // _lockingTimer = 1;
    }

    public void Update(float delta)
    {
        // if (Ship.Target == null)
        //     return;
        //
        // if (_locking && _cooldown < 0)
        // { 
        //     //if the crosshair stays under a certain LockOnBuffer distance for a certain amount of time (LockOnTime)
        //     if (acos(Vector2.Dot(Ship.Direction, (Ship.Target.transform.position-Ship.transform.position).normalized)) * Mathf.Rad2Deg < _missile.LockOnDegrees)
        //     {
        //         var d = delta / _missile.LockOnTime.Evaluate(Hardpoint);
        //         _lockingTimer -= d;
        //         if (!_locked && _lockingTimer < 0)
        //         {
        //             _locked = true;
        //             _audio.Play();
        //         } else if(Mathf.Repeat(_lockingTimer,1.0f/(_missile.Beeps+1))-d<0)
        //             GameManager.Instance.InterfaceAudio.PlayOneShot(_missile.BeepSound);
        //     }
        //     else
        //     {
        //         _lockingTimer = 1;
        //         _audio.Stop();
        //     }     
        // }
        // else
        // {
        //      _cooldown -= delta / _missile.Cooldown.Evaluate(Hardpoint);
        // }
        //  
        //
        // if (_firingVisibility > 0.01f)
        // {
        //     Hardpoint.Ship.Visibility += _firingVisibility;
        //     _firingVisibility = 0;
        // }
        
      
    }

    public void FixedUpdate(float delta)
    {
    }
    
    public IItemBehaviorData Data => _missile;
}

[MessagePackObject]
public class ReactorBehaviorData : IItemBehaviorData
{
    [Inspectable] [Key("charge")]      public PerformanceStat Charge;
    [Inspectable] [Key("capacitance")] public PerformanceStat Capacitance;
    [Inspectable] [Key("efficiency")]  public PerformanceStat Efficiency;
    [Inspectable] [Key("overload")]  public PerformanceStat OverloadEfficiency;
    [Inspectable] [Key("underload")]  public PerformanceStat UnderloadRecovery;
    
    public IItemBehavior CreateInstance(Ship ship, Gear item)
    {
        return new ReactorBehavior(this, ship, item);
    }

    [IgnoreMember] public PerformanceStat[] DynamicStats => new[] {Charge, Efficiency};
}

public class ReactorBehavior : IItemBehavior
{
    private ReactorBehaviorData _data;

    public Ship Ship { get; }
    public Gear Item { get; }

    public IItemBehaviorData Data => _data;

    public ReactorBehavior(ReactorBehaviorData data, Ship ship, Gear item)
    {
        _data = data;
        Ship = ship;
        Item = item;
    }

    public void FixedUpdate(float delta)
    {
        var cap = _data.Capacitance.Evaluate(Item, Ship);
        var charge = _data.Charge.Evaluate(Item, Ship) * delta;
        var efficiency = _data.Efficiency.Evaluate(Item, Ship);

        Ship.AddHeat(charge / efficiency);
        Ship.Charge += charge;

        if (Ship.Charge > cap)
        {
            Ship.AddHeat(-(Ship.Charge - cap) / efficiency * (1 - 1 / _data.UnderloadRecovery.Evaluate(Item, Ship)));
            Ship.Charge = cap;
        }

        if (Ship.Charge < 0)
        {
            Ship.AddHeat( -Ship.Charge / (_data.OverloadEfficiency.Evaluate(Item, Ship)));
            Ship.Charge = 0;
        }

    }

    public void Update(float delta)
    {
    }
}