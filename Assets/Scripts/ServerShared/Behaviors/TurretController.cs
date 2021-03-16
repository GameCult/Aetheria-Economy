/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;
using UniRx;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using float3 = Unity.Mathematics.float3;

[MessagePackObject, JsonObject(MemberSerialization.OptIn), RuntimeInspectable]
public class TurretControllerData : BehaviorData
{
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new TurretController(context, this, entity, item);
    }
}

public class TurretController : IBehavior
{
    private TurretControllerData _data;
    private List<Weapon> _weapons = new List<Weapon>();
    private float _shotSpeed;
    private bool _predictShots;

    public Entity EquippedEntity { get; }
    public EquippedItem Item { get; }
    public ItemManager Context { get; }

    public BehaviorData Data => _data;

    public TurretController(ItemManager context, TurretControllerData data, Entity equippedEntity, EquippedItem item)
    {
        Context = context;
        _data = data;
        EquippedEntity = equippedEntity;
        Item = item;
        var weapons = equippedEntity.Equipment.Where(e => e.Behaviors.Any(b => b.Data is WeaponData));
        foreach (var weapon in weapons)
        {
            CheckWeapon(weapon);
        }

        equippedEntity.Equipment.ObserveAdd().Subscribe(add => CheckWeapon(add.Value));
        equippedEntity.Equipment.ObserveRemove().Subscribe(remove =>
        {
            foreach (var b in remove.Value.Behaviors)
                if (b is Weapon weapon)
                    _weapons.Remove(weapon);
        });
    }

    private void CheckWeapon(EquippedItem item)
    {
        foreach (var b in item.Behaviors)
            if (b is Weapon weapon)
            {
                _weapons.Add(weapon);
                var vel = weapon.Item.Evaluate(weapon.WeaponData.Velocity);
                if (vel > .1f)
                {
                    _predictShots = true;
                    _shotSpeed = vel;
                }
            }
    }

    public bool Execute(float delta)
    {
        if (EquippedEntity.Target.Value != null)
        {
            var diff = EquippedEntity.Target.Value.Position - EquippedEntity.Position;
            if (_predictShots)
            {
                var targetHullData = EquippedEntity.ItemManager.GetData(EquippedEntity.Target.Value.Hull) as HullData;
                var targetVelocity = float3(EquippedEntity.Target.Value.Velocity.x, 0, EquippedEntity.Target.Value.Velocity.y);
                var predictedPosition = AetheriaMath.FirstOrderIntercept(
                    EquippedEntity.Position, float3.zero, _shotSpeed,
                    EquippedEntity.Target.Value.Position, targetVelocity
                );
                predictedPosition.y = EquippedEntity.Zone.GetHeight(predictedPosition.xz) + targetHullData.GridOffset;
                EquippedEntity.LookDirection = normalize(predictedPosition - EquippedEntity.Position);
            }
            else
                EquippedEntity.LookDirection = normalize(diff);
            var dist = length(diff);

            foreach (var x in _weapons)
            {
                var data = x.Data as WeaponData;
                var fire = dot(
                    EquippedEntity.HardpointTransforms[EquippedEntity.Hardpoints[x.Item.Position.x, x.Item.Position.y]].direction,
                    EquippedEntity.LookDirection) > .99f;
                if (x.Item.Evaluate(data.Range) > dist && fire)
                {
                    x.Activate();
                }
                else if (x.Firing)
                    x.Deactivate();
            }
        }
        else
        {
            foreach (var x in _weapons)
            {
                if (x.Firing)
                    x.Deactivate();
            }
            EquippedEntity.Target.Value = EquippedEntity.VisibleHostiles.FirstOrDefault(e => e is Ship);
        }
        return true;
    }
}