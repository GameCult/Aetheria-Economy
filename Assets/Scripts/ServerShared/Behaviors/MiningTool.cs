/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn), Order(10)]
public class MiningToolData : BehaviorData
{
    [Inspectable, JsonProperty("dps"), Key(1)]
    public PerformanceStat DamagePerSecond = new PerformanceStat();
    
    [Inspectable, JsonProperty("efficiency"), Key(2)]
    public PerformanceStat Efficiency = new PerformanceStat();
    
    [Inspectable, JsonProperty("penetration"), Key(3)]
    public PerformanceStat Penetration = new PerformanceStat();
    
    [Inspectable, JsonProperty("range"), Key(4)]
    public PerformanceStat Range = new PerformanceStat();
    
    public override IBehavior CreateInstance(EquippedItem item)
    {
        return new MiningTool(this, item);
    }
}

public class MiningTool : IBehavior
{
    public Guid AsteroidBelt;
    public int Asteroid;
    
    private MiningToolData _data;

    private EquippedItem Item { get; }

    public BehaviorData Data => _data;
    public float Range { get; private set; }

    public MiningTool(MiningToolData data, EquippedItem item)
    {
        _data = data;
        Item = item;
    }

    public bool Execute(float dt)
    {
        Range = Item.Evaluate(_data.Range);
        var belt = Item.Entity.Zone.AsteroidBelts[AsteroidBelt];
        if (AsteroidBelt != Guid.Empty && 
            Item.Entity.Zone.AsteroidExists(AsteroidBelt, Asteroid) && 
            length(Item.Entity.Position.xz - belt.Positions[Asteroid].xz) - belt.Scales[Asteroid] < Range)
        {
            Item.Entity.Zone.MineAsteroid(
                Item.Entity,
                AsteroidBelt,
                Asteroid,
                Item.Evaluate(_data.DamagePerSecond) * dt,
                Item.Evaluate(_data.Efficiency),
                Item.Evaluate(_data.Penetration));
            return true;
        }

        return false;
    }
}