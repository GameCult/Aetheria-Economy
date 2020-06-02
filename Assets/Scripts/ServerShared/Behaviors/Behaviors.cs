using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JsonKnownTypes;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using static Unity.Mathematics.noise;

public interface IBehavior
{
    bool Update(float delta);
    BehaviorData Data { get; }
}

// public interface IActivatedBehavior : IBehavior
// {
//     bool Activate();
//     void Deactivate();
// }

public interface IAnalogBehavior : IBehavior
{
    void SetAxis(float value);
}

public interface IDisposableBehavior
{
    void Dispose();
}

public interface IInitializableBehavior
{
    void Initialize();
}

public interface IAlwaysUpdatedBehavior
{
    void AlwaysUpdate(float delta);
}

public interface IPersistentBehavior//<T> where T : IBehavior
{
    PersistentBehaviorData Store();
    void Restore(PersistentBehaviorData data);
}

public interface IController
{
    bool Available { get; }
    TaskType TaskType { get; }
    Guid Zone { get; }
    void AssignTask(Guid task);
}

[MessagePackObject,
 Union(0, typeof(FactoryPersistence)),
 JsonObject(MemberSerialization.OptIn),
 JsonConverter(typeof(JsonKnownTypesConverter<PersistentBehaviorData>))]
public abstract class PersistentBehaviorData
{
}

[InspectableField, 
 Union(0, typeof(ProjectileWeaponData)), 
 Union(1, typeof(LauncherData)),
 Union(2, typeof(ReactorData)), 
 Union(3, typeof(RadiatorData)), 
 Union(4, typeof(StatModifierData)), 
 Union(5, typeof(SensorData)),
 Union(6, typeof(ReflectorData)),
 Union(7, typeof(ShieldData)),
 Union(8, typeof(ThrusterData)),
 Union(9, typeof(TurningData)),
 Union(10, typeof(VelocityConversionData)),
 Union(11, typeof(VelocityLimitData)),
 Union(12, typeof(FactoryData)),
 Union(13, typeof(PatrolControllerData)),
 Union(14, typeof(TowingControllerData)),
 Union(15, typeof(CooldownData)),
 Union(16, typeof(HeatData)),
 Union(17, typeof(HitscanData)),
 Union(18, typeof(ItemUsageData)),
 Union(19, typeof(RadianceData)),
 Union(20, typeof(SwitchData)),
 Union(21, typeof(TriggerData)),
 Union(22, typeof(VisibilityData)),
 Union(23, typeof(ThermotoggleData)),
 Union(24, typeof(EnergyDrawData)),
 Union(25, typeof(MiningControllerData)),
 Union(26, typeof(MiningToolData)),
 Union(27, typeof(SurveyControllerData)),
 Union(28, typeof(ResourceScannerData)),
 Union(29, typeof(WanderControllerData)),
 JsonConverter(typeof(JsonKnownTypesConverter<BehaviorData>)), JsonObject(MemberSerialization.OptIn)]
public abstract class BehaviorData
{
    [InspectableField, JsonProperty("group"), Key(0)]
    public int Group;
    
    public abstract IBehavior CreateInstance(GameContext context, Entity entity, Gear item);
}