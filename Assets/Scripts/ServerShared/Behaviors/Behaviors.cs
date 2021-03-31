/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

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
    bool Execute(float delta);
    BehaviorData Data { get; }
}

public interface IActivatedBehavior : IBehavior
{
    void Activate();
    void Deactivate();
}

public interface IAnalogBehavior : IBehavior
{
    float Axis { get; set; }
}

public interface IDisposableBehavior
{
    void Dispose();
}

public interface IEventBehavior
{
    void ResetEvents();
}

public interface IInitializableBehavior
{
    void Initialize();
}

public interface IInteractiveBehavior
{
    bool Exposed { get; }
}

public interface IAlwaysUpdatedBehavior
{
    void Update(float delta);
}

public interface IProgressBehavior
{
    float Progress { get; }
}

public interface IOrderedBehavior
{
    int Order { get; }
}

public interface IPersistentBehavior//<T> where T : IBehavior
{
    PersistentBehaviorData Store();
    void Restore(PersistentBehaviorData data);
}

public interface IPopulationAssignment
{
    int AssignedPopulation { get; set; }
}

[MessagePackObject,
 JsonObject(MemberSerialization.OptIn),
 JsonConverter(typeof(JsonKnownTypesConverter<PersistentBehaviorData>))]
public abstract class PersistentBehaviorData
{
}

[Inspectable, 
 Union(0, typeof(GuidedWeaponData)),
 Union(1, typeof(LauncherData)),
 Union(2, typeof(ReactorData)), 
 Union(3, typeof(RadiatorData)), 
 Union(4, typeof(StatModifierData)), 
 Union(5, typeof(SensorData)),
 Union(6, typeof(ReflectorData)),
 Union(7, typeof(ShieldData)),
 Union(8, typeof(ThrusterData)),
 Union(9, typeof(WearData)),
 Union(10, typeof(VelocityConversionData)),
 Union(11, typeof(VelocityLimitData)),
 // Union(13, typeof(PatrolControllerData)),
 // Union(14, typeof(TowingControllerData)),
 Union(15, typeof(CooldownData)),
 Union(16, typeof(HeatData)),
 // Union(17, typeof(HitscanData)),
 Union(18, typeof(ItemUsageData)),
 Union(19, typeof(RadianceData)),
 Union(20, typeof(SwitchData)),
 Union(21, typeof(TriggerData)),
 Union(22, typeof(VisibilityData)),
 Union(23, typeof(ThermotoggleData)),
 Union(24, typeof(EnergyDrawData)),
 // Union(25, typeof(MiningControllerData)),
 Union(26, typeof(MiningToolData)),
 // Union(27, typeof(SurveyControllerData)),
 Union(28, typeof(ResourceScannerData)),
 // Union(29, typeof(WanderControllerData)),
 // Union(30, typeof(HaulingControllerData)),
 Union(31, typeof(CapacitorData)),
 Union(32, typeof(CockpitData)),
 Union(33, typeof(HeatStorageData)),
 Union(34, typeof(TurretControllerData)),
 Union(35, typeof(InstantWeaponData)),
 Union(36, typeof(ConstantWeaponData)),
 Union(37, typeof(ChargedWeaponData)),
 Union(38, typeof(AutoWeaponData)),
 JsonConverter(typeof(JsonKnownTypesConverter<BehaviorData>)), JsonObject(MemberSerialization.OptIn)]
public abstract class BehaviorData
{
    [Inspectable, JsonProperty("group"), Key(0)]
    public int Group;
    
    public abstract IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item);

    public override string ToString()
    {
        var className = base.ToString();
        var dataIndex = className.IndexOf("data", StringComparison.InvariantCultureIgnoreCase);
        return dataIndex>0 ? className.Substring(0,dataIndex).SplitCamelCase() : className;
    }
}