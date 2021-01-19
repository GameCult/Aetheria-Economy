using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using RethinkDb.Driver.Ast;
using Unity.Mathematics;

[MessagePackObject]
public class EntityPack
{
    [Key(0)]
    public string Name;
    
    [Key(1)]
    public EquippableItem Hull;
    
    [Key(2)]
    public (int2 position, EquippableItem item)[] Equipment;
    
    [Key(3)]
    public (int2 position, EquippableItem item)[] CargoBays;
    
    [Key(4)]
    public (int2 position, EquippableItem item)[] DockingBays;

    [Key(5)]
    public Dictionary<int2, PersistentBehaviorData[]> PersistedBehaviors;

    [Key(6)]
    public float[,] Temperature;
    
    [Key(7)]
    public float[,] Armor;
    
    [Key(8)]
    public bool2[,] Conductivity;
    
    [Key(9)]
    public Dictionary<int, float> HardpointArmor;
    
    [Key(10)]
    public int[] DockingBayAssignments;
    
    [Key(11)]
    public (int2 position, ItemInstance item)[][] CargoContents;
    
    [Key(12)]
    public (int2 position, ItemInstance item)[][] DockingBayContents;

    [Key(13)]
    public EntityPack[] Children;

    private int _price;

    public static EntityPack Pack(Entity entity)
    {
        var pack = new EntityPack();
        
        // Filter item behavior collections by those with any persistent behaviors
        // For each item create an object containing the item position and a list of persistent behaviors
        // Then turn that into a dictionary mapping from item position to an array of every behaviors persistent data
        pack.PersistedBehaviors = entity.Equipment
            .Where(item => item.Behaviors.Any(b=>b is IPersistentBehavior))
            .Select(item => new {equippable=item, behaviors = item.Behaviors
                .Where(b=>b is IPersistentBehavior)
                .Cast<IPersistentBehavior>()})
            .ToDictionary(x=> x.equippable.Position, x=>x.behaviors.Select(b => b.Store()).ToArray());

        pack.Hull = entity.Hull;
        pack.Name = entity.Name;
        pack.Equipment = entity.Equipment.Select(e => (e.Position, e.EquippableItem)).ToArray();
        pack.CargoBays = entity.CargoBays.Select(e => (e.Position, e.EquippableItem)).ToArray();
        pack.DockingBays = entity.DockingBays.Select(e => (e.Position, e.EquippableItem)).ToArray();
        pack.DockingBayAssignments = entity.DockingBays.Select(x => entity.Children.IndexOf(x.DockedShip)).ToArray();
        pack.CargoContents = entity.CargoBays.Select(b => b.Cargo.Select(i => (i.Value, i.Key)).ToArray()).ToArray();
        pack.DockingBayContents = entity.DockingBays.Select(b => b.Cargo.Select(i => (i.Value, i.Key)).ToArray()).ToArray();
        pack.Armor = entity.Armor;
        pack.Temperature = entity.Temperature;
        pack.Conductivity = entity.HullConductivity;
        var hullData = entity.ItemManager.GetData(entity.Hull) as HullData;
        pack.HardpointArmor = entity.HardpointArmor.ToDictionary(x => hullData.Hardpoints.IndexOf(x.Key), x => x.Value);
        pack.Children = entity.Children.Select(Pack).ToArray();
        return pack;
    }

    public static Ship Unpack(ItemManager itemManager, Zone zone, EntityPack pack, bool instantiate = false)
    {
        var entity = new Ship(itemManager, zone, instantiate ? (EquippableItem) itemManager.Instantiate(pack.Hull) : pack.Hull);
        Restore(itemManager, zone, pack, entity, instantiate);
        return entity;
    }

    public static OrbitalEntity Unpack(ItemManager itemManager, Zone zone, EntityPack pack, Guid orbit, bool instantiate = false)
    {
        var entity = new OrbitalEntity(itemManager, zone, instantiate ? (EquippableItem) itemManager.Instantiate(pack.Hull) : pack.Hull, orbit);
        Restore(itemManager, zone, pack, entity, instantiate);
        return entity;
    }

    public static void Restore(ItemManager itemManager, Zone zone, EntityPack pack, Entity entity, bool instantiate = false)
    {
        entity.Name = pack.Name;
        entity.Children = pack.Children.Select(c =>
        {
            var child = Unpack(itemManager, zone, c, instantiate);
            child.Parent = entity;
            return (Entity) child;
        }).ToList();
        foreach (var (position, item) in pack.Equipment) entity.TryEquip(instantiate ? (EquippableItem) itemManager.Instantiate(item) : item, position);
        foreach (var (position, item) in pack.CargoBays) entity.TryEquip(instantiate ? (EquippableItem) itemManager.Instantiate(item) : item, position);
        foreach (var (position, item) in pack.DockingBays) entity.TryEquip(instantiate ? (EquippableItem) itemManager.Instantiate(item) : item, position);

        for (var i = 0; i < pack.DockingBayAssignments.Length; i++)
        {
            if (pack.DockingBayAssignments[i] != -1)
                entity.DockingBays[i].DockedShip = entity.Children[pack.DockingBayAssignments[i]] as Ship;
        }

        for (var bayIndex = 0; bayIndex < pack.CargoContents.Length; bayIndex++)
            foreach (var (position, item) in pack.CargoContents[bayIndex])
                entity.CargoBays[bayIndex].TryStore(instantiate ? itemManager.Instantiate(item) : item, position);

        for (var bayIndex = 0; bayIndex < pack.DockingBayContents.Length; bayIndex++)
            foreach (var (position, item) in pack.DockingBayContents[bayIndex])
                entity.DockingBays[bayIndex].TryStore(instantiate ? itemManager.Instantiate(item) : item, position);

        // Iterate only over the behaviors of items which contain persistent data
        // Filter the behaviors for each item to get the persistent ones, then cast them and combine with the persisted data array for that item
        foreach (var persistentBehaviorData in entity.Equipment
            .Where(item => pack.PersistedBehaviors.ContainsKey(item.Position))
            .SelectMany(item => item.Behaviors
                .Where(b=> b is IPersistentBehavior)
                .Cast<IPersistentBehavior>()
                .Zip(pack.PersistedBehaviors[item.Position], (behavior, data) => new{behavior, data})))
            persistentBehaviorData.behavior.Restore(persistentBehaviorData.data);

        entity.Temperature = pack.Temperature;
        entity.Armor = pack.Armor;
        var hullData = entity.ItemManager.GetData(entity.Hull) as HullData;
        entity.HullConductivity = pack.Conductivity;
        entity.HardpointArmor = pack.HardpointArmor.ToDictionary(x => hullData.Hardpoints[x.Key], x => x.Value);
    }

    public int Price(ItemManager itemManager)
    {
        if (_price == 0)
        {
            var hullData = itemManager.GetData(Hull);
            _price = hullData.Price;

            foreach (var (_, item) in Equipment)
            {
                var itemData = itemManager.GetData(item);
                _price += itemData.Price;
            }
            foreach (var (_, item) in CargoBays)
            {
                var itemData = itemManager.GetData(item);
                _price += itemData.Price;
            }
            foreach (var (_, item) in DockingBays)
            {
                var itemData = itemManager.GetData(item);
                _price += itemData.Price;
            }

            for (var bayIndex = 0; bayIndex < CargoContents.Length; bayIndex++)
                foreach (var (_, item) in CargoContents[bayIndex])
                {
                    var itemData = itemManager.GetData(item);
                    if (item is SimpleCommodity s)
                        _price += itemData.Price * s.Quantity;
                    else
                        _price += itemData.Price;
                }

            for (var bayIndex = 0; bayIndex < DockingBayContents.Length; bayIndex++)
                foreach (var (_, item) in DockingBayContents[bayIndex])
                {
                    var itemData = itemManager.GetData(item);
                    if (item is SimpleCommodity s)
                        _price += itemData.Price * s.Quantity;
                    else
                        _price += itemData.Price;
                }
            
        }

        return _price;
    }
}
