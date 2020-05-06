using MessagePack;
using Newtonsoft.Json;

[Union(0, typeof(ProjectileWeaponData)),
 Union(1, typeof(LauncherData))]
public abstract class WeaponData : BehaviorData
{
    [InspectableField, JsonProperty("damageType"), Key(0)]
    public DamageType DamageType;

    [InspectableField, JsonProperty("damage"), Key(1)]  
    public PerformanceStat Damage = new PerformanceStat();

    [InspectableField, JsonProperty("range"), Key(2)]  
    public PerformanceStat Range = new PerformanceStat();

    [InspectableField, JsonProperty("cooldown"), Key(3)]  
    public PerformanceStat Cooldown = new PerformanceStat();

    [InspectableField, JsonProperty("heat"), Key(4)]  
    public PerformanceStat Heat = new PerformanceStat();

    [InspectableField, JsonProperty("visibility"), Key(5)]  
    public PerformanceStat Visibility = new PerformanceStat();

    [InspectableField, JsonProperty("visibilityDecay"), Key(6)]  
    public PerformanceStat VisibilityDecay = new PerformanceStat();

    [InspectableField, JsonProperty("burstCount"), Key(7)]  
    public int BurstCount;

    [InspectableField, JsonProperty("burstTime"), Key(8)]  
    public PerformanceStat BurstTime = new PerformanceStat();
}