using MessagePack;
using Newtonsoft.Json;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class HitscanData : WeaponData
{
    [InspectableField, JsonProperty("perSecond"), Key(6)]
    public bool PerSecond;
    
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
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
    private Gear Item { get; }
    private GameContext Context { get; }
    private float _firingVisibility;
    
    public BehaviorData Data => _data;
    public float Range { get; private set; }

    public Hitscan(GameContext context, HitscanData c, Entity entity, Gear item)
    {
        Context = context;
        _data = c;
        Entity = entity;
        Item = item;
    }

    public bool Update(float delta)
    {
        // TODO: Implement hitscan weapons!
        Range = Context.Evaluate(_data.Range, Item, Entity);
        return true;
    }

}