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

// [MessagePackObject, JsonObject(MemberSerialization.OptIn), EntityTypeRestriction(HullType.Ship), Order(-100)]
// public class MiningControllerData : ControllerData
// {
//     public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
//     {
//         return new MiningController(context, this, entity, item);
//     }
// }
//
// public class MiningController : ControllerBase<Mining>, IBehavior, IPersistentBehavior, IInitializableBehavior
// {
//     
//     private MiningControllerData _data;
//     private EquippedItem Item { get; }
//     private bool _taskStarted;
//     private int _asteroid = -1;
//     private MiningTool _miningTool;
//     private Switch _toolSwitch;
//     
//     public MiningController(ItemManager itemManager, MiningControllerData data, Entity entity, EquippedItem item) : base(itemManager, data, entity)
//     {
//         _data = data;
//         Item = item;
//     }
//
//     public new void Initialize()
//     {
//         // _miningTool = Entity.GetBehavior<MiningTool>();
//         // _toolSwitch = Entity.GetSwitch<MiningTool>();
//         base.Initialize();
//     }
//
//     public new bool Execute(float delta)
//     {
//         // if (Task != null)
//         // {
//         //     if (!_taskStarted)
//         //     {
//         //         NextAsteroid();
//         //         _taskStarted = true;
//         //     }
//         //     else
//         //     {
//         //         if (!Moving)
//         //         {
//         //             if (_entity.OccupiedCapacity < _entity.Capacity - 1)
//         //             {
//         //                 var asteroidTransform = Zone.GetAsteroidTransform(Task.Asteroids, _asteroid);
//         //                 if (length(_entity.Position - asteroidTransform.xy) - asteroidTransform.w > _miningTool.Range)
//         //                 {
//         //                     _entity.SetMessage("Moving to target asteroid.");
//         //                     MoveTo(() => Zone.GetAsteroidTransform(Task.Asteroids, _asteroid).xy, 
//         //                         () => Zone.GetAsteroidVelocity(Task.Asteroids, _asteroid));
//         //                     _toolSwitch.Activated = false;
//         //                 }
//         //                 else
//         //                 {
//         //                     if (Zone.AsteroidExists(Task.Asteroids, _asteroid))
//         //                     {
//         //                         _miningTool.AsteroidBelt = Task.Asteroids;
//         //                         _miningTool.Asteroid = _asteroid;
//         //                         _toolSwitch.Activated = true;
//         //                         Aim.Objective = Zone.GetAsteroidTransform(Task.Asteroids, _asteroid).xy;
//         //                         Aim.Update(delta);
//         //                     }
//         //                     else NextAsteroid();
//         //                 }
//         //             }
//         //             else
//         //             {
//         //                 _entity.SetMessage("Out of cargo space. Returning home to offload cargo.");
//         //                 GoHome(OnArriveHome);
//         //             }
//         //         }
//         //     }
//         // }
//         return base.Execute(delta);
//     }
//
//     // private void OnArriveHome()
//     // {
//     //     var homeEntity = _context.ItemData.Get<Entity>(HomeEntity);
//     //     foreach (var item in _entity.Cargo
//     //         .Select(id => _context.ItemData.Get<ItemInstance>(id))
//     //         .Where(item => item is SimpleCommodity)
//     //         .Cast<SimpleCommodity>()
//     //         .ToArray())
//     //     {
//     //         int quantity = min((int) ((homeEntity.Capacity - homeEntity.OccupiedCapacity) / _context.GetData(item).Size), item.Quantity);
//     //         var newItem = _entity.RemoveCargo(item, quantity);
//     //         homeEntity.AddCargo(newItem);
//     //         
//     //         if (quantity < item.Quantity)
//     //         {
//     //             homeEntity.SetMessage("Colony is out of cargo space. Closing Mining Task.");
//     //             FinishTask();
//     //             _taskStarted = false;
//     //             return;
//     //         }
//     //     }
//     // }
//
//     private void NextAsteroid()
//     {
//         _asteroid = Zone.NearestAsteroid(Task.Asteroids, Entity.Position.xz);
//         Entity.SetMessage("Selecting new asteroid.");
//     }
//
//     public PersistentBehaviorData Store()
//     {
//         return new MiningControllerPersistence
//         {
//             Task = Task
//         };
//     }
//
//     public void Restore(PersistentBehaviorData data)
//     {
//         var miningControllerPersistence = data as MiningControllerPersistence;
//         Task = miningControllerPersistence.Task;
//     }
// }
//
// public class MiningControllerPersistence : PersistentBehaviorData
// {
//     [JsonProperty("task"), Key(0)]
//     public Mining Task;
// }