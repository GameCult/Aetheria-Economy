using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuidedProjectileManager : InstantWeaponEffectManager
{
    public Prototype ProjectilePrototype;

    public override void Fire(WeaponData data, EquippedItem item, EntityInstance source, EntityInstance target)
    {
        if(data is LauncherData launcher)
        {
            var p = ProjectilePrototype.Instantiate<GuidedProjectile>();
            p.Source = source.Transform;
            p.SourceEntity = source.Entity;
            p.Target = target.Transform;
            p.Frequency = launcher.DodgeFrequency;
            p.Thrust = source.Entity.ItemManager.Evaluate(launcher.Thrust, item.EquippableItem, source.Entity);
            var hp = source.Entity.Hardpoints[item.Position.x, item.Position.y];
            var barrel = source.GetBarrel(hp);
            p.StartPosition = p.transform.position = barrel.position;
            p.Range = source.Entity.ItemManager.Evaluate(data.Range, item.EquippableItem, source.Entity);
            p.Velocity = barrel.forward * source.Entity.ItemManager.Evaluate(launcher.LaunchSpeed, item.EquippableItem, source.Entity);
            p.Damage = source.Entity.ItemManager.Evaluate(data.Damage, item.EquippableItem, source.Entity);
            p.Penetration = source.Entity.ItemManager.Evaluate(data.Penetration, item.EquippableItem, source.Entity);
            p.Spread = source.Entity.ItemManager.Evaluate(data.DamageSpread, item.EquippableItem, source.Entity);
            p.DamageType = data.DamageType;
            p.GuidanceCurve = launcher.GuidanceCurve.ToCurve();
            p.LiftCurve = launcher.LiftCurve.ToCurve();
            p.ThrustCurve = launcher.ThrustCurve.ToCurve();
            p.Thrust = source.Entity.ItemManager.Evaluate(launcher.Thrust, item.EquippableItem, source.Entity);
            p.TopSpeed = source.Entity.ItemManager.Evaluate(launcher.Velocity, item.EquippableItem, source.Entity);
        }
        else Debug.LogError($"Weapon {item.EquippableItem.Name} linked to {name} effect, but is not a Launcher!");
    }
}
