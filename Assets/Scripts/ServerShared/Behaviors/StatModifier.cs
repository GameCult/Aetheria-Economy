using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class StatModifierData : BehaviorData
{
    [InspectableField, JsonProperty("stat"), Key(1)]  
    public StatReference Stat = new StatReference();
    
    [InspectableField, JsonProperty("modifier"), Key(2)]  
    public PerformanceStat Modifier = new PerformanceStat();
    
    [InspectableField, JsonProperty("type"), Key(3)]  
    public StatModifierType Type;

    [InspectableType(typeof(BehaviorData)), JsonProperty("requireBehavior"), Key(4)]
    public Type RequireBehavior;
    
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new StatModifier(context, this, entity, item);
    }
}

[Order(-4)]
public class StatModifier : IBehavior, IInitializableBehavior, IDisposableBehavior
{
    private StatModifierData _data;
    private Entity Entity { get; }
    private Gear Item { get; }
    private GameContext Context { get; }

    public BehaviorData Data => _data;

    private PerformanceStat[] _stats;

    public StatModifier(GameContext context, StatModifierData data, Entity entity, Gear item)
    {
        _data = data;
        Entity = entity;
        Item = item;
        Context = context;
    }

    public void Initialize()
    {
        var targetType = Context.StatObjects.FirstOrDefault(so => so.Name == _data.Stat.Target);
        if (targetType != null)
        {
            var statField = targetType.GetFields()
                .Where(f => f.FieldType == typeof(PerformanceStat)).FirstOrDefault(f => f.Name == _data.Stat.Stat);
            if (statField != null)
            {
                if (typeof(EquippableItemData).IsAssignableFrom(targetType))
                    _stats = Entity.EquippedItems
                        .Select(id => Context.Cache.Get<Gear>(id))
                        .Where(gear => _data.RequireBehavior == null || gear.ItemData.Behaviors.Any(behavior => behavior.GetType() == _data.RequireBehavior))
                        .Where(gear => gear.ItemData.GetType() == targetType)
                        .Select(gear => statField.GetValue(gear.ItemData) as PerformanceStat)
                        .ToArray();
                else
                    _stats = Entity.EquippedItems
                        .Select(id => Context.Cache.Get<Gear>(id))
                        .Where(gear => _data.RequireBehavior == null || gear.ItemData.Behaviors.Any(behavior => behavior.GetType() == _data.RequireBehavior))
                        .SelectMany(gear => gear.ItemData.Behaviors)
                        .Where(behaviorData => behaviorData.GetType() == targetType)
                        .Select(behaviorData => statField.GetValue(behaviorData) as PerformanceStat)
                        .ToArray();
            }
        }
    }

    public bool Update(float delta)
    {
        foreach (var stat in _stats)
            (_data.Type == StatModifierType.Constant
                ? stat.GetConstantModifiers(Entity)
                : stat.GetScaleModifiers(Entity))[this] = Context.Evaluate(_data.Modifier, Item, Entity);
        return true;
    }

    public void Dispose()
    {
        foreach (var stat in _stats)
            (_data.Type == StatModifierType.Constant ? stat.GetConstantModifiers(Entity) : stat.GetScaleModifiers(Entity)).Remove(this);
    }
}

public enum StatModifierType
{
    Constant,
    Multiplier
}