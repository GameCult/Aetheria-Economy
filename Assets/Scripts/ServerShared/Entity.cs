/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using int2 = Unity.Mathematics.int2;

public abstract class Entity
{
    public Zone Zone;
    public MegaCorporation Faction;
    public EquippableItem Hull;
    
    public float3 Position;
    public float2 Direction = float2(0,1);
    public float2 Velocity;
    
    public float[,] Temperature;
    public float[,] NewTemperature;
    public bool2[,] HullConductivity;
    public float[,] ThermalMass;

    public readonly ReactiveCollection<EquippedItem> Equipment = new ReactiveCollection<EquippedItem>();
    public readonly ReactiveCollection<EquippedCargoBay> CargoBays = new ReactiveCollection<EquippedCargoBay>();
    public readonly ReactiveCollection<EquippedDockingBay> DockingBays = new ReactiveCollection<EquippedDockingBay>();
    public readonly ReactiveCollection<Entity> VisibleEntities = new ReactiveCollection<Entity>();
    public readonly ReactiveCollection<Entity> VisibleHostiles = new ReactiveCollection<Entity>();

    public Entity Parent;
    public List<Entity> Children = new List<Entity>();
    public ReactiveProperty<Entity> Target = new ReactiveProperty<Entity>((Entity)null);

    public float3 LookDirection;
    
    public string Name;
    
    public int Population;
    public Dictionary<Guid, float> Personality = new Dictionary<Guid, float>();
    
    public readonly Dictionary<string, float> Messages = new Dictionary<string, float>();
    public readonly Dictionary<object, float> VisibilitySources = new Dictionary<object, float>();
    public readonly ReactiveDictionary<Entity, float> EntityInfoGathered = new ReactiveDictionary<Entity, float>(); 
    public readonly Dictionary<HardpointData, (float3 position, float3 direction)> HardpointTransforms = 
        new Dictionary<HardpointData, (float3 position, float3 direction)>();
    
    public readonly (List<Weapon> weapons, List<EquippedItem> items)[] TriggerGroups;

    public List<IPopulationAssignment> PopulationAssignments = new List<IPopulationAssignment>();

    public EquippedItem[,] GearOccupancy;
    public HardpointData[,] Hardpoints;
    public float[,] Armor;
    public float[,] MaxArmor;
    
    private EquippedItem[] _orderedEquipment;
    private List<Weapon> _weapons = new List<Weapon>();
    private List<Capacitor> _capacitors = new List<Capacitor>();
    private List<Reactor> _reactors = new List<Reactor>();
    private List<Radiator> _heatsinks = new List<Radiator>();
    protected bool _active;

    private bool _heatsinksEnabled = true;

    public bool HeatsinksEnabled
    {
        get => _heatsinksEnabled;
        set
        {
            if (value == _heatsinksEnabled) return;
            _heatsinksEnabled = value;
            foreach (var heatsink in _heatsinks)
                heatsink.Item.Enabled.Value = value;
        }
    }
    
    public EntitySettings Settings { get; }
    
    public bool OverrideShutdown { get; set; }
    
    public bool Active
    {
        get => _active;
    }
    
    public IEnumerable<Weapon> Weapons
    {
        get => _weapons;
    }
    
    public Shield Shield { get; private set; }
    public Cockpit Cockpit { get; private set; }
    public float Heatstroke { get; private set; }
    
    public float TargetRange { get; private set; }
    public float MaxTemp { get; private set; }
    public float MinTemp { get; private set; }
    public ItemManager ItemManager { get; }
    public int AssignedPopulation => PopulationAssignments.Sum(pa => pa.AssignedPopulation);
    public float Mass { get; private set; }
    public float Visibility => VisibilitySources.Values.Sum();
    
    public Subject<(int2 pos, float damage)> ArmorDamage = new Subject<(int2, float)>();
    public Subject<(EquippedItem item, float damage)> ItemDamage = new Subject<(EquippedItem, float)>();
    // public Subject<EquippedItem> ItemOffline = new Subject<EquippedItem>();
    // public Subject<EquippedItem> ItemOnline = new Subject<EquippedItem>();
    public Subject<float> HullDamage = new Subject<float>();
    public Subject<Entity> Docked = new Subject<Entity>();
    public Subject<Unit> HeatstrokeRisk = new Subject<Unit>();
    public Subject<Unit> HeatstrokeDeath = new Subject<Unit>();
    
    public UniRx.IObservable<EquippedItem> ItemDestroyed;
    public UniRx.IObservable<int2> HullArmorDepleted;
    public UniRx.IObservable<HardpointData> HardpointArmorDepleted;
    public UniRx.IObservable<Weapon> WeaponDestroyed;
    public UniRx.IObservable<Unit> Death;

    private List<IDisposable> _subscriptions = new List<IDisposable>();

    public virtual void Activate()
    {
        _active = true;
        Heatstroke = 0;
        foreach (var item in Equipment)
        foreach (var behavior in item.Behaviors)
        {
            if(behavior is IInitializableBehavior initializableBehavior)
                initializableBehavior.Initialize();
        }
        //foreach(var entity in Zone.Entities) EntityInfoGathered.Add(entity, 0);
        _subscriptions.Add(Zone.Entities.ObserveRemove().Subscribe(remove =>
        {
            if (Target.Value == remove.Value) Target.Value = null;
            EntityInfoGathered.Remove(remove.Value);
            VisibleEntities.Remove(remove.Value);
            VisibleHostiles.Remove(remove.Value);
        }));
        _subscriptions.Add(VisibleEntities.ObserveRemove().Subscribe(remove =>
        {
            if (Target.Value == remove.Value) Target.Value = null;
        }));
        GenerateTriggerGroups();
    }

    public virtual void Deactivate()
    {
        foreach(var s in _subscriptions) s.Dispose();
        _subscriptions.Clear();
        _active = false;
        EntityInfoGathered.Clear();
        VisibleEntities.Clear();
        VisibleHostiles.Clear();
    }

    public Entity(ItemManager itemManager, Zone zone, EquippableItem hull, EntitySettings settings)
    {
        Settings = settings;
        ItemManager = itemManager;
        Zone = zone;
        Hull = hull;
        Name = hull.Name;
        MapEntity();
        TriggerGroups = new (List<Weapon> triggers, List<EquippedItem> items)[itemManager.GameplaySettings.TriggerGroupCount];
        for(int i=0; i<itemManager.GameplaySettings.TriggerGroupCount; i++)
            TriggerGroups[i] = (new List<Weapon>(), new List<EquippedItem>());

        ItemDestroyed = ItemDamage.Where(x => x.item.EquippableItem.Durability < .01f).Select(x=>x.item);
        WeaponDestroyed = ItemDestroyed.Select(x => x.Behaviors.FirstOrDefault(b => b is Weapon) as Weapon).Where(x => x != null);
        HullArmorDepleted = ArmorDamage.Where(x => Armor[x.pos.x, x.pos.y] < .01f).Select(x => x.pos);
        Death = HullDamage.Select(_ => Unit.Default).Where(_ => Hull.Durability < .01f).Merge(HeatstrokeDeath);

        EntityInfoGathered.ObserveReplace().Subscribe(replace =>
        {
            if (replace.OldValue < ItemManager.GameplaySettings.TargetDetectionInfoThreshold &&
                replace.NewValue > ItemManager.GameplaySettings.TargetDetectionInfoThreshold)
            {
                VisibleEntities.Add(replace.Key);
                if(IsHostileTo(replace.Key))
                    VisibleHostiles.Add(replace.Key);
            }
            if (replace.OldValue > ItemManager.GameplaySettings.TargetDetectionInfoThreshold &&
                replace.NewValue < ItemManager.GameplaySettings.TargetDetectionInfoThreshold)
            {
                VisibleEntities.Remove(replace.Key);
                if(IsHostileTo(replace.Key))
                    VisibleHostiles.Remove(replace.Key);
            }
        });
        EntityInfoGathered.ObserveRemove().Subscribe(remove => VisibleEntities.Remove(remove.Key));
    }

    public bool IsHostileTo(Entity other)
    {
        if (Faction == null)
        {
            return other.Faction != null;
        }

        return Faction.PlayerHostile && other.Faction == null;
    }

    private void MapEntity()
    {
        var hullData = ItemManager.GetData(Hull) as HullData;
        Equipment.Add(new EquippedItem(ItemManager, Hull, int2.zero, this));
        Mass = hullData.Mass;
        Temperature = new float[hullData.Shape.Width, hullData.Shape.Height];
        NewTemperature = new float[hullData.Shape.Width, hullData.Shape.Height];
        HullConductivity = new bool2[hullData.Shape.Width,hullData.Shape.Height];
        ThermalMass = new float[hullData.Shape.Width, hullData.Shape.Height];
        Armor = new float[hullData.Shape.Width, hullData.Shape.Height];
        MaxArmor = new float[hullData.Shape.Width, hullData.Shape.Height];
        Hardpoints = new HardpointData[hullData.Shape.Width, hullData.Shape.Height];
        foreach (var hardpoint in hullData.Hardpoints)
        {
            foreach (var hardpointCoord in hardpoint.Shape.Coordinates)
            {
                var hullCoord = hardpoint.Position + hardpointCoord;
                Hardpoints[hullCoord.x, hullCoord.y] = hardpoint;
            }
        }
        var cellCount = hullData.Shape.Coordinates.Length;
        foreach (var v in hullData.Shape.Coordinates)
        {
            Armor[v.x, v.y] = hullData.Armor;
            MaxArmor[v.x, v.y] = hullData.Armor;
            if (Hardpoints[v.x, v.y] != null)
            {
                Armor[v.x, v.y] += Hardpoints[v.x, v.y].Armor;
                MaxArmor[v.x, v.y] += Hardpoints[v.x, v.y].Armor;
            }
            Temperature[v.x, v.y] = 280;
            ThermalMass[v.x, v.y] = hullData.Mass * hullData.SpecificHeat / cellCount;
        }
        GearOccupancy = new EquippedItem[hullData.Shape.Width, hullData.Shape.Height];
    }

    public void GenerateTriggerGroups()
    {
        foreach (var group in Weapons
            .GroupBy(w => w.Item.EquippableItem.Data)
            .Select((weapons, index) => (weapons, index)))
        {
            TriggerGroups[group.index].weapons = group.weapons.ToList();
            TriggerGroups[group.index].items = group.weapons.Select(w=>w.Item).ToList();
        }
    }

    public void AddHeat(int2 position, float heat, bool ignoreThermalMass = false)
    {
        if (ignoreThermalMass)
            Temperature[position.x, position.y] += heat;
        else
            Temperature[position.x, position.y] += heat / ThermalMass[position.x, position.y];
    }

    public int CountItemsInCargo(Guid itemDataID)
    {
        int sum = 0;
        foreach (var x in CargoBays)
        {
            if (x.ItemsOfType.ContainsKey(itemDataID))
            {
                foreach (var i in x.ItemsOfType[itemDataID]) sum += i is SimpleCommodity simpleCommodity ? simpleCommodity.Quantity : 1;
            }
        }

        return sum;
    }

    public EquippedCargoBay FindItemInCargo(Guid itemDataID)
    {
        return CargoBays.FirstOrDefault(c => c.ItemsOfType.ContainsKey(itemDataID));
    }

    // Attempts to move a given number of items of the given type to the target Entity
    // Returns the number of items successfully transferred
    public int TryTransferItems(Entity target, Guid itemDataID, int quantity)
    {
        int quantityTransferred = 0;
        while (quantityTransferred < quantity)
        {
            EquippedCargoBay originInventory = CargoBays.FirstOrDefault(c => c.ItemsOfType.ContainsKey(itemDataID));

            if (originInventory == null) break;

            var itemInstance = originInventory.ItemsOfType[itemDataID][0];

            if (itemInstance is SimpleCommodity simpleCommodity)
            {
                var targetQuantity = min(simpleCommodity.Quantity, quantity - quantityTransferred);
                if (!target.CargoBays.Any(c => originInventory.TryTransferItem(c, simpleCommodity, targetQuantity)))
                {
                    quantityTransferred += targetQuantity - simpleCommodity.Quantity;
                    break;
                }

                quantityTransferred += targetQuantity;
            }
            else if (itemInstance is CraftedItemInstance craftedItemInstance)
            {
                if (!target.CargoBays.Any(c => originInventory.TryTransferItem(c, craftedItemInstance)))
                    break;
                
                quantityTransferred++;
            }
        }

        return quantityTransferred;
    }

    public EquippableItem TryUnequip(EquippedItem item)
    {
        // Don't allow unequipping when the entity is active
        if (_active) return null;
        
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

            CargoBays.Remove(cargoBay);
        }
        
        Equipment.Remove(item);
        _orderedEquipment = Equipment.OrderBy(x => x.SortPosition).ToArray();
        
        var hullData = ItemManager.GetData(Hull) as HullData;
        var itemData = ItemManager.GetData(item.EquippableItem);
        foreach (var i in hullData.Shape.Coordinates)
            if (GearOccupancy[i.x, i.y] == item)
            {
                ThermalMass[i.x, i.y] -= ItemManager.GetThermalMass(item.EquippableItem) / itemData.Shape.Coordinates.Length;
                GearOccupancy[i.x, i.y] = null;
            }
        Mass -= itemData.Mass;
        foreach (var b in item.Behaviors)
        {
            if (b is Weapon weapon)
                _weapons.Remove(weapon);
            if (b is Capacitor capacitor)
                _capacitors.Remove(capacitor);
            if (b is Reactor reactor)
                _reactors.Remove(reactor);
            if (b is Radiator heatsink)
                _heatsinks.Remove(heatsink);
            if (b is Shield)
                Shield = null;
            if (b is Cockpit)
                Cockpit = null;
        }

        return item.EquippableItem;
    }

    // Check whether the given item will fit when its origin is placed at the given coordinate
    private bool ItemFits(EquippableItemData itemData, HullData hullData, EquippableItem item, int2 hullCoord)
    {
        // If the given coordinate isn't even in the ship it obviously won't fit
        if (!hullData.Shape[hullCoord]) return false;
        
        // Items without specific hardpoints on the ship can be freely rotated and placed anywhere
        if (itemData.HardpointType == HardpointType.Tool)
        {
            // Check every cell of the item's shape
            foreach (var i in itemData.Shape.Coordinates)
            {
                // If there is any gear already occupying that space, it won't fit
                // If there's a hardpoint there, it won't fit
                // Thermal items have their own layer and do not collide with gear
                var itemCoord = hullCoord + itemData.Shape.Rotate(i, item.Rotation);
                if (!hullData.InteriorCells[itemCoord] || 
                    itemData.HardpointType == HardpointType.Tool && Hardpoints[itemCoord.x, itemCoord.y] != null || 
                    GearOccupancy[itemCoord.x, itemCoord.y] != null) 
                    return false;
            }
        }
        else
        {
            var hardpoint = Hardpoints[hullCoord.x, hullCoord.y];
            
            // If there's no hardpoint there, it won't fit
            if (hardpoint == null) return false;

            // If the hardpoint type doesn't match the item, it won't fit
            if (hardpoint.Type != itemData.HardpointType) return false;
            
            // Items placed in hardpoints are automatically aligned to hardpoint rotation
            item.Rotation = hardpoint.Rotation;

            // Inset the shapes of both item and hardpoint
            var itemShapeInset = hullData.Shape.Inset(itemData.Shape, hullCoord, item.Rotation);
            var hardpointShapeInset = hullData.Shape.Inset(hardpoint.Shape, hardpoint.Position);
            
            // Check every cell of the hardpoint shape
            foreach(var v in hardpointShapeInset.Coordinates)
                if (GearOccupancy[v.x, v.y] != null)
                    return false;
            
            // Check every cell of the item's shape
            foreach (var i in itemShapeInset.Coordinates)
            {
                // If the hardpoint does not have a matching cell, it wont fit
                if (!hardpointShapeInset[i]) return false;
            
                // If there is any gear already occupying that space, it won't fit
                if (GearOccupancy[i.x, i.y] != null) return false;
            }
        }

        return true;
    }

    // Check whether the given item will fit when its origin is placed at the given coordinate on the hull
    public bool ItemFits(EquippableItem item, int2 hullCoord)
    {
        // Don't allow equipping while deployed
        if (_active) return false;

        var itemData = ItemManager.GetData(item);
        var hullData = ItemManager.GetData(Hull) as HullData;
        return ItemFits(itemData, hullData, item, hullCoord);
    }

    public bool TryFindSpace(EquippableItem item, out int2 hullCoord)
    {
        // Don't allow equipping while deployed
        if (_active)
        {
            hullCoord = int2.zero;
            return false;
        }
        var itemData = ItemManager.GetData(item);
        var hullData = ItemManager.GetData(Hull) as HullData;
        
        // Tools and thermal equipment can be installed anywhere on the ship
        // Search the whole ship for somewhere the item will fit
        if (itemData.HardpointType == HardpointType.Tool)
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
        // Don't allow equipping while deployed
        if (_active) return false;
        
        var itemData = ItemManager.GetData(item);
        var hullData = ItemManager.GetData(Hull) as HullData;

        if (!ItemFits(itemData, hullData, item, hullCoord)) return false;
        
        EquippedItem equippedItem;
        if (itemData.HardpointType == HardpointType.Tool)
        {
            if(itemData is CargoBayData)
            {
                if (itemData is DockingBayData)
                {
                    equippedItem = new EquippedDockingBay(ItemManager, item, hullCoord, this, $"{Name} Docking Bay {DockingBays.Count + 1}");
                    DockingBays.Add((EquippedDockingBay) equippedItem);
                }
                else
                {
                    equippedItem = new EquippedCargoBay(ItemManager, item, hullCoord, this, $"{Name} Cargo Bay {CargoBays.Count + 1}");
                    CargoBays.Add((EquippedCargoBay) equippedItem);
                }
            }
            else
            {
                equippedItem = new EquippedItem(ItemManager, item, hullCoord, this);
                Equipment.Add(equippedItem);
            }
        }
        else
        {
            equippedItem = new EquippedItem(ItemManager, item, hullCoord, this);
            Equipment.Add(equippedItem);
        }
        
        foreach (var b in equippedItem.Behaviors)
        {
            if (b is Weapon weapon)
                _weapons.Add(weapon);
            if(b is Capacitor capacitor)
                _capacitors.Add(capacitor);
            if(b is Reactor reactor)
                _reactors.Add(reactor);
            if(b is Radiator heatsink)
                _heatsinks.Add(heatsink);
            if (b is Shield shield)
                Shield = shield;
            if (b is Cockpit cockpit)
                Cockpit = cockpit;
        }

        // equippedItem.OnOnline += () => ItemOnline.OnNext(equippedItem);
        // equippedItem.OnOffline += () => ItemOffline.OnNext(equippedItem);
            
        foreach (var i in itemData.Shape.Coordinates)
        {
            var occupiedCoord = hullCoord + itemData.Shape.Rotate(i, item.Rotation);
            // TODO: Track thermal mass of cargo bay contents as reactive property
            ThermalMass[occupiedCoord.x, occupiedCoord.y] += ItemManager.GetThermalMass(item) / itemData.Shape.Coordinates.Length;
            GearOccupancy[occupiedCoord.x, occupiedCoord.y] = equippedItem;
        }
                
        Mass += itemData.Mass;
        _orderedEquipment = Equipment.OrderBy(x => x.SortPosition).ToArray();
        return true;
    }

    public EquippedDockingBay TryDock(Ship ship)
    {
        var bay = DockingBays.FirstOrDefault(x => x.DockedShip == null);
        if (bay != null)
        {
            bay.DockedShip = ship;
            ship.SetParent(this);
            Zone.Entities.Remove(ship);
            Deactivate();
            ship.Docked.OnNext(this);
        }

        return bay;
    }

    public bool TryUndock(Ship ship)
    {
        var bay = DockingBays.FirstOrDefault(x => x.DockedShip == ship);
        if (bay == null)
        {
            ItemManager.Log($"Ship {ship.Name} attempted to undock from {Name}, but it was not docked!");
            return false;
        }

        if (bay.Cargo.Any())
            return false;

        bay.DockedShip = null;
        ship.RemoveParent();
        Zone.Entities.Add(ship);
        ship.Activate();

        return true;
    }

    public bool CanConsumeEnergy(float energy)
    {
        var capEnergy = _capacitors.Sum(cap => cap.Charge);
        int onlineReactors = _reactors.Count(reactor=>reactor.Item.Online.Value);
        return capEnergy > energy || onlineReactors > 0;
    }

    public bool TryConsumeEnergy(float energy)
    {
        if (energy < .01f) return true;
        int chargedCapacitors;
        do
        {
            chargedCapacitors = _capacitors.Count(capacitor => capacitor.Charge > .01f);
            var chargeToRemove = energy;
            foreach (var cap in _capacitors)
            {
                if(cap.Charge > 0.01f)
                {
                    var chargeRemoved = min(chargeToRemove / chargedCapacitors, cap.Charge);
                    cap.AddCharge(-chargeRemoved);
                    energy -= chargeRemoved;
                }
            }
        } while (chargedCapacitors > 0 && energy > .01f);

        if (energy < .01f) return true;

        int onlineReactors = _reactors.Count(reactor=>reactor.Item.Online.Value);
        foreach (var reactor in _reactors)
        {
            if (reactor.Item.Online.Value)
            {
                reactor.ConsumeEnergy(energy / onlineReactors);
            }
        }

        return onlineReactors > 0;
    }

    private void AddChild(Entity entity)
    {
        Mass += entity.Mass;
        Children.Add(entity);
    }

    private void RemoveChild(Entity entity)
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
        foreach (var equippedItem in Equipment)
            if(equippedItem.Behaviors != null)
                foreach (var behavior in equippedItem.Behaviors)
                    if (behavior is T b)
                        return b;
        return null;
    }

    public IEnumerable<T> GetBehaviors<T>() where T : class, IBehavior
    {
        foreach (var equippedItem in Equipment)
            if(equippedItem.Behaviors != null)
                foreach (var behavior in equippedItem.Behaviors)
                    if (behavior is T b)
                        yield return b;
    }

    public IEnumerable<T> GetBehaviorData<T>() where T : BehaviorData
    {
        foreach (var equippedItem in Equipment)
            foreach (var behavior in equippedItem.Behaviors)
                if (behavior.Data is T b)
                    yield return b;
    }

    // public IEnumerable<(T t, Switch s)> GetSwitch<T>() where T : class, IBehavior
    // {
    //     foreach (var equippedItem in Equipment)
    //         foreach (var group in equippedItem.BehaviorGroups.Values)
    //             foreach(var behavior in group.Behaviors)
    //                 if (behavior is T t)
    //                 {
    //                     var s = group.GetExposed<Switch>();
    //                     if (s != null) yield return (t, s);
    //                 }
    // }
    //
    // public IEnumerable<(T behavior, Trigger trigger)> GetTrigger<T>() where T : class, IBehavior
    // {
    //     foreach (var equippedItem in Equipment)
    //         foreach (var group in equippedItem.BehaviorGroups.Values)
    //             foreach(var behavior in group.Behaviors)
    //                 if (behavior is T t)
    //                 {
    //                     var s = group.GetExposed<Trigger>();
    //                     if (s != null) yield return (t, s);
    //                 }
    // }
    //
    // public IEnumerable<(T behavior, Axis axis)> GetAxis<T>() where T : class, IBehavior
    // {
    //     foreach (var equippedItem in Equipment)
    //         foreach (var group in equippedItem.BehaviorGroups.Values)
    //             foreach(var behavior in group.Behaviors)
    //                 if (behavior is T t)
    //                 {
    //                     var s = group.GetExposed<Axis>();
    //                     if (s != null) yield return (t, s);
    //                 }
    // }

    public virtual void Update(float delta)
    {
        var hullData = ItemManager.GetData(Hull) as HullData;

        TargetRange = Target.Value == null ? -1 : length(Position - Target.Value.Position);

        foreach (var v in VisibilitySources.Keys.ToArray())
        {
            VisibilitySources[v] *= max(1 - ItemManager.GameplaySettings.VisibilityDecay * delta, 0);

            if (VisibilitySources[v] < 0.01f) VisibilitySources.Remove(v);
        }

        UpdateTemperature(delta);
        
        
        if (_active)
        {
            if(Cockpit != null)
            {
                if (Cockpit.Item.Temperature > ItemManager.GameplaySettings.HeatstrokeTemperature)
                {
                    var previous = Heatstroke;
                    Heatstroke = saturate(
                        Heatstroke +
                        pow(Cockpit.Item.Temperature - ItemManager.GameplaySettings.HeatstrokeTemperature, ItemManager.GameplaySettings.HeatstrokeExponent) *
                        ItemManager.GameplaySettings.HeatstrokeMultiplier * delta);
                    if(previous < ItemManager.GameplaySettings.SevereHeatstrokeRiskThreshold && Heatstroke > ItemManager.GameplaySettings.SevereHeatstrokeRiskThreshold)
                        HeatstrokeRisk.OnNext(Unit.Default);
                    if(Heatstroke > 1)
                    {
                        HeatstrokeDeath.OnNext(Unit.Default);
                        Deactivate();
                    }
                }
                else
                {
                    Heatstroke = saturate(Heatstroke - ItemManager.GameplaySettings.HeatstrokeRecoverySpeed * delta);
                }
            }
            
            foreach (var equippedItem in _orderedEquipment)
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
        foreach(var child in Children)
            child.Update(delta);

        if (Parent != null)
        {
            Position = Parent.Position;
            Velocity = Parent.Velocity;
        }
        else Position.y = Zone.GetHeight(Position.xz) + hullData.GridOffset;
    }

    private void UpdateTemperature(float delta)
    {
        var hullData = ItemManager.GetData(Hull) as HullData;
        
        MaxTemp = Single.MinValue;
        MinTemp = Single.MaxValue;
        
        //float[,] newTemp = new float[hullData.Shape.Width,hullData.Shape.Height];
        var radiation = 0f;
        foreach (var v in hullData.Shape.Coordinates)
        {
            var temp = Temperature[v.x, v.y];
            var totalTemp = temp / ItemManager.GameplaySettings.HeatConductionMultiplier;
            var totalConductivity = 1f / ItemManager.GameplaySettings.HeatConductionMultiplier;
            
            if (hullData.Shape[int2(v.x - 1, v.y)])
            {
                var conductivity = (GearOccupancy[v.x, v.y]?.Conductivity ?? 1) *
                                   (GearOccupancy[v.x - 1, v.y]?.Conductivity ?? 1) *
                                   (HullConductivity[v.x - 1, v.y].x ? hullData.Conductivity : 1 / hullData.Conductivity) *
                                   (ThermalMass[v.x - 1, v.y] / ThermalMass[v.x, v.y]);
                totalConductivity += conductivity;
                totalTemp += Temperature[v.x - 1, v.y] * conductivity;
            }

            if (hullData.Shape[int2(v.x + 1, v.y)])
            {
                var conductivity = (GearOccupancy[v.x, v.y]?.Conductivity ?? 1) *
                                   (GearOccupancy[v.x + 1, v.y]?.Conductivity ?? 1) *
                                   (HullConductivity[v.x, v.y].x ? hullData.Conductivity : 1 / hullData.Conductivity) *
                                   (ThermalMass[v.x + 1, v.y] / ThermalMass[v.x, v.y]);
                totalConductivity += conductivity;
                totalTemp += Temperature[v.x + 1, v.y] * conductivity;
            }


            if (hullData.Shape[int2(v.x, v.y - 1)])
            {
                var conductivity = (GearOccupancy[v.x, v.y]?.Conductivity ?? 1) *
                                   (GearOccupancy[v.x, v.y - 1]?.Conductivity ?? 1) * 
                                   (HullConductivity[v.x, v.y - 1].y ? hullData.Conductivity : 1 / hullData.Conductivity) *
                                   (ThermalMass[v.x, v.y - 1] / ThermalMass[v.x, v.y]);
                totalConductivity += conductivity;
                totalTemp += Temperature[v.x, v.y - 1] * conductivity;
            }


            if (hullData.Shape[int2(v.x, v.y + 1)])
            {
                var conductivity = (GearOccupancy[v.x, v.y]?.Conductivity ?? 1) *
                                   (GearOccupancy[v.x, v.y + 1]?.Conductivity ?? 1) * 
                                   (HullConductivity[v.x, v.y].y ? hullData.Conductivity : 1 / hullData.Conductivity) *
                                   (ThermalMass[v.x, v.y + 1] / ThermalMass[v.x, v.y]);
                totalConductivity += conductivity;
                totalTemp += Temperature[v.x, v.y + 1] * conductivity;
            }
            
            NewTemperature[v.x, v.y] = totalTemp / totalConductivity;

            var r = 0f;
            // For all cells on the border of the entity, radiate some heat into space, increasing the visibility of the ship
            if (Parent==null && !hullData.InteriorCells[v])
            {
                var rad = pow(NewTemperature[v.x, v.y], ItemManager.GameplaySettings.HeatRadiationExponent) *
                          ItemManager.GameplaySettings.HeatRadiationMultiplier;
                NewTemperature[v.x, v.y] -= rad * delta;
                r += rad;
            }

            radiation += r;
            
            if(float.IsNaN(NewTemperature[v.x, v.y]) || NewTemperature[v.x, v.y] < 0)
                ItemManager.Log("HOUSTON, WE HAVE A PROBLEM!");

            if (NewTemperature[v.x, v.y] < MinTemp)
                MinTemp = NewTemperature[v.x, v.y];
            
            if (NewTemperature[v.x, v.y] > MaxTemp)
                MaxTemp = NewTemperature[v.x, v.y];
        }

        VisibilitySources[this] = radiation;
        var swap = Temperature;
        Temperature = NewTemperature;
        NewTemperature = swap;
    }

    public void SetMessage(string message)
    {
        Messages[message] = ItemManager.GameplaySettings.MessageDuration;
    }
}

public class EquippedItem
{
    public int SortPosition;
    public EquippableItem EquippableItem;
    public int2 Position;
    public Dictionary<int, BehaviorGroup> BehaviorGroups;

    private bool _thermalOnline;
    private bool _durabilityOnline;
    
    public IBehavior[] Behaviors { get; }
    public float Conductivity { get; }
    public float ThermalPerformance { get; private set; }
    public float ThermalExponent { get; }
    public float DurabilityPerformance { get; private set; }
    public float DurabilityExponent { get; }
    public float Wear { get; private set; }
    public Shape InsetShape { get; }
    public Entity Entity { get; }
    public EquippableItemData Data { get; }

    public ReactiveProperty<bool> ThermalOnline { get; } = new ReactiveProperty<bool>(false);
    public ReactiveProperty<bool> DurabilityOnline { get; } = new ReactiveProperty<bool>(false);
    public ReadOnlyReactiveProperty<bool> Online { get; }
    public ReactiveProperty<bool> Enabled { get; } = new ReactiveProperty<bool>(true);
    public ReadOnlyReactiveProperty<bool> Active { get; }


    public float Temperature
    {
        get
        {
            float sum = 0;
            foreach (var x in InsetShape.Coordinates) sum += Entity.Temperature[x.x, x.y];
            return sum/InsetShape.Coordinates.Length;
        }
    }
    

    protected readonly ItemManager _itemManager;
    //private bool _online;

    public EquippedItem(ItemManager itemManager, EquippableItem item, int2 position, Entity entity)
    {
        _itemManager = itemManager;
        Data = _itemManager.GetData(item);
        Entity = entity;
        EquippableItem = item;
        Position = position;
        Conductivity = Data.Conductivity;
        ThermalExponent = _itemManager.Evaluate(Data.HeatExponent, EquippableItem);
        DurabilityExponent = _itemManager.Evaluate(Data.DurabilityExponent, EquippableItem);
        var hullData = itemManager.GetData(entity.Hull);
        InsetShape = hullData.Shape.Inset(Data.Shape, position, item.Rotation);

        Online = new ReadOnlyReactiveProperty<bool>(ThermalOnline
            .CombineLatest(DurabilityOnline, (thermal, durability) => thermal && durability).DistinctUntilChanged());
        Active = new ReadOnlyReactiveProperty<bool>(Enabled
            .CombineLatest(Online, (enabled, online) => enabled && online).DistinctUntilChanged());
        

        Behaviors = Data.Behaviors
            .Select(bd => bd.CreateInstance(itemManager, entity, this))
            .ToArray();

        BehaviorGroups = Behaviors
            .GroupBy(b => b.Data.Group)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => new BehaviorGroup
            {
                Behaviors = g.ToArray()
            });

        foreach (var behavior in Behaviors)
        {
            if (behavior is IOrderedBehavior orderedBehavior)
                SortPosition = orderedBehavior.Order;
            if(behavior is IPopulationAssignment populationAssignment)
                entity.PopulationAssignments.Add(populationAssignment);
        }
    }

    public float Evaluate(PerformanceStat stat)
    {
        var heat = 1f;
        if(stat.HeatDependent)
            heat = pow(ThermalPerformance, ThermalExponent * stat.HeatExponentMultiplier);
        var durability = 1f;
        if(stat.DurabilityDependent)
            durability = pow(DurabilityPerformance, DurabilityExponent);
        var quality = pow(_itemManager.Quality(stat, EquippableItem), stat.QualityExponent);

        var scaleModifier = 1.0f;
        foreach (var value in stat.GetScaleModifiers(Entity).Values) scaleModifier = scaleModifier * value;

        float constantModifier = 0;
        foreach (var value in stat.GetConstantModifiers(Entity).Values) constantModifier += value;

        var result = lerp(stat.Min, stat.Max, durability * quality * heat) * scaleModifier + constantModifier;
        if (float.IsNaN(result))
            return stat.Min;
        return result;
    }

    public void AddHeat(float heat, bool ignoreThermalMass = false)
    {
        foreach(var hullCoord in InsetShape.Coordinates)
            Entity.AddHeat(hullCoord, heat / InsetShape.Coordinates.Length, ignoreThermalMass);
    }

    public void Update(float delta)
    {
        foreach (var behavior in Behaviors)
            if(behavior is IAlwaysUpdatedBehavior alwaysUpdatedBehavior) alwaysUpdatedBehavior.Update(delta);

        var temp = Temperature;
        ThermalPerformance = Data.Performance(temp);
        DurabilityPerformance = EquippableItem.Durability / Data.Durability;
        var performanceThreshold = Entity.Settings.ShutdownPerformance;
        Wear = (1 - pow(Data.Performance(temp),
                (1 - pow(_itemManager.CompoundQuality(EquippableItem), _itemManager.GameplaySettings.QualityWearExponent)) *
                _itemManager.GameplaySettings.ThermalWearExponent)
            ) * Data.Durability / Data.ThermalResilience;
        ThermalOnline.Value = ThermalPerformance > performanceThreshold || Entity.OverrideShutdown && EquippableItem.OverrideShutdown;
        DurabilityOnline.Value = EquippableItem.Durability > .01f;

        if (Active.Value)
        {
            foreach (var group in BehaviorGroups.Values)
            {
                foreach (var behavior in group.Behaviors)
                {
                    if (!behavior.Execute(delta))
                        break;
                }
            }
        }
    }

    public T GetBehavior<T>() where T : class, IBehavior
    {
        foreach (var behavior in Behaviors)
            if (behavior is T b)
                return b;
        return null;
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
    public string Name { get; }
    
    public EquippedCargoBay(ItemManager itemManager, EquippableItem item, int2 position, Entity entity, string name) : base(itemManager, item, position, entity)
    {
        Data = _itemManager.GetData(EquippableItem) as CargoBayData;
        Name = name;

        Mass = Data.Mass;
        ThermalMass = Data.Mass * Data.SpecificHeat;
        
        Occupancy = new ItemInstance[Data.InteriorShape.Width,Data.InteriorShape.Height];
    }

    // Check whether the given item will fit when its origin is placed at the given coordinate
    public bool ItemFits(ItemInstance item, int2 cargoCoord)
    {
        var itemData = _itemManager.GetData(item);
        // Check every cell of the item's shape
        foreach (var i in itemData.Shape.Coordinates)
        {
            // If there is an item already occupying that space, it won't fit
            var itemCargoCoord = cargoCoord + itemData.Shape.Rotate(i, item.Rotation);
            if (!Data.InteriorShape[itemCargoCoord] || (Occupancy[itemCargoCoord.x, itemCargoCoord.y] != null && Occupancy[itemCargoCoord.x, itemCargoCoord.y] != item)) return false;
        }

        return true;
    }
    
    public bool TryFindSpace(ItemInstance item)
    {
        if (item is SimpleCommodity simpleCommodity)
            return TryFindSpace(simpleCommodity, out _);
        if (item is CraftedItemInstance craftedItem)
            return TryFindSpace(craftedItem, out _);
        return false;
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
    
    public bool TryStore(ItemInstance item)
    {
        if (item is SimpleCommodity simpleCommodity)
            return TryStore(simpleCommodity);
        if (item is CraftedItemInstance craftedItem)
            return TryStore(craftedItem);
        return false;
    }

    public bool TryStore(ItemInstance item, int2 cargoCoord)
    {
        if (item is SimpleCommodity simpleCommodity)
            return TryStore(simpleCommodity, cargoCoord);
        if (item is CraftedItemInstance craftedItem)
            return TryStore(craftedItem, cargoCoord);
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
            foreach(var v in Data.InteriorShape.Coordinates)
                if (Occupancy[v.x, v.y] == item)
                    Occupancy[v.x, v.y] = null;

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
        foreach(var v in Data.InteriorShape.Coordinates)
            if (Occupancy[v.x, v.y] == item)
                Occupancy[v.x, v.y] = null;
        
        Cargo.Remove(item);
        ItemsOfType[item.Data].Remove(item);
        if (!ItemsOfType[item.Data].Any())
            ItemsOfType.Remove(item.Data);
        
        Mass -= _itemManager.GetMass(item);
        ThermalMass -= _itemManager.GetThermalMass(item);
    }

    public void Remove(ItemInstance item)
    {
        if (item is SimpleCommodity simpleCommodity)
            Remove(simpleCommodity, simpleCommodity.Quantity);
        if (item is CraftedItemInstance craftedItem)
            Remove(craftedItem);
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

public class EquippedDockingBay : EquippedCargoBay
{
    public Ship DockedShip;
    public int2 MaxSize => _data.MaxSize;
    private DockingBayData _data;
    public EquippedDockingBay(ItemManager itemManager, EquippableItem item, int2 position, Entity entity, string name) : base(itemManager, item, position, entity, name)
    {
        _data = _itemManager.GetData(EquippableItem) as DockingBayData;
    }
}

public class BehaviorGroup
{
    public IBehavior[] Behaviors;

    public T GetBehavior<T>() where T : class, IBehavior
    {
        foreach (var b in Behaviors)
        {
            if (!(b is T s)) continue;
            return s;
        }

        return null;
    }
    
    public T GetExposed<T>() where T : class, IBehavior, IInteractiveBehavior
    {
        foreach (var b in Behaviors)
        {
            if (!(b is T s) || !((IInteractiveBehavior)b).Exposed) continue;
            return s;
        }

        return null;
    }
    
    //public IAnalogBehavior Axis;
}