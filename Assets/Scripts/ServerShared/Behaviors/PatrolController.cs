/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, JsonObject(MemberSerialization.OptIn), EntityTypeRestriction(HullType.Ship), Order(-100)]
public class PatrolControllerData : ControllerData
{
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new PatrolController(context, this, entity, item);
    }
}

public class PatrolController : IBehavior, IInitializableBehavior
{
    public BehaviorData Data => _data;
    
    private PatrolControllerData _data;
    private ItemManager Context { get; }
    private Entity Entity { get; }
    private EquippedItem Item { get; }
    private Locomotion _locomotion;
    private Guid _targetOrbit;
    
    public PatrolController(ItemManager context, PatrolControllerData data, Entity entity, EquippedItem item)
    {
        Context = context;
        _data = data;
        Entity = entity;
        Item = item;
    }
    
    public void Initialize()
    {
        _locomotion = new Locomotion(Context, Entity, _data);
        RandomTarget();
    }

    public bool Update(float delta)
    {
        _locomotion.Objective = Entity.Zone.GetOrbitPosition(_targetOrbit);
        _locomotion.Update(delta);
        
        if(length(Entity.Position.xz - _locomotion.Objective) < _data.TargetDistance)
            RandomTarget();
        
        return true;
    }
    
    private void RandomTarget()
    {
        _targetOrbit = Entity.Zone.Planets.Keys.ToArray()[Context.Random.NextInt(Entity.Zone.Planets.Count)];
    }
}
