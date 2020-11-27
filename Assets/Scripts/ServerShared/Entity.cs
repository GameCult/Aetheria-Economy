
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
    public readonly HashSet<Guid> Cargo; // Type: ItemInstance

    [JsonProperty("gear"), Key(7)]
    public readonly List<Guid> Gear;
    
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
    
    [JsonProperty("incompleteCargo"), Key(17)]
    public Dictionary<Guid, double> IncompleteCargo = new Dictionary<Guid, double>();

    [JsonProperty("active"), Key(18)]
    private bool _active;
    public bool Active
    {
        get => _active;
        set
        {
            _active = value;
            if (_active)
            {
                foreach (var hardpoint in Hardpoints)
                    if(hardpoint.Gear!=null)
                        foreach (var behavior in hardpoint.Behaviors)
                        {
                            if(behavior is IInitializableBehavior initializableBehavior)
                                initializableBehavior.Initialize();
                        }
            }
        }
    }
    //public List<IncompleteItem> IncompleteCargo = new List<IncompleteItem>();

    [IgnoreMember] public Zone Zone;
    [IgnoreMember] public List<Hardpoint> Hardpoints;
    [IgnoreMember] public readonly Dictionary<object, float> VisibilitySources = new Dictionary<object, float>();
    [IgnoreMember] public readonly Dictionary<string, float> Messages = new Dictionary<string, float>();
    // [IgnoreMember] public Dictionary<Gear, EquippableItemData> GearData;
    // [IgnoreMember] public Dictionary<Gear, List<IBehavior>> ItemBehaviors;

    private List<IPopulationAssignment> PopulationAssignments = new List<IPopulationAssignment>();

    [IgnoreMember] public int AssignedPopulation => PopulationAssignments.Sum(pa => pa.AssignedPopulation);
    [IgnoreMember] public float Mass { get; private set; }
    [IgnoreMember] public float OccupiedCapacity { get; private set; }
    [IgnoreMember] public float ThermalMass { get; private set; }
    [IgnoreMember] public float Visibility => VisibilitySources.Values.Sum();

    public class ChangeEvent : IChangeSource
    {
        public event Action OnChanged;

        public void Change()
        {
            OnChanged?.Invoke();
        }
    }
    private bool _cargoChanged;
    public ChangeEvent CargoEvent = new ChangeEvent();
    
    private bool _gearChanged;
    public ChangeEvent GearEvent = new ChangeEvent();

    public ChangeEvent ChildEvent = new ChangeEvent();

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


    public Entity(GameContext context, Guid hull, IEnumerable<Guid> gear, IEnumerable<Guid> cargo, Zone zone, Guid corporation)
    {
        Context = context;
        Zone = zone;
        Corporation = corporation;

        Gear = gear.ToList();
        Cargo = new HashSet<Guid>(cargo);
        Hull = hull;

        Hydrate();
    }

    // private void GenerateHardpoints()
    // {
    //     
    // }

    public void Hydrate()
    {
        //var gearIDs = EquippedItems.Append(Hull);
        //var gear = gearIDs.ToDictionary(id => id, id => Context.Cache.Get<Gear>(id));
        //GearData = gear.Values.ToDictionary(g => g, g => g.ItemData);
        
        Hardpoints = new List<Hardpoint>();
        Hardpoints.Add(new Hardpoint
        {
            HardpointData = new HardpointData{ Type = HardpointType.Hull }
        });
        var hull = Context.Cache.Get<Gear>(Hull);
        Equip(hull, Hardpoints[0]);
        
        var hullData = Context.Cache.Get<HullData>(hull.Data);
        Hardpoints.AddRange(hullData.Hardpoints.Select(hd => new Hardpoint {HardpointData = hd}));

        // Create a copy of the gear list and remove every item actually equipped
        var remainingGear = new List<Guid>(Gear);
        foreach (var gearID in Gear)
            if (Equip(gearID))
                remainingGear.Remove(gearID);

        foreach (var remaining in remainingGear)
        {
            Context.Log($"Item {Context.Cache.Get<Gear>(remaining).Name} equipped on entity {Name} has no compatible hardpoint, moved to cargo.");
            Gear.Remove(remaining);
            Cargo.Add(remaining);
        }
        
        if (_active)
        {
            foreach (var hardpoint in Hardpoints)
                if(hardpoint.Gear!=null)
                    foreach (var behavior in hardpoint.Behaviors)
                    {
                        if(behavior is IInitializableBehavior initializableBehavior)
                            initializableBehavior.Initialize();
                    }
        }
        
        RecalculateMass();
    }

    public void HydrateHardpoint(Hardpoint hardpoint)
    {
        if (hardpoint.Gear == null)
        {
            Context.Log("Attempted to hydrate hardpoint with no gear on it!");
            return;
        }

        hardpoint.ItemData = hardpoint.Gear.ItemData;

        hardpoint.Behaviors = hardpoint.Gear.ItemData.Behaviors
            .Select(bd => bd.CreateInstance(Context, this, hardpoint.Gear))
            .ToArray();
        
        hardpoint.BehaviorGroups = hardpoint.Behaviors
            .GroupBy(b => b.Data.Group)
            .OrderBy(g=>g.Key)
            .Select(g=>new BehaviorGroup { 
                    Behaviors = g.ToArray(),
                    //Axis = (IAnalogBehavior) g.FirstOrDefault(b=>b is IAnalogBehavior),
                    Switch = (Switch) g.FirstOrDefault(b=>b is Switch),
                    Trigger = (Trigger) g.FirstOrDefault(b=>b is Trigger)
                })
            .ToArray();

        foreach (var behavior in hardpoint.Behaviors)
        {
            if(behavior is IPopulationAssignment populationAssignment)
                PopulationAssignments.Add(populationAssignment);
        }
    }

    public void Unequip(Hardpoint hardpoint)
    {
        if (hardpoint.Gear != null)
        {
            Gear.Remove(hardpoint.Gear.ID);
            Cargo.Add(hardpoint.Gear.ID);
            hardpoint.Gear = null;
            _cargoChanged = true;
            _gearChanged = true;
        }
    }

    public void Equip(Gear gear, Hardpoint hardpoint)
    {
        // Hardpoint is occupied, remove existing item from gear list
        if (hardpoint.Gear != null) Unequip(hardpoint);
        if (Cargo.Contains(gear.ID)) Cargo.Remove(gear.ID);

        hardpoint.Gear = gear;
        HydrateHardpoint(hardpoint);
        _gearChanged = true;
    }

    public bool Equip(Guid gearID, bool force = false)
    {
        // Check all portions of inventory for a matching item
        if (Cargo.Contains(gearID)) Cargo.Remove(gearID);
        else if (IncompleteGear.ContainsKey(gearID))
            IncompleteGear.Remove(gearID);
        else if(!Gear.Contains(gearID))
            throw new ArgumentException("Attempted to equip gear which is not present in inventory!");
        
        var gear = Context.Cache.Get<Gear>(gearID);
        var gearData = Context.Cache.Get<GearData>(gear.Data);
        
        // Find an empty hardpoint of the correct type
        var hardpoint = Hardpoints
            .FirstOrDefault(hp => hp.Gear == null && hp.HardpointData.Type == gearData.HardpointType);

        // With force option enabled, we'll try to find an empty hardpoint but otherwise any will do
        if (hardpoint == null && force)
            hardpoint = Hardpoints
                .FirstOrDefault(hp => hp.HardpointData.Type == gearData.HardpointType);

        // No compatible hardpoint found, return false
        if (hardpoint == null)
            return false;

        Equip(gear, hardpoint);
        return true;
    }

    public CraftedItemInstance AddCargo(CraftedItemInstance item)
    {
        Cargo.Add(item.ID);
        Mass += item.Mass;
        ThermalMass += item.ThermalMass;
        OccupiedCapacity += item.Size;
        _cargoChanged = true;
        return item;
    }

    public CraftedItemInstance RemoveCargo(CraftedItemInstance item)
    {
        if(!Cargo.Contains(item.ID))
            throw new ArgumentException("Attempted to remove an item which is not in cargo!");
        Cargo.Remove(item.ID);
        Mass -= item.Mass;
        ThermalMass -= item.ThermalMass;
        OccupiedCapacity -= item.Size;
        _cargoChanged = true;
        return item;
    }

    public SimpleCommodity AddCargo(SimpleCommodity item)
    {
        // Regardless of whether a match is found, the item's mass and size are added to the entity
        Mass += item.Mass;
        ThermalMass += item.ThermalMass;
        OccupiedCapacity += item.Size;
        
        // Try to find an matching instance of the same commodity in cargo
        var existingCommodityID = Cargo.FirstOrDefault(id =>
            Context.Cache.Get<ItemInstance>(id) is SimpleCommodity simpleCommodity &&
            simpleCommodity.ItemData == item.ItemData);
        
        // A match was found, delete the original item and increment the quantity of the match
        if (existingCommodityID != Guid.Empty)
        {
            var existingCommodity = Context.Cache.Get<SimpleCommodity>(existingCommodityID);
            existingCommodity.Quantity += item.Quantity;
            Context.Cache.Delete(item);
            _cargoChanged = true;
            return existingCommodity;
        }
        
        // No match was found, simply add the item instance to the cargo
        Cargo.Add(item.ID);
        _cargoChanged = true;
        return item;
    }

    public SimpleCommodity RemoveCargo(SimpleCommodity item, int quantity)
    {
        if(!Cargo.Contains(item.ID))
            throw new ArgumentException("Attempted to remove an item which is not in cargo!");
        
        // If the quantity matches exactly, huzzah, just remove the instance from cargo
        if (item.Quantity == quantity)
            Cargo.Remove(item.ID);
        else
        {
            // Quantity does not match, create new item instance and decrement source quantity
            var newSimpleCommodity = new SimpleCommodity
            {
                Context = Context,
                Data = item.Data,
                Quantity = quantity
            };
            Context.Cache.Add(newSimpleCommodity);
            item.Quantity -= quantity;
            item = newSimpleCommodity;
        }
        
        Mass -= item.Mass;
        ThermalMass -= item.ThermalMass;
        OccupiedCapacity -= item.Size;

        _cargoChanged = true;
        return item;
    }

    public void AddChild(Entity entity)
    {
        Mass += entity.Mass;
        ThermalMass += entity.ThermalMass;
        Children.Add(entity.ID);
        ChildEvent.Change();
    }

    public void RemoveChild(Entity entity)
    {
        Mass -= entity.Mass;
        ThermalMass -= entity.ThermalMass;
        Children.Remove(entity.ID);
        ChildEvent.Change();
    }

    public Guid Build(BlueprintData blueprint, float quality, string name, bool direct = false)
    {
        if (direct)
        {
            if(blueprint.FactoryItem!=Guid.Empty)
                throw new ArgumentException("Attempted to directly build a blueprint which requires a factory!");
            var blueprintItem = Context.Cache.Get<GearData>(blueprint.Item);
            if(blueprintItem == null)
                throw new ArgumentException("Attempted to directly build a blueprint for a missing or incompatible item!");
        }

        //var newItemID = Guid.Empty;

        // GetBlueprintIngredients will assign the output lists with the items matching the blueprint, and return false if they are not found
        if (!GetBlueprintIngredients(blueprint, out var simpleIngredients, out var compoundIngredients))
            return Guid.Empty;
        
        // These will be bundled with the data for the compound commodity instance
        var ingredients = new List<ItemInstance>();
        ingredients.AddRange(compoundIngredients.Select(RemoveCargo));
        ingredients.AddRange(simpleIngredients.Select(sc => RemoveCargo(sc, blueprint.Ingredients[sc.Data])));
            
        var blueprintItemData = Context.Cache.Get(blueprint.Item);
        
        // Creating new crafted item instances is a bit more complicated
        if (blueprintItemData is CraftedItemData)
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
                    Name = name,
                    SourceEntity = ID
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
                    Name = name,
                    SourceEntity = ID
                };
            }
            Context.Cache.Add(newItem);
            if (direct)
                IncompleteGear[newItem.ID] = blueprint.ProductionTime;
            else 
                IncompleteCargo[newItem.ID] = blueprint.ProductionTime;
            return newItem.ID;
        }

        var newSimpleCommodity = new SimpleCommodity
        {
            Context = Context,
            Data = blueprintItemData.ID,
            Quantity = 1
        };
        Context.Cache.Add(newSimpleCommodity);
        IncompleteCargo[newSimpleCommodity.ID] = blueprint.ProductionTime;
        return newSimpleCommodity.ID;
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
            Gear.Select(i => Context.Cache.Get<Gear>(i)).Sum(i => i.Mass) + 
            IncompleteGear.Select(i => Context.Cache.Get<Gear>(i.Key)).Sum(i => i.Mass) + 
            Cargo.Select(i => Context.Cache.Get<ItemInstance>(i)).Sum(ii => ii.Mass) + 
            IncompleteCargo.Select(i => Context.Cache.Get<ItemInstance>(i.Key)).Sum(ii => ii.Mass) + 
            Children.Select(i => Context.Cache.Get<Entity>(i)).Sum(c=>c.Mass);
        
        ThermalMass = Context.Cache.Get<Gear>(Hull).ThermalMass + 
                       Gear.Select(i => Context.Cache.Get<Gear>(i)).Sum(i => i.ThermalMass) +
                       IncompleteGear.Select(i => Context.Cache.Get<Gear>(i.Key)).Sum(i => i.ThermalMass) + 
                       Cargo.Select(i => Context.Cache.Get<ItemInstance>(i)).Sum(ii => ii.ThermalMass) + 
                       IncompleteCargo.Select(i => Context.Cache.Get<ItemInstance>(i.Key)).Sum(ii => ii.ThermalMass) + 
                       Children.Select(i => Context.Cache.Get<Entity>(i)).Sum(c=>c.ThermalMass);

        OccupiedCapacity = Gear.Select(i => Context.Cache.Get<Gear>(i)).Sum(i => i.Size) +
                           Cargo.Select(i => Context.Cache.Get<ItemInstance>(i)).Sum(ii => ii.Size);
    }

    public T GetBehavior<T>() where T : class, IBehavior
    {
        foreach (var hardpoint in Hardpoints)
            if(hardpoint.Gear!=null)
                foreach (var behavior in hardpoint.Behaviors)
                    if (behavior is T b)
                        return b;
        return null;
    }

    public IEnumerable<T> GetBehaviors<T>() where T : class, IBehavior
    {
        foreach (var hardpoint in Hardpoints)
            if(hardpoint.Gear!=null)
                foreach (var behavior in hardpoint.Behaviors)
                    if (behavior is T b)
                        yield return b;
    }

    public IEnumerable<T> GetBehaviorData<T>() where T : BehaviorData
    {
        foreach (var hardpoint in Hardpoints)
            if(hardpoint.Gear!=null)
                foreach (var behavior in hardpoint.Behaviors)
                    if (behavior.Data is T b)
                        yield return b;
    }

    public Switch GetSwitch<T>() where T : class, IBehavior
    {
        foreach (var hardpoint in Hardpoints)
            if(hardpoint.Gear!=null)
                foreach (var group in hardpoint.BehaviorGroups)
                    foreach(var behavior in group.Behaviors)
                        if (behavior is T)
                            return group.Switch;
        return null;
    }

    public Trigger GetTrigger<T>() where T : class, IBehavior
    {
        foreach (var hardpoint in Hardpoints)
            if(hardpoint.Gear!=null)
                foreach (var group in hardpoint.BehaviorGroups)
                    foreach(var behavior in group.Behaviors)
                        if (behavior is T)
                            return group.Trigger;
        return null;
    }

    public void AddHeat(float heat)
    {
        if (Parent != Guid.Empty)
        {
            // var parent = Context.Cache.Get<Entity>(Parent);
            // parent.AddHeat(heat);
        }
        else
            Temperature += heat / ThermalMass;

    }

    public virtual void Update(float delta)
    {
        if (Active)
        {
            foreach (var hardpoint in Hardpoints.Where(hp=>hp.Gear!=null))
            {
                foreach (var group in hardpoint.BehaviorGroups)
                foreach(var behavior in group.Behaviors)
                    if(!behavior.Update(delta))
                        break;
            
                foreach (var behavior in hardpoint.Behaviors.Where(b => b is IAlwaysUpdatedBehavior).Cast<IAlwaysUpdatedBehavior>())
                    behavior.AlwaysUpdate(delta);
            
                if(hardpoint.Gear.ItemData.Performance(Temperature) < .01f)
                    hardpoint.Gear.Durability -= delta;
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
                if (IncompleteGear[incompleteGear] < 0) Equip(incompleteGear);
            }
        }

        if (Parent != Guid.Empty)
        {
            var parent = Context.Cache.Get<Entity>(Parent);
            Position = parent.Position;
            Velocity = parent.Velocity;
            Temperature = parent.Temperature;
        }

        if (_cargoChanged)
        {
            CargoEvent.Change();
            _cargoChanged = false;
        }
        if (_gearChanged)
        {
            GearEvent.Change();
            _gearChanged = false;
        }
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
        PersistedBehaviors = Hardpoints
            .Where(hp => hp.Behaviors.Any(b=>b is IPersistentBehavior))
            .Select(hp => new {gear=hp.Gear, behaviors = hp.Behaviors
                .Where(b=>b is IPersistentBehavior)
                .Cast<IPersistentBehavior>()})
            .ToDictionary(x=> x.gear.ID, x=>x.behaviors.Select(b => b.Store()).ToArray());
    }

    public void OnAfterDeserialize()
    {
        Hydrate();

        // Iterate only over the behaviors of items which contain persistent data
        // Filter the behaviors for each item to get the persistent ones, then cast them and combine with the persisted data array for that item
        foreach (var persistentBehaviorData in Hardpoints
            .Where(hardpoint => PersistedBehaviors.ContainsKey(hardpoint.Gear.ID)).
            SelectMany(hardpoint => hardpoint.Behaviors
                .Where(b=> b is IPersistentBehavior)
                .Cast<IPersistentBehavior>()
                .Zip(PersistedBehaviors[hardpoint.Gear.ID], (behavior, data) => new{behavior, data})))
            persistentBehaviorData.behavior.Restore(persistentBehaviorData.data);
    }
}

//[MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class Hardpoint
{
    public Gear Gear;
    public EquippableItemData ItemData;
    public HardpointData HardpointData;
    public IBehavior[] Behaviors;
    public BehaviorGroup[] BehaviorGroups;
}

public class BehaviorGroup
{
    public IBehavior[] Behaviors;
    public Trigger Trigger;
    public Switch Switch;
    //public IAnalogBehavior Axis;
}