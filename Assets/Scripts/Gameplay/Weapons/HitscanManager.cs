public class HitscanManager : InstantWeaponEffectManager
{
    public Prototype Prototype;
    public override void Fire(WeaponData data, EquippedItem item, EntityInstance source, EntityInstance target)
    {
        var p = Prototype.Instantiate<HitscanEffect>();
        p.SourceEntity = source.Entity;
        var hp = source.Entity.Hardpoints[item.Position.x, item.Position.y];
        var barrel = source.GetBarrel(hp);
        var t = p.transform;
        t.position = barrel.position;
        t.forward = barrel.forward;
        p.Range = source.Entity.ItemManager.Evaluate(data.Range, item.EquippableItem, source.Entity);
        p.Damage = source.Entity.ItemManager.Evaluate(data.Damage, item.EquippableItem, source.Entity);
        p.Penetration = source.Entity.ItemManager.Evaluate(data.Penetration, item.EquippableItem, source.Entity);
        p.Spread = source.Entity.ItemManager.Evaluate(data.DamageSpread, item.EquippableItem, source.Entity);
        p.DamageType = data.DamageType;
        p.Zone = source.Entity.Zone;
        p.Fire();
    }
}