/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

// [MessagePackObject, JsonObject(MemberSerialization.OptIn)]
// public class ParticleEffectData : BehaviorData
// {
//     [InspectablePrefab, JsonProperty("Particles"), Key(1)]
//     public string ParticlesPrefab;
//
//     [InspectableField, JsonProperty("thrust"), Key(2)]  
//     public bool AreaEmitter;
//     
//     public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
//     {
//         return new ParticleEffect(context, this, entity, item);
//     }
// }
//
// public class ParticleEffect : IBehavior, IAlwaysUpdatedBehavior
// {
//     private ParticleEffectData _data;
//
//     public Entity Entity { get; }
//     public EquippedItem Item { get; }
//     public ItemManager Context { get; }
//
//     public BehaviorData Data => _data;
//     
//     public float Emission { get; private set; }
//
//     public ParticleEffect(ItemManager context, ParticleEffectData data, Entity entity, EquippedItem item)
//     {
//         Context = context;
//         _data = data;
//         Entity = entity;
//         Item = item;
//     }
//
//     public float Execute(float delta, float input)
//     {
//         Emission = saturate(input);
//         return input;
//     }
//
//     public void Update(float delta)
//     {
//         Emission = 0;
//     }
// }