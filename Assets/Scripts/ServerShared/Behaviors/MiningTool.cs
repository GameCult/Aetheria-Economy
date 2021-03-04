/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn), Order(10)]
public class MiningToolData : BehaviorData
{
    [InspectableField, JsonProperty("dps"), Key(1)]
    public PerformanceStat DamagePerSecond = new PerformanceStat();
    
    [InspectableField, JsonProperty("efficiency"), Key(2)]
    public PerformanceStat Efficiency = new PerformanceStat();
    
    [InspectableField, JsonProperty("penetration"), Key(3)]
    public PerformanceStat Penetration = new PerformanceStat();
    
    [InspectableField, JsonProperty("range"), Key(4)]
    public PerformanceStat Range = new PerformanceStat();
    
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new MiningTool(context, this, entity, item);
    }
}

public class MiningTool : IBehavior
{
    public Guid AsteroidBelt;
    public int Asteroid;
    
    private MiningToolData _data;

    private Entity Entity { get; }
    private EquippedItem Item { get; }
    private ItemManager Context { get; }

    public BehaviorData Data => _data;
    public float Range { get; private set; }

    public MiningTool(ItemManager context, MiningToolData data, Entity entity, EquippedItem item)
    {
        _data = data;
        Entity = entity;
        Item = item;
        Context = context;
    }

    public bool Execute(float delta)
    {
        Range = Context.Evaluate(_data.Range, Item);
        var belt = Entity.Zone.AsteroidBelts[AsteroidBelt];
        if (AsteroidBelt != Guid.Empty && 
            Entity.Zone.AsteroidExists(AsteroidBelt, Asteroid) && 
            length(Entity.Position.xz - belt.Positions[Asteroid].xz) - belt.Scales[Asteroid] < Range)
        {
            Entity.Zone.MineAsteroid(
                Entity,
                AsteroidBelt,
                Asteroid,
                Context.Evaluate(_data.DamagePerSecond, Item) * delta,
                Context.Evaluate(_data.Efficiency, Item),
                Context.Evaluate(_data.Penetration, Item));
            return true;
        }

        return false;
    }
}