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
    private Dictionary<Trigger, WeaponData> _triggers = new Dictionary<Trigger, WeaponData>();

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
            CheckWeapon(item);
        }

        equippedEntity.Equipment.ObserveAdd().Subscribe(add => CheckWeapon(add.Value));
        equippedEntity.Equipment.ObserveRemove().Subscribe(remove =>
        {
            foreach (var bg in remove.Value.BehaviorGroups)
                if (_triggers.ContainsKey(bg.Trigger))
                    _triggers.Remove(bg.Trigger);
        });
    }

    private void CheckWeapon(EquippedItem item)
    {
        foreach (var bg in item.BehaviorGroups)
        {
            if (bg.Trigger != null)
            {
                foreach (var b in bg.Behaviors)
                {
                    if(b.Data is WeaponData weaponData)
                        _triggers.Add(bg.Trigger, weaponData);
                }
            }
        }
    }

    public bool Execute(float delta)
    {
        if (EquippedEntity.Target.Value != null)
        {
            var diff = EquippedEntity.Target.Value.Position - EquippedEntity.Position;
            EquippedEntity.LookDirection = normalize(diff);
            var dist = length(diff);

            foreach (var x in _triggers)
            {
                if (Context.Evaluate(x.Value.Range, x.Key.Item.EquippableItem, EquippedEntity) > dist &&
                    dot(EquippedEntity.HardpointTransforms[EquippedEntity.Hardpoints[x.Key.Item.Position.x, x.Key.Item.Position.y]].direction,
                        EquippedEntity.LookDirection) > .9f)
                {
                    x.Key.Pull();
                }
            }
        }
        else
        {
            EquippedEntity.Target.Value = EquippedEntity.Zone.Entities.FirstOrDefault(e => e is Ship);
        }
        return true;
    }
}