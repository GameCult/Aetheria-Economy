using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserManager : InstantWeaponEffectManager
{
    public Prototype ProjectilePrototype;

    public override void Fire(WeaponData data, EquippedItem item, EntityInstance source, EntityInstance target)
    {
        var p = ProjectilePrototype.Instantiate<Laser>();
        p.SourceEntity = source.Entity;
        var hp = source.Entity.Hardpoints[item.Position.x, item.Position.y];
        var barrel = source.GetBarrel(hp);
        p.Range = source.Entity.ItemManager.Evaluate(data.Range, item.EquippableItem, source.Entity);
        var t = p.transform;
        t.SetParent(barrel);
        t.forward = barrel.forward;
        t.position = barrel.position;
        p.Damage = source.Entity.ItemManager.Evaluate(data.Damage, item.EquippableItem, source.Entity);
        p.Penetration = source.Entity.ItemManager.Evaluate(data.Penetration, item.EquippableItem, source.Entity);
        p.Spread = source.Entity.ItemManager.Evaluate(data.DamageSpread, item.EquippableItem, source.Entity);
        p.DamageType = data.DamageType;
    }
}
