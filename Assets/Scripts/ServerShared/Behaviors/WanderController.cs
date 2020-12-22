/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;
using RethinkDb.Driver.Ast;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, JsonObject(MemberSerialization.OptIn), EntityTypeRestriction(HullType.Ship), Order(-100)]
public class WanderControllerData : ControllerData
{
    [InspectableField, JsonProperty("randomDockTime"), Key(6)]  
    public float RandomDockTime = 5;
    
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new WanderController(context, this, entity, item);
    }
}

public class WanderController : ControllerBase<WanderTask>, IBehavior
{
    public WanderTarget WanderTarget;
    public BehaviorData Data => _data;
    
    private WanderControllerData _data;
    private EquippedItem Item { get; }
    private Guid _target;
    private float _dockTime = -1;
    
    public WanderController(ItemManager itemManager, WanderControllerData data, Entity entity, EquippedItem item) : base(itemManager, data, entity)
    {
        _data = data;
        Item = item;
    }

    public new bool Update(float delta)
    {
        if (!Moving && _dockTime < 0)
        {
            NextTarget();
        }

        _dockTime -= delta;
        return base.Update(delta);
    }

    private void NextTarget()
    {
        if (WanderTarget == WanderTarget.Planets)
        {
            var planets = Zone.Planets.Values.ToArray();
            var randomPlanet = planets[ItemManager.Random.NextInt(planets.Length)];
            MoveTo(() => Zone.GetOrbitPosition(randomPlanet.Orbit), () => Zone.GetOrbitVelocity(randomPlanet.Orbit));
        }
        else if (WanderTarget == WanderTarget.Orbitals)
        {
            var entities = Zone.Entities.Where(e=>e is OrbitalEntity).ToArray();
            var randomEntity = entities[ItemManager.Random.NextInt(entities.Length)];
            MoveTo(randomEntity, true, () =>
            {
                Entity.SetParent(randomEntity);
                _dockTime = ItemManager.Random.NextFloat(_data.RandomDockTime);
            });
        }
    }
}

public enum WanderTarget
{
    Planets,
    Orbitals
}