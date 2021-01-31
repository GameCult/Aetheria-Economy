using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuidedProjectileManager : InstantWeaponEffectManager
{
    public Prototype ProjectilePrototype;

    public override void Fire(InstantWeapon weapon, EquippedItem item, EntityInstance source, EntityInstance target)
    {
        if(weapon.Data is LauncherData launcher)
        {
            var p = ProjectilePrototype.Instantiate<GuidedProjectile>();
            p.Source = source.Transform;
            p.SourceEntity = source.Entity;
            p.Target = target.Transform;
            p.Frequency = launcher.DodgeFrequency;
            var hp = source.Entity.Hardpoints[item.Position.x, item.Position.y];
            var barrel = source.GetBarrel(hp);
            p.StartPosition = p.transform.position = barrel.position;
            p.Damage = weapon.Damage;
            p.Range = weapon.Range;
            p.Penetration = weapon.Penetration;
            p.Spread = weapon.DamageSpread;
            p.DamageType = weapon.WeaponData.DamageType;
            p.GuidanceCurve = launcher.GuidanceCurve.ToCurve();
            p.LiftCurve = launcher.LiftCurve.ToCurve();
            p.ThrustCurve = launcher.ThrustCurve.ToCurve();
            p.Velocity = barrel.forward * weapon.Velocity;
            p.Thrust = source.Entity.ItemManager.Evaluate(launcher.Thrust, item.EquippableItem, source.Entity);
            p.TopSpeed = source.Entity.ItemManager.Evaluate(launcher.MissileVelocity, item.EquippableItem, source.Entity);
        }
        else Debug.LogError($"Weapon {item.EquippableItem.Name} linked to {name} effect, but is not a Launcher!");
    }
}
