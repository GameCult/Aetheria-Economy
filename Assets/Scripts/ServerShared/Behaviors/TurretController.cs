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
    public override IBehavior CreateInstance(EquippedItem item)
    {
        return new TurretController(this, item);
    }
}

public class TurretController : IBehavior
{
    private TurretControllerData _data;
    private List<Weapon> _weapons = new List<Weapon>();
    private float _shotSpeed;
    private bool _predictShots;

    public EquippedItem Item { get; }

    public BehaviorData Data => _data;

    public TurretController(TurretControllerData data, EquippedItem item)
    {
        _data = data;
        Item = item;
        var weapons = Item.Entity.Equipment.Where(e => e.Behaviors.Any(b => b.Data is WeaponData));
        foreach (var weapon in weapons)
        {
            CheckWeapon(weapon);
        }

        Item.Entity.Equipment.ObserveAdd().Subscribe(add => CheckWeapon(add.Value));
        Item.Entity.Equipment.ObserveRemove().Subscribe(remove =>
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
        if (Item.Entity.Target.Value != null)
        {
            var diff = Item.Entity.Target.Value.Position - Item.Entity.Position;
            if (_predictShots)
            {
                var targetHullData = Item.Entity.ItemManager.GetData(Item.Entity.Target.Value.Hull) as HullData;
                var targetVelocity = float3(Item.Entity.Target.Value.Velocity.x, 0, Item.Entity.Target.Value.Velocity.y);
                var predictedPosition = AetheriaMath.FirstOrderIntercept(
                    Item.Entity.Position, float3.zero, _shotSpeed,
                    Item.Entity.Target.Value.Position, targetVelocity
                );
                predictedPosition.y = Item.Entity.Zone.GetHeight(predictedPosition.xz) + targetHullData.GridOffset;
                Item.Entity.LookDirection = normalize(predictedPosition - Item.Entity.Position);
            }
            else
                Item.Entity.LookDirection = normalize(diff);
            var dist = length(diff);

            foreach (var x in _weapons)
            {
                var data = x.Data as WeaponData;
                var fire = dot(
                    Item.Entity.HardpointTransforms[Item.Entity.Hardpoints[x.Item.Position.x, x.Item.Position.y]].direction,
                    Item.Entity.LookDirection) > .99f;
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
            Item.Entity.Target.Value = Item.Entity.VisibleHostiles.FirstOrDefault(e => e is Ship);
        }
        return true;
    }
}