
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
public abstract class Entity : DatabaseEntry, IMessagePackSerializationCallbackReceiver, INamedEntry
{
    [JsonProperty("temperature"), Key(1)]
    public float Temperature;
    
    [JsonProperty("energy"), Key(2)]
    public float Energy;
    
    [JsonProperty("position"), Key(3)]
    public float2 Position;
    
    [JsonProperty("direction"), Key(4)]
    public float2 Direction = float2(0,1);
    
    [JsonProperty("velocity"), Key(5)]
    public float2 Velocity;
    
    [JsonProperty("cargo"), Key(6)]
    public readonly List<Guid> Cargo; // Type: ItemInstance
    
    [JsonProperty("equipment"), Key(7)]
    public readonly List<Guid> EquippedItems; // Type: Gear
    
    [JsonProperty("parent"), Key(8)]
    public Guid Parent; // Type: Entity
    
    [JsonProperty("children"), Key(9)]
    public List<Guid> Children = new List<Guid>(); // Type: Entity
    
    [JsonProperty("hull"), Key(10)]
    public Guid Hull; // Type: Gear
    
    [JsonProperty("persistedBehaviorData"), Key(11)]
    public Dictionary<Guid, PersistentBehaviorData[]> PersistedBehaviors;
    
    [JsonProperty("corporation"), Key(12)]
    public Guid Corporation;
    
    [JsonProperty("name"), Key(13)]
    public string Name;
    
    [JsonProperty("population"), Key(14)]
    public int Population;
    
    [JsonProperty("personality"), Key(15)]
    public Dictionary<Guid, float> Personality = new Dictionary<Guid, float>();
    
    [JsonProperty("incompleteGear"), Key(16)]
    public Dictionary<Guid, double> IncompleteGear = new Dictionary<Guid, double>();
    //public List<IncompleteItem> IncompleteGear = new List<IncompleteItem>();
    
    [JsonProperty("incompleteGear"), Key(17)]
    public Dictionary<Guid, double> IncompleteCargo = new Dictionary<Guid, double>();
    //public List<IncompleteItem> IncompleteCargo = new List<IncompleteItem>();
    
    // [IgnoreMember] protected List<IBehavior> Behaviors;
    [IgnoreMember] public Guid Zone;
    [IgnoreMember] public Dictionary<int, List<IBehavior>> Behaviors;
    [IgnoreMember] public Dictionary<int, Switch> Switches;
    [IgnoreMember] public Dictionary<int, Trigger> Triggers;
    [IgnoreMember] public Dictionary<int, AxisSetting> Axes;
    [IgnoreMember] public Dictionary<int, float> AxisOverrides = new Dictionary<int, float>();
    [IgnoreMember] public readonly Dictionary<object, float> VisibilitySources = new Dictionary<object, float>();
    [IgnoreMember] public readonly Dictionary<string, float> Messages = new Dictionary<string, float>();
    [IgnoreMember] public Dictionary<Gear, EquippableItemData> GearData;
    [IgnoreMember] public Dictionary<Gear, List<IBehavior>> ItemBehaviors;

    [IgnoreMember] public float Mass { get; private set; }
    [IgnoreMember] public float OccupiedCapacity { get; private set; }
    [IgnoreMember] public float SpecificHeat { get; private set; }
    [IgnoreMember] public float Visibility => VisibilitySources.Values.Sum();

    public event Action OnInventoryUpdate;
    
    public float Capacity
    {
        get
        {
            var hull = Context.Cache.Get<Gear>(Hull);
            var hullData = Context.Cache.Get<HullData>(hull.Data);
            return Context.Evaluate(hullData.Capacity, hull, this);
        }
    }

    [IgnoreMember] public string EntryName
    {
        get => Name;
        set => Name = value;
    }


    public Entity(GameContext context, Guid hull, IEnumerable<Guid> items, IEnumerable<Guid> cargo, Guid zone, Guid corporation)
    {
        Context = context;
        Zone = zone;
        Corporation = corporation;

        EquippedItems = items.ToList();
        Cargo = cargo.ToList();
        Hull = hull;

        RecalculateMass();

        AddItemBehaviors();
    }

    public void AddItemBehaviors()
    {
        var gearIDs = EquippedItems.Append(Hull);
        var gear = gearIDs.ToDictionary(id => id, id => Context.Cache.Get<Gear>(id));
        GearData = gear.Values.ToDictionary(g => g, g => g.ItemData);
        
        // Associate items with behavior list, used for persistence and convenience
        ItemBehaviors = gearIDs
            .Where(i=> gear[i].ItemData.Behaviors?.Any()??false)
            .ToDictionary(i=> gear[i], i => gear[i].ItemData.Behaviors
                .OrderBy(bd => bd.GetType().GetCustomAttribute<OrderAttribute>()?.Order ?? 0)
                .Select(bd => bd.CreateInstance(Context, this, gear[i]))
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

    public void Equip(Gear gear)
    {
        if (Cargo.Contains(gear.ID)) Cargo.Remove(gear.ID);
        else if (IncompleteGear.Any(ig => ig.Key == gear.ID))
            IncompleteGear.Remove(IncompleteGear.First(ig => ig.Key == gear.ID).Key);
        else
            throw new ArgumentException("Attempted to equip gear which is not present in inventory!");

        EquippedItems.Add(gear.ID);
        OnBeforeSerialize();
        OnAfterDeserialize();
    }

    // public Guid Build(BlueprintData blueprint)
    // {
    //     var gear = Context.CreateInstance(blueprint.Item, blueprint.Quality * .5f, blueprint.Quality);
    //     IncompleteGear[gear.ID] = blueprint.ProductionTime;
    //     RecalculateMass();
    //     return gear.ID;
    //     // IncompleteGear.Add(new IncompleteItem
    //     // {
    //     //     Item = gear.ID,
    //     //     Blueprint = blueprint.ID,
    //     //     RemainingTime = blueprint.ProductionTime
    //     // });
    // }

    public List<Guid> Build(BlueprintData blueprint, float quality, string name, bool equip = false)
    {
        if (equip)
        {
            if(blueprint.FactoryItem!=Guid.Empty)
                throw new ArgumentException("Attempted to directly build a blueprint which requires a factory!");
            var blueprintItem = Context.Cache.Get<GearData>(blueprint.Item);
            if(blueprintItem == null)
                throw new ArgumentException("Attempted to directly build a blueprint for a missing or incompatible item!");
        }
        
        var newItems = new List<Guid>();
        if (GetBlueprintIngredients(blueprint, out var simpleIngredients, out var compoundIngredients))
        {
            var ingredients = new List<ItemInstance>();
            ingredients.AddRange(compoundIngredients);
            foreach (var compoundCommodity in compoundIngredients)
            {
                Cargo.Remove(compoundCommodity.ID);
            }
            foreach (var simpleCommodity in simpleIngredients)
            {
                var blueprintQuantity = blueprint.Ingredients
                    .First(ingredient => ingredient.Key == simpleCommodity.ItemData.ID).Value;
                if (simpleCommodity.Quantity == blueprintQuantity)
                {
                    ingredients.Add(simpleCommodity);
                    Cargo.Remove(simpleCommodity.ID);
                }
                else
                {
                    var newSimpleCommodity = new SimpleCommodity
                    {
                        Context = Context,
                        Data = simpleCommodity.Data,
                        Quantity = blueprintQuantity
                    };
                    simpleCommodity.Quantity -= blueprintQuantity;
                    ingredients.Add(newSimpleCommodity);
                }
            }
            
            var blueprintItemData = Context.Cache.Get(blueprint.Item);
            
            if (blueprintItemData is CraftedItemData)
            {
                for (int i = 0; i < blueprint.Quantity; i++)
                {
                    CraftedItemInstance newItem;
                
                    if (blueprintItemData is EquippableItemData equippableItemData)
                    {
                        var newGear = new Gear
                        {
                            Context = Context,
                            Data = blueprintItemData.ID,
                            Ingredients = ingredients.Select(ii=>ii.ID).ToList(),
                            Quality = quality,
                            Blueprint = blueprint.ID,
                            Name = name
                        };
                        newGear.Durability = Context.Evaluate(equippableItemData.Durability, newGear);
                        newItem = newGear;
                    }
                    else
                    {
                        newItem = new CompoundCommodity
                        {
                            Context = Context,
                            Data = blueprintItemData.ID,
                            Ingredients = ingredients.Select(ii=>ii.ID).ToList(),
                            Quality = quality,
                            Blueprint = blueprint.ID,
                            Name = name
                        };
                    }
                    Context.Cache.Add(newItem);
                    if (equip)
                        IncompleteGear[newItem.ID] = blueprint.ProductionTime;
                    else 
                        IncompleteCargo[newItem.ID] = blueprint.ProductionTime;
                    newItems.Add(newItem.ID);
                    // IncompleteCargo.Add(new IncompleteItem
                    // {
                    //     Item = newItem.ID,
                    //     Blueprint = blueprint.ID,
                    //     RemainingTime = blueprint.ProductionTime
                    // });
                }
            }
            else
            {
                var newSimpleCommodity = new SimpleCommodity
                {
                    Context = Context,
                    Data = blueprintItemData.ID,
                    Quantity = blueprint.Quantity
                };
                Context.Cache.Add(newSimpleCommodity);
                IncompleteCargo[newSimpleCommodity.ID] = blueprint.ProductionTime;
                newItems.Add(newSimpleCommodity.ID);
                // IncompleteCargo.Add(new IncompleteItem
                // {
                //     Item = newSimpleCommodity.ID,
                //     Blueprint = blueprint.ID,
                //     RemainingTime = blueprint.ProductionTime
                // });
            }
                
            RecalculateMass();
        }

        return newItems;
    }

    public bool GetBlueprintIngredients(BlueprintData blueprint, out List<SimpleCommodity> simpleIngredients,
        out List<CompoundCommodity> compoundIngredients)
    {
        simpleIngredients = new List<SimpleCommodity>();
        compoundIngredients = new List<CompoundCommodity>();
        var hasAllIngredients = true;
        var cargoInstances = Cargo.Select(c => Context.Cache.Get<ItemInstance>(c));
        foreach (var kvp in blueprint.Ingredients)
        {
            var itemData = Context.Cache.Get(kvp.Key);
            if (itemData is SimpleCommodityData)
            {
                var matchingItem = cargoInstances.FirstOrDefault(ii =>
                {
                    if (!(ii is SimpleCommodity simpleCommodity)) return false;
                    return simpleCommodity.Data == itemData.ID && simpleCommodity.Quantity >= kvp.Value;
                }) as SimpleCommodity;
                hasAllIngredients = hasAllIngredients && matchingItem != null;
                if(matchingItem != null)
                    simpleIngredients.Add(matchingItem);
            }
            else
            {
                var matchingItems =
                    cargoInstances.Where(ii => (ii as CompoundCommodity)?.Data == itemData.ID).Cast<CompoundCommodity>().ToArray();
                hasAllIngredients = hasAllIngredients && matchingItems.Length >= kvp.Value;
                if(matchingItems.Length >= kvp.Value)
                    compoundIngredients.AddRange(matchingItems.Take(kvp.Value));
            }
        }

        return hasAllIngredients;
    }

    public void RecalculateMass()
    {
        Mass = Context.Cache.Get<Gear>(Hull).Mass + 
            EquippedItems.Select(i => Context.Cache.Get<Gear>(i)).Sum(i => i.Mass) + 
            IncompleteGear.Select(i => Context.Cache.Get<Gear>(i.Key)).Sum(i => i.Mass) + 
            Cargo.Select(i => Context.Cache.Get<ItemInstance>(i)).Sum(ii => ii.Mass) + 
            IncompleteCargo.Select(i => Context.Cache.Get<ItemInstance>(i.Key)).Sum(ii => ii.Mass) + 
            Children.Select(i => Context.Cache.Get<Entity>(i)).Sum(c=>c.Mass);
        
        SpecificHeat = Context.Cache.Get<Gear>(Hull).HeatCapacity + 
                       EquippedItems.Select(i => Context.Cache.Get<Gear>(i)).Sum(i => i.HeatCapacity) +
                       IncompleteGear.Select(i => Context.Cache.Get<Gear>(i.Key)).Sum(i => i.HeatCapacity) + 
                       Cargo.Select(i => Context.Cache.Get<ItemInstance>(i)).Sum(ii => ii.HeatCapacity) + 
                       IncompleteCargo.Select(i => Context.Cache.Get<ItemInstance>(i.Key)).Sum(ii => ii.HeatCapacity) + 
                       Children.Select(i => Context.Cache.Get<Entity>(i)).Sum(c=>c.SpecificHeat);

        OccupiedCapacity = EquippedItems.Select(i => Context.Cache.Get<Gear>(i)).Sum(i => i.ItemData.Size) +
                           Cargo.Select(i => Context.Cache.Get<ItemInstance>(i)).Sum(ii => Context.GetSize(ii));
        
        OnInventoryUpdate?.Invoke();
    }

    public void ClearInventoryListeners()
    {
        OnInventoryUpdate = null;
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

    public int GetSwitch<T>() where T : class, IBehavior
    {
        var s = Behaviors
            .FirstOrDefault(x => x.Value.Any(g => g is Switch) && x.Value.Any(g => g is T))
            .Value.First(g => g is Switch) as Switch;
        return Switches.FirstOrDefault(kvp => kvp.Value == s).Key;
    }

    public Switch GetSwitch(IBehavior behavior)
    {
        return Behaviors
            .FirstOrDefault(x => x.Value.Any(g => g == behavior))
            .Value.First(g => g is Switch) as Switch;
    }

    public void AddHeat(float heat)
    {
        if (Parent != Guid.Empty)
        {
            // var parent = Context.Cache.Get<Entity>(Parent);
            // parent.AddHeat(heat);
        }
        else
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
        
        foreach (var behaviors in Behaviors.Values)
            foreach (var behavior in behaviors.Where(b => b is IAlwaysUpdatedBehavior).Cast<IAlwaysUpdatedBehavior>())
                behavior.AlwaysUpdate(delta);

        foreach (var item in EquippedItems
            .Select(item => Context.Cache.Get<Gear>(item))
            .Where(item => item.ItemData.Performance(Temperature) < .01f))
            item.Durability -= delta;

        if (Parent != Guid.Empty)
        {
            var parent = Context.Cache.Get<Entity>(Parent);
            Position = parent.Position;
            Velocity = parent.Velocity;
            Temperature = parent.Temperature;
        }

        // Processing stale item data properly was a pain in my ass, so the shortcut is just persisting and restoring everything
        if (GearData.Any(kvp => kvp.Key.ItemData != kvp.Value))
        {
            OnBeforeSerialize();
            OnAfterDeserialize();
        }

        foreach (var message in Messages.Keys.ToArray())
        {
            Messages[message] = Messages[message] - delta;
            if (Messages[message] < 0)
                Messages.Remove(message);
        }

        foreach (var incompleteGear in IncompleteGear.Keys.ToArray())
        {
            IncompleteGear[incompleteGear] = IncompleteGear[incompleteGear] - delta;
            if (IncompleteGear[incompleteGear] < 0)
            {
                Equip(Context.Cache.Get<Gear>(incompleteGear));
            }
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

    public void SetMessage(string message)
    {
        Messages[message] = Context.GlobalData.MessageDuration;
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
        AddItemBehaviors();
        
        RecalculateMass();

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

[MessagePackObject]
public class IncompleteItem
{
    [Key(0)] public Guid Item;
    [Key(1)] public Guid Blueprint;
    [Key(2)] public double RemainingTime;
}