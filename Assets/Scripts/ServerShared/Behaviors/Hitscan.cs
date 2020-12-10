using MessagePack;
using Newtonsoft.Json;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class HitscanData : WeaponData
{
    [InspectableField, JsonProperty("perSecond"), Key(6)]
    public bool PerSecond;
    
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new Hitscan(context, this, entity, item);
    }
}

public class Hitscan : IBehavior
{
    private bool _firing;
    private float _cooldown; // normalized
    private HitscanData _data;
    private Entity Entity { get; }
    private EquippedItem Item { get; }
    private ItemManager Context { get; }
    private float _firingVisibility;
    
    public BehaviorData Data => _data;
    public float Range { get; private set; }

    public Hitscan(ItemManager context, HitscanData c, Entity entity, EquippedItem item)
    {
        Context = context;
        _data = c;
        Entity = entity;
        Item = item;
    }

    public bool Update(float delta)
    {
        // TODO: Implement hitscan weapons!
        Range = Context.Evaluate(_data.Range, Item.EquippableItem, Entity);
        return true;
    }

}