public class HitscanManager : InstantWeaponEffectManager
{
    public Prototype Prototype;
    public override void Fire(InstantWeapon weapon, EquippedItem item, EntityInstance source, EntityInstance target)
    {
        var p = Prototype.Instantiate<HitscanEffect>();
        p.SourceEntity = source.Entity;
        var hp = source.Entity.Hardpoints[item.Position.x, item.Position.y];
        var barrel = source.GetBarrel(hp);
        var t = p.transform;
        t.position = barrel.position;
        t.forward = barrel.forward;
        p.Damage = weapon.Damage;
        p.Penetration = weapon.Penetration;
        p.Spread = weapon.DamageSpread;
        p.DamageType = weapon.WeaponData.DamageType;
        p.Zone = source.Entity.Zone;
        p.Fire();
    }
}