using System;
using System.Linq;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Random = Unity.Mathematics.Random;

public class LoadoutGenerator
{
    public Random Random;
    public ItemManager ItemManager { get; }
    public Sector Sector { get; }
    public SectorZone Zone { get; }
    public MegaCorporation Faction { get; }
    public float PriceExponent { get; }

    public LoadoutGenerator(
        ref Random random, 
        ItemManager itemManager, 
        Sector sector, 
        SectorZone zone, 
        MegaCorporation faction, 
        float priceExponent)
    {
        Random = random;
        ItemManager = itemManager;
        Sector = sector;
        Zone = zone;
        Faction = faction;
        PriceExponent = priceExponent;
    }
    
    public EntityPack GenerateShipLoadout()
    {
        var hull = ItemManager.CreateInstance(RandomHull(HullType.Ship)) as EquippableItem;
        var entity = new Ship(ItemManager, null, hull, ItemManager.GameplaySettings.DefaultEntitySettings);
        entity.Faction = Faction;
        OutfitEntity(entity);
        return EntitySerializer.Pack(entity);
    }

    public EntityPack GenerateTurretLoadout()
    {
        var hull = ItemManager.CreateInstance(RandomHull(HullType.Turret)) as EquippableItem;
        var entity = new OrbitalEntity(ItemManager, null, hull, Guid.Empty, ItemManager.GameplaySettings.DefaultEntitySettings);
        entity.Faction = Faction;
        OutfitEntity(entity);
        return EntitySerializer.Pack(entity);
    }

    public HullData RandomHull(HullType type)
    {
        return ItemManager.ItemData.GetAll<HullData>()
            .Where(item => item.HullType == type && (item.Manufacturer == Faction.ID || Faction.Allegiance.ContainsKey(item.Manufacturer)))
            .WeightedRandomElements(ref Random,
                item =>
                    (item.Manufacturer == Faction.ID ? 1 : Faction.Allegiance[item.Manufacturer]) / // Prioritize items from allied manufacturers
                    Zone.Distance[Sector.HomeZones[ItemManager.ItemData.Get<MegaCorporation>(item.Manufacturer)]] / // Penalize distance to manufacturer headquarters
                    pow(item.Price, PriceExponent), // Penalize item price to a controllable degree
                1
            ).FirstOrDefault();
    }
    
    public T RandomItem<T>(Predicate<T> filter = null) where T : EquippableItemData
    {
        return ItemManager.ItemData.GetAll<T>()
            .Where(item => Sector.ContainsFaction(item.Manufacturer) &&
                           (item.Manufacturer == Faction.ID || Faction.Allegiance.ContainsKey(item.Manufacturer)) &&
                           (filter?.Invoke(item) ?? true))
            .WeightedRandomElements(ref Random, item =>
                    (item.Manufacturer == Faction.ID ? 1 : Faction.Allegiance[item.Manufacturer]) * // Prioritize items from allied manufacturers
                    item.Shape.Coordinates.Length / // Prioritize items that fill more of the hardpoint's slots
                    Zone.Distance[Sector.HomeZones[ItemManager.ItemData.Get<MegaCorporation>(item.Manufacturer)]] / // Penalize distance to manufacturer headquarters
                    pow(item.Price, PriceExponent), // Penalize item price to a controllable degree
                1
            ).FirstOrDefault();
    }
    
    public T RandomItem<T>(HardpointData hardpoint, Predicate<T> filter = null) where T : EquippableItemData
    {
        return RandomItem<T>(item => item.HardpointType == hardpoint.Type &&
                                  (filter?.Invoke(item) ?? true) && 
                                  item.Shape.FitsWithin(hardpoint.Shape, hardpoint.Rotation, out _));
    }

    private void OutfitEntity(Entity entity)
    {
        var hullData = ItemManager.GetData(entity.Hull) as HullData;
        foreach (var hardpoint in hullData.Hardpoints)
        {
            if (hardpoint.Type == HardpointType.ControlModule)
            {
                var controllerData = RandomItem<GearData>(hardpoint,
                    item => item.Behaviors.Any(b => entity is Ship && b is CockpitData || entity is OrbitalEntity && b is TurretControllerData));
                if (controllerData == null) throw new InvalidLoadoutException("No compatible controller found for entity!");
                var controller = ItemManager.CreateInstance(controllerData) as EquippableItem;
                if (!entity.TryEquip(controller))
                {
                    throw new InvalidLoadoutException($"Failed to equip selected {Enum.GetName(typeof(HardpointType), hardpoint.Type)}!");
                }
            }
            else
            {
                var itemData = RandomItem<GearData>(hardpoint);
                if (itemData == null) ItemManager.Log($"No compatible item found for entity {Enum.GetName(typeof(HardpointType), hardpoint.Type)} hardpoint!");
                else
                {
                    //throw new InvalidLoadoutException($"No compatible item found for entity {Enum.GetName(typeof(HardpointType), hardpoint.Type)} hardpoint!");
                    var item = ItemManager.CreateInstance(itemData) as EquippableItem;
                    if (!entity.TryEquip(item))
                    {
                        throw new InvalidLoadoutException($"Failed to equip selected {Enum.GetName(typeof(HardpointType), hardpoint.Type)}!");
                    }
                }
            }
        }

        var emptyShape = new Shape(hullData.Shape.Width, hullData.Shape.Height);
        foreach (var v in hullData.Shape.Coordinates)
        {
            if (hullData.InteriorCells[v] && entity.Hardpoints[v.x, v.y] == null)
                emptyShape[v] = true;
        }

        var capacitorData = RandomItem<GearData>(item =>
            item.Behaviors.Any(b => b is CapacitorData) &&
            item.Shape.FitsWithin(emptyShape, out _, out _));
        if (capacitorData == null) throw new InvalidLoadoutException("No compatible capacitor found for entity!");

        if(!capacitorData.Shape.FitsWithin(emptyShape, out var capacitorRotation, out var capacitorPosition))
            throw new InvalidLoadoutException("Failed to equip selected capacitor!");
        var capacitor = ItemManager.CreateInstance(capacitorData) as EquippableItem;
        capacitor.Rotation = capacitorRotation;
        if (!entity.TryEquip(capacitor, capacitorPosition))
        {
            throw new InvalidLoadoutException("Failed to equip selected capacitor!");
        }
    }
}

public class InvalidLoadoutException : Exception
{
    public InvalidLoadoutException()
    {
    }

    public InvalidLoadoutException(string message)
        : base(message)
    {
    }

    public InvalidLoadoutException(string message, Exception inner)
        : base(message, inner)
    {
    }
}