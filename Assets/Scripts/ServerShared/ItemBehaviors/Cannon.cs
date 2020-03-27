using MessagePack;
using Newtonsoft.Json;

[RethinkTable("Items")]
[InspectableField]
[MessagePackObject]
[JsonObject(MemberSerialization.OptIn)]
public class CannonBehaviorData : IItemBehaviorData
{
    [InspectablePrefab] [JsonProperty("bullet")] [Key(0)]
    public string BulletPrefab;
    
    [InspectableField] [JsonProperty("damageType")] [Key(1)]
    public DamageType DamageType;

    [InspectableField] [JsonProperty("burstCount")] [Key(2)]
    public int BurstCount;

    [InspectableField] [JsonProperty("burstTime")] [Key(3)]
    public PerformanceStat BurstTime;

    [InspectableField] [JsonProperty("bulletRange")] [Key(4)]
    public PerformanceStat Range;

    [InspectableField] [JsonProperty("spread")] [Key(5)]
    public PerformanceStat Spread;

    [InspectableField] [JsonProperty("bulletInherit")] [Key(6)]
    public float Inherit;

    [InspectableField] [JsonProperty("deflection")] [Key(7)]
    public float Deflection;

    [InspectableField] [JsonProperty("visibility")] [Key(8)]
    public PerformanceStat Visibility;

    [InspectableField] [JsonProperty("visibilityDecay")] [Key(9)]
    public PerformanceStat VisibilityDecay;

    [InspectableField] [JsonProperty("bulletVelocity")] [Key(10)]
    public PerformanceStat Velocity;

    [InspectableField] [JsonProperty("cooldown")] [Key(11)]
    public PerformanceStat Cooldown;

    [InspectableField] [JsonProperty("damage")] [Key(12)]
    public PerformanceStat Damage;

    [InspectableField] [JsonProperty("heat")] [Key(13)]
    public PerformanceStat Heat;
    
    public IItemBehavior CreateInstance(GameContext context, Ship ship, Gear item)
    {
        return new CannonBehavior(context, this, ship, item);
    }
}

public class CannonBehavior : IActivatedItemBehavior
{
    private bool _firing;
    private float _cooldown; // normalized
    private CannonBehaviorData _cannon;
    public Ship Ship { get; }
    public Gear Item { get; }
    public GameContext Context { get; }
    private float _firingVisibility;

    public CannonBehavior(GameContext context, CannonBehaviorData c, Ship ship, Gear item)
    {
        Context = context;
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
        _cooldown -= delta / Context.Evaluate(_cannon.Cooldown, Item, Ship);

        if (_firingVisibility < 0.01f)
        {
            Ship.VisibilitySources.Remove(this);
        }
    }

    public void FixedUpdate(float delta)
    {
        _firingVisibility *= Context.Evaluate(_cannon.VisibilityDecay, Item, Ship);
    }
    
    public IItemBehaviorData Data => _cannon;
}