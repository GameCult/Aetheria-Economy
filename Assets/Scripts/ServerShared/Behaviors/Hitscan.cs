using MessagePack;
using Newtonsoft.Json;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class HitscanData : WeaponData
{
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new Hitscan(context, this, entity, item);
    }
}

public class Hitscan : IActivatedBehavior
{
    private bool _firing;
    private float _cooldown; // normalized
    private HitscanData _data;
    private Entity Entity { get; }
    private Gear Item { get; }
    private GameContext Context { get; }
    private float _firingVisibility;
    
    public BehaviorData Data => _data;

    public Hitscan(GameContext context, HitscanData c, Entity entity, Gear item)
    {
        Context = context;
        _data = c;
        Entity = entity;
        Item = item;
    }
    
    public bool Activate()
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
        return true;
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

    public bool Update(float delta)
    {
        _cooldown -= delta / Context.Evaluate(_data.Cooldown, Item, Entity);

        _firingVisibility *= Context.Evaluate(_data.VisibilityDecay, Item, Entity);
        
        if (_firingVisibility < 0.01f)
        {
            Entity.VisibilitySources.Remove(this);
        }
        return true;
    }

    public void Remove()
    {
    }
}