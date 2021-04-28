/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using MessagePack;
// using Newtonsoft.Json;
// using Unity.Mathematics;
// using static Unity.Mathematics.math;
//
// [MessagePackObject, JsonObject(MemberSerialization.OptIn), EntityTypeRestriction(HullType.Ship), Order(-100)]
// public class HaulingControllerData : ControllerData
// {
//     [InspectableField, JsonProperty("targetDistance"), Key(6)]  
//     public float DockTime = 2;
//     public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
//     {
//         return new HaulingController(context, this, entity, item);
//     }
// }
//
// public class HaulingController : ControllerBase<HaulingTask>, IBehavior, IPersistentBehavior
// {
//     private HaulingControllerData _data;
//     private EquippedItem Item { get; }
//     
//     private bool _taskStarted;
//     private int _itemsDelivered;
//     private int _itemsCarried;
//     private SimpleCommodity _simpleCommodityDelivery;
//     private List<CraftedItemInstance> _craftedItemDelivery = new List<CraftedItemInstance>();
//     
//     public HaulingController(ItemManager itemManager, HaulingControllerData data, Entity entity, EquippedItem item) : base(itemManager, data, entity)
//     {
//         _data = data;
//         Item = item;
//     }
//
//     public new bool Execute(float delta)
//     {
//         if (Task != null)
//         {
//             if (!_taskStarted)
//             {
//                 MoveTo(Task.Origin, true, OnPickup);
//                 //MoveTo(haulingTask.Zone, OnOriginZoneArrival);
//                 _taskStarted = true;
//             }
//         }
//         return base.Execute(delta);
//     }
//     
//     // void OnOriginZoneArrival()
//     // {
//     //     var haulingTask = _context.ItemData.Get<HaulingTask>(Task);
//     //     
//     //     _entity.SetMessage("Entering pickup phase.");
//     //     
//     //     MoveTo(_context.ItemData.Get<Entity>(haulingTask.Origin), true, OnPickup);
//     // }
//     
//     void OnPickup()
//     {
//         Entity.SetParent(Task.Origin);
//
//         _itemsCarried = Task.Origin.TryTransferItems(Entity, Task.ItemType, Task.Quantity - _itemsDelivered);
//
//         if (_itemsCarried == 0)
//         {
//             FinishTask();
//             return;
//         }
//         
//         Wait(_data.DockTime, OnItemObtained);
//     }
//
//     void OnItemObtained()
//     {
//         Entity.SetMessage("Entering delivery phase.");
//
//         MoveTo(Task.Target, true, OnDelivery);
//         //MoveTo(target.Zone.Data.ID, OnTargetZoneArrival);
//     }
//     
//     // void OnTargetZoneArrival()
//     // {
//     //     var haulingTask = _context.ItemData.Get<HaulingTask>(Task);
//     //     
//     //     MoveTo(_context.ItemData.Get<Entity>(haulingTask.Target), true, OnDelivery);
//     // }
//     
//     void OnDelivery()
//     {
//         Entity.SetParent(Task.Target);
//
//         var itemsDelivered = Entity.TryTransferItems(Task.Target, Task.ItemType, _itemsCarried);
//         _itemsCarried -= itemsDelivered;
//         _itemsDelivered += itemsDelivered;
//
//         Wait(_data.DockTime, () =>
//         {
//             // We were unable to transfer all onboard items to target entity, cargo is probably full. Bring back the surplus and end the mission
//             if(_itemsCarried > 0)
//                 MoveTo(Task.Origin, true, ReturnSurplusToOrigin);
//             // We have delivered the target quantity of items, end the mission
//             else if(_itemsDelivered >= Task.Quantity)
//                 FinishTask();
//             // We aren't done yet. Resetting the task will make us go for another delivery run
//             else
//                 _taskStarted = false;
//         });
//     }
//
//     void ReturnSurplusToOrigin()
//     {
//         Entity.SetParent(Task.Origin);
//
//         Entity.TryTransferItems(Task.Origin, Task.ItemType, _itemsCarried);
//         
//         Wait(_data.DockTime, OnItemObtained);
//         
//         FinishTask();
//     }
//
//     public PersistentBehaviorData Store()
//     {
//         return new HaulingControllerPersistence
//         {
//             Task = Task
//         };
//     }
//
//     public void Restore(PersistentBehaviorData data)
//     {
//         var towingControllerPersistence = data as HaulingControllerPersistence;
//         Task = towingControllerPersistence.Task;
//     }
// }
//
// public class HaulingControllerPersistence : PersistentBehaviorData
// {
//     [JsonProperty("task"), Key(0)]
//     public HaulingTask Task;
// }