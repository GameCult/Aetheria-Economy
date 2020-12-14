using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JsonKnownTypes;
using MessagePack;
using Newtonsoft.Json;
using UniRx;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using int2 = Unity.Mathematics.int2;

[MessagePackObject, 
 Union(0, typeof(Ship)),
 Union(1, typeof(OrbitalEntity)),
 JsonObject(MemberSerialization.OptIn), JsonConverter(typeof(JsonKnownTypesConverter<Entity>))]
public abstract class Entity
{
    public Zone Zone;
    public EquippableItem Hull;
    
    public float2 Position;
    public float2 Direction = float2(0,1);
    public float2 Velocity;
    
    public float[,] Temperature;
    public float Energy;

    public readonly ReactiveCollection<EquippedItem> EquippedItems = new ReactiveCollection<EquippedItem>();
    public readonly ReactiveCollection<EquippedCargoBay> EquippedCargoBays = new ReactiveCollection<EquippedCargoBay>();

    public Entity Parent;
    public List<Entity> Children = new List<Entity>();
    
    public string Name;
    
    public int Population;
    public Dictionary<Guid, float> Personality = new Dictionary<Guid, float>();
    
    public readonly Dictionary<string, float> Messages = new Dictionary<string, float>();
    public readonly Dictionary<object, float> VisibilitySources = new Dictionary<object, float>();
    
    private List<IPopulationAssignment> PopulationAssignments = new List<IPopulationAssignment>();


    private bool _active;
    public EquippedItem[,] GearOccupancy;
    public EquippedItem[,] ThermalOccupancy;
    public HardpointData[,] Hardpoints;
    
    public ItemManager ItemManager { get; }
    public int AssignedPopulation => PopulationAssignments.Sum(pa => pa.AssignedPopulation);
    public float Mass { get; private set; }
    public float Visibility => VisibilitySources.Values.Sum();
    
    public bool Active
    {
        get => _active;
        set
        {
            _active = value;
            if (_active)
            {
                foreach (var hardpoint in EquippedItems)
                    if(hardpoint.EquippableItem!=null)
                        foreach (var behavior in hardpoint.Behaviors)
                        {
                            if(behavior is IInitializableBehavior initializableBehavior)
                                initializableBehavior.Initialize();
                        }
            }
        }
    }

    public Entity(ItemManager itemManager, Zone zone, EquippableItem hull)
    {
        ItemManager = itemManager;
        Zone = zone;
        Hull = hull;
        MapShip();
    }

    private void MapShip()
    {
        var hullData = ItemManager.GetData(Hull) as HullData;
        GearOccupancy = new EquippedItem[hullData.Shape.Width, hullData.Shape.Height];
        ThermalOccupancy = new EquippedItem[hullData.Shape.Width, hullData.Shape.Height];
        Hardpoints = new HardpointData[hullData.Shape.Width, hullData.Shape.Height];
        foreach (var hardpoint in hullData.Hardpoints)
        {
            foreach (var hardpointCoord in hardpoint.Shape.Coordinates)
            {
                var hullCoord = hardpoint.Position + hardpointCoord;
                Hardpoints[hullCoord.x, hullCoord.y] = hardpoint;
            }
        }
    }

    public void Hydrate(EquippedItem equippedItem)
    {
        if (equippedItem.EquippableItem == null)
        {
            ItemManager.Log("Attempted to hydrate EquippedGear with no gear on it! This should be impossible!");
            return;
        }

        var gearData = ItemManager.GetData(equippedItem.EquippableItem);

        equippedItem.Behaviors = gearData.Behaviors
            .Select(bd => bd.CreateInstance(ItemManager, this, equippedItem))
            .ToArray();
        
        equippedItem.BehaviorGroups = equippedItem.Behaviors
            .GroupBy(b => b.Data.Group)
            .OrderBy(g=>g.Key)
            .Select(g=>new BehaviorGroup { 
                    Behaviors = g.ToArray(),
                    //Axis = (IAnalogBehavior) g.FirstOrDefault(b=>b is IAnalogBehavior),
                    Switch = (Switch) g.FirstOrDefault(b=>b is Switch),
                    Trigger = (Trigger) g.FirstOrDefault(b=>b is Trigger)
                })
            .ToArray();

        foreach (var behavior in equippedItem.Behaviors)
        {
            if(behavior is IPopulationAssignment populationAssignment)
                PopulationAssignments.Add(populationAssignment);
        }
    }

    public EquippedCargoBay FindItemInCargo(Guid itemDataID)
    {
        return EquippedCargoBays.FirstOrDefault(c => c.ItemsOfType.ContainsKey(itemDataID));
    }

    // Attempts to move a given number of items of the given type to the target Entity
    // Returns the number of items successfully transferred
    public int TryTransferItems(Entity target, Guid itemDataID, int quantity)
    {
        int quantityTransferred = 0;
        while (quantityTransferred < quantity)
        {
            EquippedCargoBay originInventory = EquippedCargoBays.FirstOrDefault(c => c.ItemsOfType.ContainsKey(itemDataID));

            if (originInventory == null) break;

            var itemInstance = originInventory.ItemsOfType[itemDataID][0];

            if (itemInstance is SimpleCommodity simpleCommodity)
            {
                var targetQuantity = min(simpleCommodity.Quantity, quantity - quantityTransferred);
                if (!target.EquippedCargoBays.Any(c => originInventory.TryTransferItem(c, simpleCommodity, targetQuantity)))
                {
                    quantityTransferred += targetQuantity - simpleCommodity.Quantity;
                    break;
                }

                quantityTransferred += targetQuantity;
            }
            else if (itemInstance is CraftedItemInstance craftedItemInstance)
            {
                if (!target.EquippedCargoBays.Any(c => originInventory.TryTransferItem(c, craftedItemInstance)))
                    break;
                
                quantityTransferred++;
            }
        }

        return quantityTransferred;
    }

    public EquippableItem Unequip(EquippedItem item)
    {
        if (item.EquippableItem == null)
        {
            ItemManager.Log("Attempted to remove equipped item with no equippable item on it! This should be impossible!");
            return null;
        }

        if (item is EquippedCargoBay cargoBay)
        {
            if(cargoBay.Cargo.Count > 0)
            {
                ItemManager.Log("Attempted to remove cargo bay that is not empty! Please check first before doing this!");
                return null;
            }

            EquippedCargoBays.Remove(cargoBay);
        }
        
        EquippedItems.Remove(item);
        
        var itemData = ItemManager.GetData(item.EquippableItem);
        foreach (var i in itemData.Shape.Coordinates)
        {
            var itemCoord = item.Position + itemData.Shape.Rotate(i, item.EquippableItem.Rotation);
            (itemData.HardpointType == HardpointType.Thermal ? ThermalOccupancy : GearOccupancy)[itemCoord.x, itemCoord.y] = null;
        }

        return item.EquippableItem;
    }

    // Check whether the given item will fit when its origin is placed at the given coordinate
    private bool ItemFits(EquippableItemData itemData, HullData hullData, EquippableItem item, int2 hullCoord)
    {
        // Items without specific hardpoints on the ship can be freely rotated and placed anywhere
        if (itemData.HardpointType == HardpointType.Tool || itemData.HardpointType == HardpointType.Thermal)
        {
            // Check every cell of the item's shape
            foreach (var i in itemData.Shape.Coordinates)
            {
                // If there is any gear already occupying that space, it won't fit
                // Thermal items have their own layer and do not collide with gear
                var itemCoord = hullCoord + itemData.Shape.Rotate(i, item.Rotation);
                if (!hullData.InteriorCells[itemCoord] || (itemData.HardpointType == HardpointType.Thermal ? ThermalOccupancy : GearOccupancy)[itemCoord.x, itemCoord.y] != null) return false;
            }
        }
        else
        {
            var hardpoint = Hardpoints[hullCoord.x, hullCoord.y];
            
            // If there's no hardpoint there, it won't fit
            if (hardpoint == null) return false;

            // If the hardpoint type doesn't match the item, it won't fit
            if (hardpoint.Type != itemData.HardpointType) return false;
            
            // Check every cell of the item's shape
            foreach (var i in itemData.Shape.Coordinates)
            {
                // If the hardpoint does not have a matching cell, it wont fit
                if (!hardpoint.Shape[i]) return false;
            
                // If there is any gear already occupying that space, it won't fit
                // Items placed in hardpoints are automatically aligned to hardpoint rotation
                var itemCoord = hardpoint.Position + itemData.Shape.Rotate(i, hardpoint.Rotation);
                if (GearOccupancy[itemCoord.x, itemCoord.y] != null) return false;
            }
        }

        return true;
    }

    // Check whether the given item will fit when its origin is placed at the given coordinate on the hull
    public bool ItemFits(EquippableItem item, int2 hullCoord)
    {
        var itemData = ItemManager.GetData(item);
        var hullData = ItemManager.GetData(Hull) as HullData;
        return ItemFits(itemData, hullData, item, hullCoord);
    }

    public bool TryFindSpace(EquippableItem item, out int2 hullCoord)
    {
        var itemData = ItemManager.GetData(item);
        var hullData = ItemManager.GetData(Hull) as HullData;
        
        // Tools and thermal equipment can be installed anywhere on the ship
        // Search the whole ship for somewhere the item will fit
        if (itemData.HardpointType == HardpointType.Tool || itemData.HardpointType == HardpointType.Thermal)
        {
            foreach (var hullCoord2 in hullData.InteriorCells.Coordinates)
            {
                if (ItemFits(itemData, hullData, item, hullCoord2))
                {
                    hullCoord = hullCoord2;
                    return true;
                }
            }
        }
        
        // Everything else has to be equipped onto a hardpoint of the same type
        // Search the ship for an empty hardpoint that matches the type and shape of the item
        else
        {
            foreach (var hardpoint in hullData.Hardpoints)
            {
                if(hardpoint.Type == itemData.HardpointType)
                {
                    foreach (var hardpointCoord in hardpoint.Shape.Coordinates)
                    {
                        var hullCoord2 = hardpoint.Position + hardpointCoord;
                        if (ItemFits(itemData, hullData, item, hullCoord2))
                        {
                            hullCoord = hullCoord2;
                            return true;
                        }
                    }
                }
            }
        }
        
        hullCoord = int2.zero;
        return false;
    }

    // Try to equip the given item anywhere it will fit, returns true when the item was successfully equipped
    public bool TryEquip(EquippableItem item) => TryFindSpace(item, out var hullCoord) && TryEquip(item, hullCoord);

    // Try to equip the given item to the given location
    public bool TryEquip(EquippableItem item, int2 hullCoord)
    {
        var itemData = ItemManager.GetData(item);
        var hullData = ItemManager.GetData(Hull) as HullData;

        if (!ItemFits(itemData, hullData, item, hullCoord)) return false;
        
        if (itemData.HardpointType == HardpointType.Tool || itemData.HardpointType == HardpointType.Thermal)
        {
            EquippedItem equippedItem;
            if(itemData is CargoBayData)
            {
                equippedItem = new EquippedCargoBay(ItemManager, item, hullCoord);
                EquippedCargoBays.Add((EquippedCargoBay) equippedItem);
            }
            else
            {
                equippedItem = new EquippedItem(ItemManager, item, hullCoord);
                EquippedItems.Add(equippedItem);
            }

            foreach (var i in itemData.Shape.Coordinates)
            {
                var occupiedCoord = hullCoord + itemData.Shape.Rotate(i, item.Rotation);
                (itemData.HardpointType == HardpointType.Thermal ? ThermalOccupancy : GearOccupancy)[occupiedCoord.x, occupiedCoord.y] = equippedItem;
            }
                
            Hydrate(equippedItem);
            return true;
        }
        else
        {
            var equippedItem = new EquippedItem(ItemManager, item, hullCoord);
            EquippedItems.Add(equippedItem);
            
            foreach (var i in itemData.Shape.Coordinates)
            {
                var occupiedCoord = hullCoord + i;
                (itemData.HardpointType == HardpointType.Thermal ? ThermalOccupancy : GearOccupancy)[occupiedCoord.x, occupiedCoord.y] = equippedItem;
            }
                
            Hydrate(equippedItem);
            return true;
        }

    }

    public void AddChild(Entity entity)
    {
        Mass += entity.Mass;
        Children.Add(entity);
    }

    public void RemoveChild(Entity entity)
    {
        Mass -= entity.Mass;
        Children.Remove(entity);
    }
    
    public void SetParent(Entity parent)
    {
        Parent = parent;
        parent.AddChild(this);
    }

    public void RemoveParent()
    {
        if (Parent == null)
            return;

        Parent.RemoveChild(this);
        Parent = null;
    }

    public T GetBehavior<T>() where T : class, IBehavior
    {
        foreach (var equippedItem in EquippedItems)
            foreach (var behavior in equippedItem.Behaviors)
                if (behavior is T b)
                    return b;
        return null;
    }

    public IEnumerable<T> GetBehaviors<T>() where T : class, IBehavior
    {
        foreach (var equippedItem in EquippedItems)
            foreach (var behavior in equippedItem.Behaviors)
                if (behavior is T b)
                    yield return b;
    }

    public IEnumerable<T> GetBehaviorData<T>() where T : BehaviorData
    {
        foreach (var equippedItem in EquippedItems)
            foreach (var behavior in equippedItem.Behaviors)
                if (behavior.Data is T b)
                    yield return b;
    }

    public Switch GetSwitch<T>() where T : class, IBehavior
    {
        foreach (var equippedItem in EquippedItems)
            foreach (var group in equippedItem.BehaviorGroups)
                foreach(var behavior in group.Behaviors)
                    if (behavior is T)
                        return group.Switch;
        return null;
    }

    public Trigger GetTrigger<T>() where T : class, IBehavior
    {
        foreach (var equippedItem in EquippedItems)
            foreach (var group in equippedItem.BehaviorGroups)
                foreach(var behavior in group.Behaviors)
                    if (behavior is T)
                        return group.Trigger;
        return null;
    }

    public virtual void Update(float delta)
    {
        if (Active)
        {
            foreach (var equippedItem in EquippedItems)
            {
                equippedItem.Update(delta);
            }

            foreach (var message in Messages.Keys.ToArray())
            {
                Messages[message] = Messages[message] - delta;
                if (Messages[message] < 0)
                    Messages.Remove(message);
            }
        }

        if (Parent != null)
        {
            Position = Parent.Position;
            Velocity = Parent.Velocity;
        }
    }

    public void SetMessage(string message)
    {
        Messages[message] = ItemManager.GameplaySettings.MessageDuration;
    }

    // public void OnBeforeSerialize()
    // {
    //     // Filter item behavior collections by those with any persistent behaviors
    //     // For each item create an object containing the item ID and a list of persistent behaviors
    //     // Then turn that into a dictionary mapping from item ID to an array of every behaviors persistent data
    //     PersistedBehaviors = EquippedItems
    //         .Where(item => item.Behaviors.Any(b=>b is IPersistentBehavior))
    //         .Select(item => new {equippable=item, behaviors = item.Behaviors
    //             .Where(b=>b is IPersistentBehavior)
    //             .Cast<IPersistentBehavior>()})
    //         .ToDictionary(x=> x.equippable.Position, x=>x.behaviors.Select(b => b.Store()).ToArray());
    // }
    //
    // public void OnAfterDeserialize()
    // {
    //     foreach(var item in EquippedItems)
    //         Hydrate(item);
    //
    //     // Iterate only over the behaviors of items which contain persistent data
    //     // Filter the behaviors for each item to get the persistent ones, then cast them and combine with the persisted data array for that item
    //     foreach (var persistentBehaviorData in EquippedItems
    //         .Where(item => PersistedBehaviors.ContainsKey(item.Position))
    //         .SelectMany(item => item.Behaviors
    //             .Where(b=> b is IPersistentBehavior)
    //             .Cast<IPersistentBehavior>()
    //             .Zip(PersistedBehaviors[item.Position], (behavior, data) => new{behavior, data})))
    //         persistentBehaviorData.behavior.Restore(persistentBehaviorData.data);
    // }
}

[MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class EquippedItem
{
    [JsonProperty("item"), Key(0)] public EquippableItem EquippableItem;

    [JsonProperty("position"), Key(1)] public int2 Position;

    [JsonProperty("temperature"), Key(2)] public float Temperature;
    
    [IgnoreMember]
    public IBehavior[] Behaviors;
    
    [IgnoreMember]
    public BehaviorGroup[] BehaviorGroups;
    
    protected readonly ItemManager _itemManager;
    private readonly EquippableItemData _data;

    public EquippedItem(ItemManager itemManager, EquippableItem item, int2 position)
    {
        _itemManager = itemManager;
        _data = _itemManager.GetData(item);
        EquippableItem = item;
        Position = position;
    }

    public void Update(float delta)
    {
        if(Temperature < _data.MinimumTemperature || Temperature > _data.MaximumTemperature)
        {
            EquippableItem.Durability -= delta;
            return;
        }
        
        foreach (var group in BehaviorGroups)
        {
            foreach (var behavior in group.Behaviors)
            {
                if (!behavior.Update(delta))
                    break;
            }
        }
            
        foreach (var behavior in Behaviors.Where(b => b is IAlwaysUpdatedBehavior).Cast<IAlwaysUpdatedBehavior>())
            behavior.AlwaysUpdate(delta);
    }
}

public class EquippedCargoBay : EquippedItem
{
    public readonly ReactiveDictionary<ItemInstance, int2> Cargo = new ReactiveDictionary<ItemInstance, int2>();

    public readonly ItemInstance[,] Occupancy;

    public readonly Dictionary<Guid, List<ItemInstance>> ItemsOfType = new Dictionary<Guid, List<ItemInstance>>();

    public readonly CargoBayData Data;
    
    public float Mass { get; private set; }
    public float ThermalMass { get; private set; }
    
    public EquippedCargoBay(ItemManager itemManager, EquippableItem item, int2 position) : base(itemManager, item, position)
    {
        Data = _itemManager.GetData(EquippableItem) as CargoBayData;

        Mass = Data.Mass;
        ThermalMass = Data.Mass * Data.SpecificHeat;
        
        Occupancy = new ItemInstance[Data.InteriorShape.Width,Data.InteriorShape.Height];
    }

    // Check whether the given item will fit when its origin is placed at the given coordinate
    private bool ItemFits(ItemInstance item, int2 cargoCoord)
    {
        var itemData = _itemManager.GetData(item);
        // Check every cell of the item's shape
        foreach (var i in itemData.Shape.Coordinates)
        {
            // If there is an item already occupying that space, it won't fit
            var itemCargoCoord = cargoCoord + itemData.Shape.Rotate(i, item.Rotation);
            if (Occupancy[itemCargoCoord.x, itemCargoCoord.y] != null) return false;
        }

        return true;
    }

    // Tries to find a place to put the given items in the inventory
    // Will attempt to fill existing item stacks first
    // Returns true only when ALL of the items have places to go
    public bool TryFindSpace(SimpleCommodity item, out List<int2> positions)
    {
        positions = new List<int2>();
        var itemData = _itemManager.GetData(item);
        var remainingQuantity = item.Quantity;
        
        // For simple commodities, search for existing item stacks to add to
        foreach (var cargoItem in Cargo.Keys)
        {
            if (item.Data != cargoItem.Data) continue;
            
            var cargoCommodity = (SimpleCommodity) cargoItem;
            if (cargoCommodity.Quantity >= itemData.MaxStack) continue;
            
            // Subtract remaining space in existing stack from remaining quantity
            remainingQuantity -= min(itemData.MaxStack - cargoCommodity.Quantity, remainingQuantity);
            positions.Add(Cargo[cargoItem]);
            
            // If we've moved all of the items into existing stacks, no need to search for empty space!
            if (remainingQuantity == 0) return true;
        }
        
        // Search all the space in the cargo bay for an empty space where the item fits
        foreach (var cargoCoord in Data.InteriorShape.Coordinates)
        {
            if (ItemFits(item, cargoCoord))
            {
                positions.Add(cargoCoord);
                return true;
            }
        }

        return false;
    }

    // Attempts to store all of the given item anywhere in the inventory
    // Will attempt to fill existing item stacks first
    // Returns true only when ALL of the items are successfully stored
    public bool TryStore(SimpleCommodity item)
    {
        TryFindSpace(item, out var positions);
        foreach (var position in positions)
        {
            if (TryStore(item, position)) return true;
        }

        return false;
    }

    // Try to store the given commodity at the given position
    // If there's a stack at the given position it will be added to
    // Returns true only when ALL of the items are successfully stored
    public bool TryStore(SimpleCommodity item, int2 cargoCoord)
    {
        var itemData = _itemManager.GetData(item);
        if (ItemFits(item, cargoCoord))
        {
            foreach (var p in itemData.Shape.Coordinates)
            {
                var pos = cargoCoord + itemData.Shape.Rotate(p, item.Rotation);
                Occupancy[pos.x, pos.y] = item;
            }
            Cargo[item] = cargoCoord;
            
            if(!ItemsOfType.ContainsKey(item.Data))
                ItemsOfType[item.Data] = new List<ItemInstance>();
            ItemsOfType[item.Data].Add(item);
        }
        else if (Occupancy[cargoCoord.x, cargoCoord.y] is SimpleCommodity cargoCommodity && cargoCommodity.Data == item.Data)
        {
            if (cargoCommodity.Quantity + item.Quantity <= itemData.MaxStack)
            {
                cargoCommodity.Quantity += item.Quantity;
            }
            else
            {
                var quantityTransferred = itemData.MaxStack - cargoCommodity.Quantity;
                item.Quantity -= quantityTransferred;
                cargoCommodity.Quantity = itemData.MaxStack;
                
                Mass += itemData.Mass * quantityTransferred;
                ThermalMass += itemData.Mass * itemData.SpecificHeat * quantityTransferred;
                return false;
            }
        }
        else return false;
        
        Mass += _itemManager.GetMass(item);
        ThermalMass += _itemManager.GetThermalMass(item);
        return true;
    }

    // Searches the cargo bay for a position where the item will fit, returns true when found
    public bool TryFindSpace(CraftedItemInstance item, out int2 position)
    {
        // Search all the space in the cargo bay for an empty space where the item fits
        foreach (var cargoCoord in Data.InteriorShape.Coordinates)
        {
            if (ItemFits(item, cargoCoord))
            {
                position = cargoCoord;
                return true;
            }
        }

        position = int2.zero;
        return false;
    }

    // Try to store the given item anywhere it will fit, returns true when the item was successfully stored
    public bool TryStore(CraftedItemInstance item) => TryFindSpace(item, out var position) && TryStore(item, position);

    // Try to store the given item at the given position, returns true when the item was successfully stored
    public bool TryStore(CraftedItemInstance item, int2 cargoCoord)
    {
        if (!ItemFits(item, cargoCoord)) return false;
        
        var itemData = _itemManager.GetData(item);
        foreach (var p in itemData.Shape.Coordinates)
        {
            var pos = cargoCoord + itemData.Shape.Rotate(p, item.Rotation);
            Occupancy[pos.x, pos.y] = item;
        }
        Cargo[item] = cargoCoord;
        
        if(!ItemsOfType.ContainsKey(item.Data))
            ItemsOfType[item.Data] = new List<ItemInstance>();
        ItemsOfType[item.Data].Add(item);
        
        Mass += _itemManager.GetMass(item);
        ThermalMass += _itemManager.GetThermalMass(item);

        return true;
    }

    public SimpleCommodity Remove(SimpleCommodity item, int quantity)
    {
        if (!Cargo.ContainsKey(item))
        {
            _itemManager.Log("Attempted to remove item from a cargo bay that it wasn't even in! Something went wrong here!");
            return null;
        }
        var itemData = _itemManager.GetData(item);
        if(quantity >= item.Quantity)
        {
            foreach (var pos in itemData.Shape.Coordinates.Select(p => Cargo[item] + itemData.Shape.Rotate(p, item.Rotation)))
                Occupancy[pos.x, pos.y] = null;

            Cargo.Remove(item);
            ItemsOfType[item.Data].Remove(item);
            if (!ItemsOfType[item.Data].Any())
                ItemsOfType.Remove(item.Data);

            Mass -= _itemManager.GetMass(item);
            ThermalMass -= _itemManager.GetThermalMass(item);

            return item;
        }

        item.Quantity -= quantity;
        Mass -= itemData.Mass * quantity;
        ThermalMass -= itemData.Mass * itemData.SpecificHeat * quantity;
        return new SimpleCommodity{Data = item.Data, Quantity = quantity, Rotation = item.Rotation};
    }

    public void Remove(CraftedItemInstance item)
    {
        if (!Cargo.ContainsKey(item))
        {
            _itemManager.Log("Attempted to remove item from a cargo bay that it wasn't even in! Something went wrong here!");
            return;
        }
        var itemData = _itemManager.GetData(item);
        foreach (var pos in itemData.Shape.Coordinates.Select(p => Cargo[item] + itemData.Shape.Rotate(p, item.Rotation)))
            Occupancy[pos.x, pos.y] = null;
        
        Cargo.Remove(item);
        ItemsOfType[item.Data].Remove(item);
        if (!ItemsOfType[item.Data].Any())
            ItemsOfType.Remove(item.Data);
        
        Mass -= _itemManager.GetMass(item);
        ThermalMass -= _itemManager.GetThermalMass(item);
    }
    
    public bool TryTransferItem(EquippedCargoBay target, SimpleCommodity item, int quantity)
    {
        if (!Cargo.ContainsKey(item))
        {
            _itemManager.Log("Attempted to remove item from a cargo bay that it wasn't even in! Something went wrong here!");
            return false;
        }

        var oldPos = Cargo[item];
        var newItem = Remove(item, quantity);
        
        if (target.TryStore(item)) return true;
        
        // Failed to transfer full quantity, move the remaining items back to their old slot
        TryStore(newItem, oldPos);
        return false;
    }
    
    public bool TryTransferItem(EquippedCargoBay target, CraftedItemInstance item)
    {
        if (!Cargo.ContainsKey(item))
        {
            _itemManager.Log("Attempted to remove item from a cargo bay that it wasn't even in! Something went wrong here!");
            return false;
        }
        
        if (!target.TryStore(item)) return false;
        Remove(item);
        return true;
    }
}

public class BehaviorGroup
{
    public IBehavior[] Behaviors;
    public Trigger Trigger;
    public Switch Switch;
    //public IAnalogBehavior Axis;
}