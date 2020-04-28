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
    Entity Entity { get; }
    Gear Item { get; }
    GameContext Context { get; }
    IBehaviorData Data { get; }
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
    object Store();
    IBehavior Restore(GameContext context, Entity entity, Gear item, Guid data);
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
 JsonConverter(typeof(JsonKnownTypesConverter<IBehaviorData>)), JsonObject(MemberSerialization.OptIn)]
public interface IBehaviorData
{
    IBehavior CreateInstance(GameContext context, Entity entity, Gear item);
}