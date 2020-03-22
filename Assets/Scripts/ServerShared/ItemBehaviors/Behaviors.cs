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

public interface IItemBehavior
{
    void Update(float delta);
    void FixedUpdate(float delta);
    Ship Ship { get; }
    Gear Item { get; }
    GameContext Context { get; }
    IItemBehaviorData Data { get; }
}

public interface IActivatedItemBehavior : IItemBehavior
{
    void Activate();
    void Deactivate();
}

[InspectableField]
[Union(0, typeof(CannonBehaviorData))]
[Union(1, typeof(LauncherBehaviorData))]
[Union(2, typeof(ReactorBehaviorData))]
// [Union(3, typeof(PlusUltraBehaviorData))]
[Union(4, typeof(AfterburnerBehaviorData))]
[Union(5, typeof(SensorBehaviorData))]
[JsonConverter(typeof(JsonKnownTypesConverter<IItemBehaviorData>))]
[JsonObject(MemberSerialization.OptIn)]
public interface IItemBehaviorData
{
    IItemBehavior CreateInstance(GameContext context, Ship ship, Gear item);
}