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
    
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new StatModifier(context, this, entity, item);
    }
}

[Order(-4)]
public class StatModifier : IBehavior, IInitializableBehavior, IDisposableBehavior
{
    private StatModifierData _data;
    private Entity Entity { get; }
    private EquippedItem Item { get; }
    private ItemManager Context { get; }

    public BehaviorData Data => _data;

    private PerformanceStat[] _stats;
    
    private static Type[] _statObjects;

    private static Type[] StatObjects => _statObjects = _statObjects ?? typeof(BehaviorData).GetAllChildClasses()
        .Concat(typeof(EquippableItemData).GetAllChildClasses()).ToArray();

    public StatModifier(ItemManager context, StatModifierData data, Entity entity, EquippedItem item)
    {
        _data = data;
        Entity = entity;
        Item = item;
        Context = context;
    }

    public void Initialize()
    {
        var targetType = StatObjects.FirstOrDefault(so => so.Name == _data.Stat.Target);
        if (targetType != null)
        {
            var statField = targetType.GetFields()
                .Where(f => f.FieldType == typeof(PerformanceStat)).FirstOrDefault(f => f.Name == _data.Stat.Stat);
            if (statField != null)
            {
                if (typeof(EquippableItemData).IsAssignableFrom(targetType))
                    _stats = Entity.Equipment
                        .Select(hp => hp.EquippableItem)
                        .Where(gear => _data.RequireBehavior == null || Context.GetData(gear).Behaviors.Any(behavior => behavior.GetType() == _data.RequireBehavior))
                        .Where(gear => Context.GetData(gear).GetType() == targetType)
                        .Select(gear => statField.GetValue(Context.GetData(gear)) as PerformanceStat)
                        .ToArray();
                else
                    _stats = Entity.Equipment
                        .Select(hp => hp.EquippableItem)
                        .Where(gear => _data.RequireBehavior == null || Context.GetData(gear).Behaviors.Any(behavior => behavior.GetType() == _data.RequireBehavior))
                        .SelectMany(gear => Context.GetData(gear).Behaviors)
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
                : stat.GetScaleModifiers(Entity))[this] = Context.Evaluate(_data.Modifier, Item.EquippableItem, Entity);
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