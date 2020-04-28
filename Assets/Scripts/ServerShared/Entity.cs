
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
    public float2 Direction = float2(0,1);
    public float2 Velocity;
    public Dictionary<Guid, List<IActivatedBehavior>> Bindings;
    public Dictionary<IAnalogBehavior, float> Axes;
    public readonly GameContext Context;
    public readonly List<ItemInstance> Cargo;
    public readonly List<Gear> EquippedItems;
    
    protected readonly List<IBehavior> Behaviors;
    
    public Gear Hull { get; }

    public float Mass { get; private set; }
    public float SpecificHeat { get; private set; }
    public float Visibility => VisibilitySources.Values.Sum();

    public Entity(GameContext context, Gear hull, IEnumerable<Gear> items, IEnumerable<ItemInstance> cargo)
    {
        Context = context;

        EquippedItems = items.ToList();
        Cargo = cargo.ToList();
        Hull = hull;

        RecalculateMass();

        var activeItems = Hull == null ? EquippedItems : EquippedItems.Append(Hull);
        
        Behaviors = activeItems
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
        
        foreach (var behavior in Behaviors)
        {
            behavior.Initialize();
        }
    }

    public void RecalculateMass()
    {
        Mass = Hull?.Mass ?? 0 + 
            EquippedItems.Sum(i => i.Mass) + 
            Cargo.Sum(ii => ii.Mass);
        
        SpecificHeat = (Hull?.HeatCapacity ?? 0) + 
                       EquippedItems.Sum(i => i.HeatCapacity) +
                       Cargo.Sum(ii => ii.HeatCapacity);
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

        foreach (var item in EquippedItems.Where(item => item.ItemData.Performance(Temperature) < .01f))
            item.Durability -= delta;
    }
}