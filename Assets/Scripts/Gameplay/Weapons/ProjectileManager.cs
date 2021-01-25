using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileManager : InstantWeaponEffectManager
{
    public Prototype ProjectilePrototype;

    public override void Fire(WeaponData data, EquippedItem item, EntityInstance source, EntityInstance target)
    {
        if(data is ProjectileWeaponData projectile)
        {
            var p = ProjectilePrototype.Instantiate<Projectile>();
            p.SourceEntity = source.Entity;
            var hp = source.Entity.Hardpoints[item.Position.x, item.Position.y];
            var barrel = source.GetBarrel(hp);
            p.Range = source.Entity.ItemManager.Evaluate(data.Range, item.EquippableItem, source.Entity);
            p.StartPosition = p.transform.position = barrel.position;
            var angle = source.Entity.ItemManager.Evaluate(projectile.Spread, item.EquippableItem, source.Entity) / 2;
            p.Velocity = (Quaternion.Euler(Random.Range(-angle, angle), Random.Range(-angle, angle), Random.Range(-angle, angle)) * barrel.forward) * 
                         source.Entity.ItemManager.Evaluate(projectile.Velocity, item.EquippableItem, source.Entity);
            p.Damage = source.Entity.ItemManager.Evaluate(data.Damage, item.EquippableItem, source.Entity);
            p.Penetration = source.Entity.ItemManager.Evaluate(data.Penetration, item.EquippableItem, source.Entity);
            p.Spread = source.Entity.ItemManager.Evaluate(data.DamageSpread, item.EquippableItem, source.Entity);
            p.DamageType = data.DamageType;
            p.Zone = source.Entity.Zone;
            p.Trail.Clear();
        }
        else Debug.LogError($"Weapon {item.EquippableItem.Name} linked to {name} effect, but is not a Projectile Weapon!");
    }
}
