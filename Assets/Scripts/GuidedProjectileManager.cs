using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuidedProjectileManager : MonoBehaviour
{
    public Prototype ProjectilePrototype;

    public void Launch(LauncherData data, EquippedItem item, EntityInstance source, EntityInstance target)
    {
        var p = ProjectilePrototype.Instantiate<GuidedProjectile>();
        p.Source = source.Transform;
        p.SourceEntity = source.Entity;
        p.Target = target.Transform;
        p.Frequency = data.DodgeFrequency;
        p.Thrust = source.Entity.ItemManager.Evaluate(data.Thrust, item.EquippableItem, source.Entity);
        var hp = source.Entity.Hardpoints[item.Position.x, item.Position.y];
        var barrel = source.GetBarrel(hp);
        p.transform.position = barrel.position;
        p.Velocity = barrel.forward * source.Entity.ItemManager.Evaluate(data.LaunchSpeed, item.EquippableItem, source.Entity);
        p.Damage = source.Entity.ItemManager.Evaluate(data.Damage, item.EquippableItem, source.Entity);
        p.Penetration = source.Entity.ItemManager.Evaluate(data.Penetration, item.EquippableItem, source.Entity);
        p.Spread = source.Entity.ItemManager.Evaluate(data.DamageSpread, item.EquippableItem, source.Entity);
        p.DamageType = data.DamageType;
        p.GuidanceCurve = data.GuidanceCurve.ToCurve();
        p.LiftCurve = data.LiftCurve.ToCurve();
        p.ThrustCurve = data.ThrustCurve.ToCurve();
        p.Thrust = source.Entity.ItemManager.Evaluate(data.Thrust, item.EquippableItem, source.Entity);
        p.TopSpeed = source.Entity.ItemManager.Evaluate(data.MissileSpeed, item.EquippableItem, source.Entity);
    }
}
