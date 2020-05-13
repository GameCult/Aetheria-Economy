
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JsonKnownTypes;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, 
 Union(0, typeof(Ship)),
 Union(1, typeof(OrbitalEntity)),
 JsonObject(MemberSerialization.OptIn), JsonConverter(typeof(JsonKnownTypesConverter<Entity>))]
public abstract class Entity : DatabaseEntry, IMessagePackSerializationCallbackReceiver
{
    [JsonProperty("temperature"), Key(1)] public float Temperature;
    [JsonProperty("energy"), Key(2)] public float Energy;
    [JsonProperty("position"), Key(3)] public float2 Position;
    [JsonProperty("direction"), Key(4)] public float2 Direction = float2(0,1);
    [JsonProperty("velocity"), Key(5)] public float2 Velocity;
    [JsonProperty("cargo"), Key(6)] public readonly List<Guid> Cargo; // Type: ItemInstance
    [JsonProperty("equipment"), Key(7)] public readonly List<Guid> EquippedItems; // Type: Gear
    [JsonProperty("parent"), Key(8)] public Guid Parent; // Type: Entity
    [JsonProperty("children"), Key(9)] public List<Guid> Children = new List<Guid>(); // Type: Entity
    [JsonProperty("hull"), Key(10)] public Guid Hull; // Type: Gear
    [JsonProperty("persistedBehaviorData"), Key(11)] public Dictionary<Guid, PersistentBehaviorData[]> PersistedBehaviors;
    [JsonProperty("corporation"), Key(12)] public Guid Corporation;
    
    // [IgnoreMember] protected List<IBehavior> Behaviors;
    [IgnoreMember] public Guid Zone;
    [IgnoreMember] public Dictionary<int, List<IBehavior>> Behaviors;
    [IgnoreMember] public Dictionary<int, Switch> Switches;
    [IgnoreMember] public Dictionary<int, Trigger> Triggers;
    [IgnoreMember] public Dictionary<int, AxisSetting> Axes;
    [IgnoreMember] public Dictionary<int, float> AxisOverrides = new Dictionary<int, float>();
    [IgnoreMember] public readonly Dictionary<object, float> VisibilitySources = new Dictionary<object, float>();
    
    private Dictionary<Gear, EquippableItemData> GearData;
    private Dictionary<Gear, List<IBehavior>> ItemBehaviors;

    [IgnoreMember] public float Mass { get; private set; }
    [IgnoreMember] public float SpecificHeat { get; private set; }
    [IgnoreMember] public float Visibility => VisibilitySources.Values.Sum();


    public Entity(GameContext context, Guid hull, IEnumerable<Guid> items, IEnumerable<Guid> cargo, Guid zone)
    {
        ID = Guid.NewGuid();
        Context = context;
        Zone = zone;

        EquippedItems = items.ToList();
        Cargo = cargo.ToList();
        Hull = hull;

        RecalculateMass();

        AddItemBehaviors();
    }

    public void AddItemBehaviors()
    {
        var gear = EquippedItems.Append(Hull);
        var gearData = gear.ToDictionary(id => id, id => Context.Cache.Get<Gear>(id));
        GearData = gearData.Values.ToDictionary(g => g, g => g.ItemData);
        
        // Associate items with behavior list, used only for persistence
        ItemBehaviors = gear
            .Where(i=> gearData[i].ItemData.Behaviors?.Any()??false)
            .ToDictionary(i=> gearData[i], i => gearData[i].ItemData.Behaviors
                .OrderBy(bd => bd.GetType().GetCustomAttribute<OrderAttribute>()?.Order ?? 0)
                .Select(bd => bd.CreateInstance(Context, this, gearData[i]))
                .ToList());

        var behaviorGroups = ItemBehaviors
            .SelectMany(x => x.Value
                .Select(b => new {item = x.Key, behavior = b}))
            .GroupBy(ib => new {ib.item, ib.behavior.Data.Group});
        
        Behaviors = behaviorGroups
            .Select((grouping, i) => new {index = i, group = grouping})
            .ToDictionary(ig => ig.index, ig => ig.group.Select(x => x.behavior).ToList());
        
        Switches = Behaviors
            .Where(x => x.Value.Any(g => g is Switch))
            .Select((x, i) => new {index = i, s = x.Value.First(g => g is Switch) as Switch})
            .ToDictionary(x => x.index, x => x.s);
        
        Triggers = Behaviors
            .Where(x => x.Value.Any(g => g is Trigger))
            .Select((x, i) => new {index = i, s = x.Value.First(g => g is Trigger) as Trigger})
            .ToDictionary(x => x.index, x => x.s);
        
        Axes = Behaviors
            .Where(x => x.Value.Any(g => g is IAnalogBehavior))
            .Select((x, i) => new {index = i, behaviors = x.Value
                .Where(g => g is IAnalogBehavior)
                .Cast<IAnalogBehavior>().ToList()})
            .ToDictionary(x => x.index, x => new AxisSetting{Behaviors = x.behaviors, Value = 0f});
        
        // Axes = Behaviors.Values
        //     .SelectMany(behaviors=>behaviors)
        //     .Where(b => b is IAnalogBehavior)
        //     .ToDictionary(b => b as IAnalogBehavior, b => 0f);
        
        foreach (var behavior in Behaviors.Values
            .SelectMany(behaviors=>behaviors)
            .Where(behavior => behavior is IInitializableBehavior)
            .Cast<IInitializableBehavior>()) behavior.Initialize();
    }

    public void RecalculateMass()
    {
        Mass = Context.Cache.Get<Gear>(Hull).Mass + 
            EquippedItems.Select(i => Context.Cache.Get<Gear>(i)).Sum(i => i.Mass) + 
            Cargo.Select(i => Context.Cache.Get<ItemInstance>(i)).Sum(ii => ii.Mass) + 
            Children.Select(i => Context.Cache.Get<Entity>(i)).Sum(c=>c.Mass);
        
        SpecificHeat = Context.Cache.Get<Gear>(Hull).HeatCapacity + 
                       EquippedItems.Select(i => Context.Cache.Get<Gear>(i)).Sum(i => i.HeatCapacity) +
                       Cargo.Select(i => Context.Cache.Get<ItemInstance>(i)).Sum(ii => ii.HeatCapacity);
    }

    public IEnumerable<T> GetBehaviors<T>() where T : class, IBehavior
    {
        foreach (var behaviors in Behaviors.Values)
            foreach (var behavior in behaviors)
                if (behavior is T b)
                    yield return b;
    }

    public IEnumerable<T> GetBehaviorData<T>() where T : BehaviorData
    {
        foreach (var behaviors in Behaviors.Values)
            foreach (var behavior in behaviors)
                if (behavior.Data is T b)
                    yield return b;
    }

    public int GetAxis(IAnalogBehavior behavior)
    {
        var axis = Axes.FirstOrDefault(x => x.Value.Behaviors.Contains(behavior));
        if (axis.IsNull())
            return -1;
        return axis.Key;
    }

    public int GetAxis<T>() where T : class, IBehavior
    {
        foreach (var axis in Axes)
            foreach (var behavior in axis.Value.Behaviors)
                if (behavior is T)
                    return axis.Key;
        return -1;
    }

    public void AddHeat(float heat)
    {
        Temperature += heat / SpecificHeat;
    }

    public virtual void Update(float delta)
    {
        foreach(var axis in Axes)
            foreach(var analogBehavior in axis.Value.Behaviors)
                analogBehavior.SetAxis(AxisOverrides.ContainsKey(axis.Key) ? AxisOverrides[axis.Key] : Axes[axis.Key].Value);

        foreach (var group in Behaviors.Values)
            foreach(var behavior in group)
                if(!behavior.Update(delta))
                    break;

        foreach (var item in EquippedItems
            .Select(item => Context.Cache.Get<Gear>(item))
            .Where(item => item.ItemData.Performance(Temperature) < .01f))
            item.Durability -= delta;

        if (Parent != Guid.Empty)
        {
            var parent = Context.Cache.Get<Entity>(Parent);
            Position = parent.Position;
            Velocity = parent.Velocity;
        }

        // Processing stale item data properly was a pain in my ass, so the shortcut is just persisting and restoring everything
        if (GearData.Any(kvp => kvp.Key.ItemData != kvp.Value))
        {
            OnBeforeSerialize();
            OnAfterDeserialize();
        }
        
        // Here's the PITA way
        // foreach (var staleItem in GearData
        //     .Where(kvp=>kvp.Key.ItemData!=kvp.Value)
        //     .Select(kvp=>kvp.Key))
        // {
        //     // Item is stale, there's an updated one in the database cache
        //     //var actualItem = Context.Cache.Get<Gear>(staleItem.ID);
        //     var staleBehaviors = ItemBehaviors[staleItem];
        //     ItemBehaviors.Remove(staleItem);
        //     
        //     // We'll be reloading the item along with its behaviors, some data must be persisted
        //     var persistentData = staleBehaviors
        //         .Where(b => b is IPersistentBehavior)
        //         .Cast<IPersistentBehavior>()
        //         .Select(b => b.Store());
        //     
        //     // Regenerate the behavior list for the updated item
        //     ItemBehaviors[staleItem] = staleItem.ItemData.Behaviors
        //         .Select(bd => bd.CreateInstance(Context, this, staleItem))
        //         .OrderBy(b => b.GetType().GetCustomAttribute<UpdateOrderAttribute>()?.Order ?? 0).ToList();
        //     
        //     foreach(var behavior in ItemBehaviors[staleItem])
        //         behavior.Initialize();
        //     
        //     // Restore state for persisted behaviors
        //     foreach(var persistentBehaviorData in ItemBehaviors[staleItem]
        //         .Where(b => b is IPersistentBehavior)
        //         .Cast<IPersistentBehavior>()
        //         .Zip(persistentData, (behavior, data) => new {behavior, data}))
        //         persistentBehaviorData.behavior.Restore(persistentBehaviorData.data);
        //     
        //     // Regenerate activated behavior bindings
        //     if (Bindings.ContainsKey(staleItem))
        //     {
        //         Bindings.Remove(staleItem);
        //         Bindings[staleItem] = ItemBehaviors[staleItem]
        //             .Where(b => b is IActivatedBehavior)
        //             .Cast<IActivatedBehavior>().ToList();
        //     }
        //
        //     // Remove axis bindings and overrides for analog behaviors
        //     foreach (var axis in staleBehaviors.Where(b => b is IAnalogBehavior).Cast<IAnalogBehavior>())
        //     {
        //         Axes.Remove(axis);
        //         if (AxisOverrides.ContainsKey(axis))
        //             AxisOverrides.Remove(axis);
        //     }
        //
        //     // Regenerate axis bindings for analog behaviors
        //     foreach (var axis in ItemBehaviors[staleItem].Where(b => b is IAnalogBehavior).Cast<IAnalogBehavior>())
        //         Axes[axis] = 0;
        // }
    }

    public void OnBeforeSerialize()
    {
        // Filter item behavior collections by those with any persistent behaviors
        // For each item create an object containing the item ID and a list of persistent behaviors
        // Then turn that into a dictionary mapping from item ID to an array of every behaviors persistent data
        PersistedBehaviors = ItemBehaviors
            .Where(x => x.Value.Any(b=>b is IPersistentBehavior))
            .Select(x => new {gear=x.Key, behaviors = x.Value
                .Where(b=>b is IPersistentBehavior)
                .Cast<IPersistentBehavior>()})
            .ToDictionary(x=> x.gear.ID, x=>x.behaviors.Select(b => b.Store()).ToArray());
    }

    public void OnAfterDeserialize()
    {
        RecalculateMass();
        
        AddItemBehaviors();

        var index = 0;
        // Iterate only over the behaviors of items which contain persistent data
        // Filter the behaviors for each item to get the persistent ones, then cast them and combine with the persisted data array for that item
        foreach (var persistentBehaviorData in ItemBehaviors
            .Where(itemBehaviors => PersistedBehaviors.ContainsKey(itemBehaviors.Key.ID)).
            SelectMany(itemBehaviors => itemBehaviors.Value
                .Where(b=> b is IPersistentBehavior)
                .Cast<IPersistentBehavior>()
                .Zip(PersistedBehaviors[itemBehaviors.Key.ID], (behavior, data) => new{behavior, data})))
            persistentBehaviorData.behavior.Restore(persistentBehaviorData.data);
    }
}

public class AxisSetting
{
    public float Value;
    public List<IAnalogBehavior> Behaviors;
}