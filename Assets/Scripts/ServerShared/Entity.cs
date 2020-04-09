
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public class Entity
{
    public Dictionary<IAnalogBehavior, float> AxisOverrides = new Dictionary<IAnalogBehavior, float>();
    public readonly Dictionary<object, float> VisibilitySources = new Dictionary<object, float>();
    public float Temperature;
    public float Energy;
    public float2 Position;
    public float2 Direction;
    
    protected readonly GameContext Context;
    protected readonly List<IBehavior> Behaviors;
    protected readonly Dictionary<Guid, List<IActivatedBehavior>> Bindings;
    protected readonly Dictionary<IAnalogBehavior, float> Axes;
    protected readonly List<Gear> Items;
    protected readonly List<ItemInstance> Cargo;

    public float Mass { get; private set; }
    public float SpecificHeat { get; private set; }
    public float Visibility => VisibilitySources.Values.Sum();

    public Entity(GameContext context, IEnumerable<Gear> items, IEnumerable<ItemInstance> cargo)
    {
        Context = context;

        Items = items.ToList();
        Cargo = cargo.ToList();
        
        Mass = Items.Sum(i => i.Mass) + 
               Cargo.Sum(ii => ii.Mass);
        
        SpecificHeat = Items.Sum(i => i.HeatCapacity) +
                       Cargo.Sum(ii => ii.HeatCapacity);
        
        Behaviors = Items
            .Where(i=>i.ItemData.Behaviors?.Any()??false)
            .SelectMany(i => i.ItemData.Behaviors
                .Select(bd => bd.CreateInstance(Context, this, i)))
            .OrderBy(b => b.GetType().GetCustomAttribute<UpdateOrderAttribute>()?.Order ?? 0).ToList();
        
        Bindings = Behaviors
            .Where(b => b is IActivatedBehavior)
            .GroupBy(b => b.Item.ID, (guid, behaviors) => new {guid, behaviors})
            .ToDictionary(g => g.guid, g => g.behaviors.Cast<IActivatedBehavior>().ToList());
        
        Axes = Behaviors
            .Where(b => b is IAnalogBehavior)
            .ToDictionary(b => b as IAnalogBehavior, b => 0f);
    }

    public IEnumerable<T> GetBehaviors<T>() where T : class, IBehavior
    {
        foreach (var behavior in Behaviors)
            if (behavior is T b)
                yield return b;
    }

    public IEnumerable<T> GetBehaviorData<T>() where T : class, IBehaviorData
    {
        foreach (var behavior in Behaviors)
            if (behavior.Data is T b)
                yield return b;
    }

    public void AddHeat(float heat)
    {
        Temperature += heat / SpecificHeat;
    }

    public virtual void Update(float delta)
    {
        foreach(var analogBehavior in Axes.Keys)
            analogBehavior.SetAxis(AxisOverrides.ContainsKey(analogBehavior) ? AxisOverrides[analogBehavior] : Axes[analogBehavior]);
        
        foreach(var behavior in Behaviors)
            behavior.Update(delta);

        foreach (var item in Items.Where(item => item.ItemData.Performance(Temperature) < .01f))
            item.Durability -= delta;
    }
}