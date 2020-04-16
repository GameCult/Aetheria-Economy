using MessagePack;
using Newtonsoft.Json;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class ProjectileWeaponData : WeaponData, IBehaviorData
{
    [InspectablePrefab, JsonProperty("bullet"), Key(9)]  
    public string BulletPrefab;

    [InspectableField, JsonProperty("spread"), Key(10)]  
    public PerformanceStat Spread = new PerformanceStat();

    [InspectableField, JsonProperty("bulletInherit"), Key(11)]  
    public float Inherit;

    [InspectableField, JsonProperty("bulletVelocity"), Key(12)]  
    public PerformanceStat Velocity = new PerformanceStat();
    
    public IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new ProjectileWeapon(context, this, entity, item);
    }
}

public class ProjectileWeapon : IActivatedBehavior
{
    private bool _firing;
    private float _cooldown; // normalized
    private ProjectileWeaponData _projectileWeapon;
    public Entity Entity { get; }
    public Gear Item { get; }
    public GameContext Context { get; }
    private float _firingVisibility;

    public ProjectileWeapon(GameContext context, ProjectileWeaponData c, Entity entity, Gear item)
    {
        Context = context;
        _projectileWeapon = c;
        Entity = entity;
        Item = item;
    }
    
    public void Activate()
    {
//        Debug.Log("Activating ProjectileWeapon");
//         _firing = true;
//         Observable.EveryUpdate().TakeWhile(_ => _firing).Subscribe(_ =>
//         {
// //            Debug.Log($"Updating Observable {_cooldown}");
//             if (_cooldown < 0)
//             {
//                 _cooldown = 1;
//                 Fire(0);
//                 if(_projectileWeapon.BurstCount>1)
//                     Observable.Interval(TimeSpan.FromSeconds(_projectileWeapon.BurstTime.Evaluate(Hardpoint) / (_projectileWeapon.BurstCount-1))).Take(_projectileWeapon.BurstCount-1)
//                     .Subscribe(l => Fire(l+1));
//             }
//         });
    }

    private void Fire(long b)
    {
        // _firingVisibility += _projectileWeapon.Visibility.Evaluate(Hardpoint);
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
    }

    public void Deactivate()
    {
        _firing = false;
    }

    public void Initialize()
    {
    }

    public void Update(float delta)
    {
        _cooldown -= delta / Context.Evaluate(_projectileWeapon.Cooldown, Item, Entity);

        _firingVisibility *= Context.Evaluate(_projectileWeapon.VisibilityDecay, Item, Entity);
        
        if (_firingVisibility < 0.01f)
        {
            Entity.VisibilitySources.Remove(this);
        }
    }
    
    public IBehaviorData Data => _projectileWeapon;
}