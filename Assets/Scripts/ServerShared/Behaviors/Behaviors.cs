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
    void Initialize();
    void Update(float delta);
    BehaviorData Data { get; }
}

public interface IActivatedBehavior : IBehavior
{
    void Activate();
    void Deactivate();
}

public interface IAnalogBehavior : IBehavior
{
    void SetAxis(float value);
}

public interface IPersistentBehavior//<T> where T : IBehavior
{
    PersistentBehaviorData Store();
    void Restore(PersistentBehaviorData data);
}

public interface IController
{
    bool Available { get; }
    TaskType JobType { get; }
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
 Union(4, typeof(AfterburnerData)), 
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
 JsonConverter(typeof(JsonKnownTypesConverter<BehaviorData>)), JsonObject(MemberSerialization.OptIn)]
public abstract class BehaviorData
{
    // [InspectableField, JsonProperty("index"), Key(0)]
    // public int Index;
    
    public abstract IBehavior CreateInstance(GameContext context, Entity entity, Gear item);
}