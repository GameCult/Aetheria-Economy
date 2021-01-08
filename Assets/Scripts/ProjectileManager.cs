using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileManager : MonoBehaviour
{
    public Prototype ProjectilePrototype;

    public void Launch(ProjectileWeaponData data, EquippedItem item, EntityInstance source)
    {
        var p = ProjectilePrototype.Instantiate<Projectile>();
        p.SourceEntity = source.Entity;
        var hp = source.Entity.Hardpoints[item.Position.x, item.Position.y];
        var barrel = source.GetBarrel(hp);
        p.Range = source.Entity.ItemManager.Evaluate(data.Range, item.EquippableItem, source.Entity);
        p.StartPosition = p.transform.position = barrel.position;
        p.Velocity = barrel.forward * source.Entity.ItemManager.Evaluate(data.Velocity, item.EquippableItem, source.Entity);
        p.Damage = source.Entity.ItemManager.Evaluate(data.Damage, item.EquippableItem, source.Entity);
        p.Penetration = source.Entity.ItemManager.Evaluate(data.Penetration, item.EquippableItem, source.Entity);
        p.Spread = source.Entity.ItemManager.Evaluate(data.DamageSpread, item.EquippableItem, source.Entity);
        p.DamageType = data.DamageType;
        p.Zone = source.Entity.Zone;
        p.Trail.Clear();
    }
}
