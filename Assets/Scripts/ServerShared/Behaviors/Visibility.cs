using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class VisibilityData : BehaviorData
{
    [InspectableField, JsonProperty("visibility"), Key(1)]  
    public PerformanceStat Visibility = new PerformanceStat();

    [InspectableField, JsonProperty("visibilityDecay"), Key(2)]  
    public PerformanceStat VisibilityDecay = new PerformanceStat();
    
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new Visibility(context, this, entity, item);
    }
}

public class Visibility : IBehavior, IAlwaysUpdatedBehavior
{
    private VisibilityData _data;

    private Entity Entity { get; }
    private Gear Item { get; }
    private GameContext Context { get; }

    public BehaviorData Data => _data;

    private float _cooldown; // Normalized

    public Visibility(GameContext context, VisibilityData data, Entity entity, Gear item)
    {
        _data = data;
        Entity = entity;
        Item = item;
        Context = context;
    }

    public bool Update(float delta)
    {
        Entity.VisibilitySources[this] = Context.Evaluate(_data.Visibility, Item, Entity);
        return true;
    }

    public void AlwaysUpdate(float delta)
    {
        // TODO: Time independent decay?
        Entity.VisibilitySources[this] *= Context.Evaluate(_data.VisibilityDecay, Item, Entity);
        
        if (Entity.VisibilitySources[this] < 0.01f) Entity.VisibilitySources.Remove(this);
    }
}