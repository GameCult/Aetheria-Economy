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
public class TowingControllerData : ControllerData
{
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new TowingController(context, this, entity, item);
    }
}

public class TowingController : ControllerBase<StationTowing>, IBehavior, IPersistentBehavior
{
    
    private TowingControllerData _data;
    private EquippedItem Item { get; }
    private bool _taskStarted;
    
    public TowingController(ItemManager itemManager, TowingControllerData data, Entity entity, EquippedItem item) : base(itemManager, data, entity)
    {
        _data = data;
        Item = item;
    }

    public new bool Execute(float delta)
    {
        if (Task != null)
        {
            if (!_taskStarted)
            {
                MoveTo(Task.Station, true, OnPickup);
                _taskStarted = true;
            }
        }
        return base.Execute(delta);
    }
    
    // void OnZoneArrival()
    // {
    //     var towingTask = _context.ItemData.Get<StationTowing>(Task);
    //     
    //     _entity.SetMessage("Entering pickup phase.");
    //     
    //     MoveTo(_context.ItemData.Get<Entity>(towingTask.Station), true, OnPickup);
    // }
    
    void OnPickup()
    {
        Task.Station.SetParent(Entity);
        
        Entity.SetMessage("Entering delivery phase.");

        MoveTo(() =>
        {
            var orbitParent = Zone.GetOrbitPosition(Task.OrbitParent);
            var parentToUs = Entity.Position.xz - orbitParent;
            return orbitParent + normalize(parentToUs) * Task.OrbitDistance;
        }, () => Zone.GetOrbitVelocity(Task.OrbitParent), OnDelivery);
    }
    
    void OnDelivery()
    {
        Task.Station.RemoveParent();
        
        Entity.SetMessage("Target delivered. Returning Home.");

        var orbit = Zone.CreateOrbit(Task.OrbitParent, Entity.Position.xz);
        Task.Station.OrbitData = orbit.ID;

        FinishTask();
        _taskStarted = false;
    }

    public PersistentBehaviorData Store()
    {
        return new TowingControllerPersistence
        {
            Task = Task
        };
    }

    public void Restore(PersistentBehaviorData data)
    {
        var towingControllerPersistence = data as TowingControllerPersistence;
        Task = towingControllerPersistence.Task;
    }
}

public class TowingControllerPersistence : PersistentBehaviorData
{
    [JsonProperty("task"), Key(0)]
    public StationTowing Task;
}