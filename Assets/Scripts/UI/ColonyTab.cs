using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ColonyTab : MonoBehaviour
{
    public TextMeshProUGUI Title;
    public PropertiesPanel General;
    public PropertiesPanel Inventory;
    public PropertiesPanel Details;
    public ContextMenu ContextMenu;

    [HideInInspector]
    public GameContext Context
    {
        get => _context;
        set
        {
            _context = value;
            General.Context = value;
            Inventory.Context = value;
            Details.Context = value;
        }
    }

    private Guid _selectedColony;
    private GameContext _context;
    private Hardpoint _selectedHardpoint;

    public void Open(Guid colony)
    {
        _selectedColony = colony;
        _selectedHardpoint = null;

        var entity = Context.Cache.Get<Entity>(_selectedColony);
        
        Title.text = entity.Name;

        UpdateGeneral(entity);
        
        entity.OnInventoryUpdate += () => UpdateInventory(entity);
        UpdateInventory(entity);
    }

    private void OnDisable()
    {
        var entity = Context.Cache.Get<Entity>(_selectedColony);
        entity.ClearInventoryListeners();
    }

    public void UpdateGeneral(Entity entity)
    {
        General.Clear();
        
        General.AddProperty("Capacity", () => $"{entity.OccupiedCapacity}/{entity.Capacity:0}");
        General.AddProperty("Mass", () => $"{entity.Mass.SignificantDigits(Context.GlobalData.SignificantDigits)}");
        General.AddProperty("Temperature", () => $"{entity.Temperature:0}°K");
        General.AddProperty("Energy", () => $"{entity.Energy:0}/{entity.GetBehaviors<Reactor>().First().Capacitance:0}");
        General.AddProperty("Population", () => $"{entity.Population}");
        var personalityList = General.AddList("Personality");
        foreach (var attribute in entity.Personality.Keys)
            personalityList.AddPersonalityProperty(Context.Cache.Get<PersonalityAttribute>(attribute),
                () => entity.Personality[attribute]);
        
        General.RefreshValues();
    }

    public void UpdateInventory(Entity entity)
    {
        Inventory.Clear();
        
        var corporation = Context.Cache.Get<Corporation>(entity.Corporation);
        var blueprints = corporation.UnlockedBlueprints.Select(id => Context.Cache.Get<BlueprintData>(id));
        var hull = Context.Cache.Get<Gear>(entity.Hull);
        var hullData = Context.Cache.Get<HullData>(hull.Data);

        // Create item data mapping for cargo
        var cargoData = entity.Cargo.ToDictionary(id => _context.Cache.Get<ItemInstance>(id),
            id => Context.Cache.Get<ItemData>(_context.Cache.Get<ItemInstance>(id).Data));
        
        
        Inventory.AddSection("Gear");

        // Skip first hardpoint, that one is always the hull
        foreach (var hardpoint in entity.Hardpoints.Skip(1))
        {
            // These are the items in the inventory which can be equipped to the current hardpoint
            var matchingCargoGear = cargoData
                .Where(x => 
                    x.Value is GearData gearData &&
                    gearData.HardpointType == hardpoint.HardpointData.Type)
                .Select(x => x.Key as Gear)
                .ToArray();

            // Hardpoint has no gear assigned to it
            if (hardpoint.Gear == null)
            {
                // Check whether we're currently building some gear for this hardpoint
                var incompleteGear = entity.IncompleteGear.Keys
                    .Select(id => Context.Cache.Get<Gear>(id))
                    .FirstOrDefault(g => g.ItemData.HardpointType == hardpoint.HardpointData.Type);
                
                // We're building something for this hardpoint, show a progress bar!
                if (incompleteGear != null)
                {
                    Inventory.AddProgressField(incompleteGear.Name, () =>
                    {
                        var productionTime = _context.Cache.Get<BlueprintData>(incompleteGear.Blueprint).ProductionTime;
                        return (productionTime - (float) entity.IncompleteGear[incompleteGear.ID]) / productionTime;
                    });
                }
                else // We're not building anything
                {
                    // These are the blueprints we could build onto this hardpoint directly
                    var matchingBlueprints = blueprints.Where(bp =>
                        bp.FactoryItem == Guid.Empty &&
                        Context.Cache.Get<ItemData>(bp.Item) is GearData gearData &&
                        gearData.HardpointType == hardpoint.HardpointData.Type).ToArray();
                    
                    Action<PointerEventData> onClick = null;
                    if (matchingCargoGear.Any() || matchingBlueprints.Any())
                        onClick = data =>
                        {
                            if (data.button == PointerEventData.InputButton.Left)
                                PopulateDetails(entity, matchingBlueprints);
                            
                            else if (data.button == PointerEventData.InputButton.Right)
                            {
                                ContextMenu.gameObject.SetActive(true);
                                ContextMenu.Clear();
                                if (matchingCargoGear.Any())
                                    ContextMenu.AddDropdown("Install", matchingCargoGear
                                        .Select<Gear, (string, Action, bool)>(gear => (gear.Name, () => entity.Equip(gear, hardpoint), true)));
                                if (matchingBlueprints.Any())
                                    ContextMenu.AddDropdown("Build", matchingBlueprints
                                        .Select<BlueprintData, (string, Action, bool)>(blueprint =>
                                            (blueprint.Name, () => 
                                                    entity.Build(blueprint, 1, _context.Cache.Get<ItemData>(blueprint.Item).Name, true),
                                                entity.GetBlueprintIngredients(blueprint, out _, out _))));
                                ContextMenu.Show();
                            }
                        };
                    Inventory.AddProperty($"Empty {Enum.GetName(typeof(HardpointType), hardpoint.HardpointData.Type)} Hardpoint", null, onClick, matchingBlueprints.Any());
                }
            }
            else
            {
                var prop = Inventory.AddProperty(hardpoint.Gear.Name, null, _ =>
                {
                    _selectedHardpoint = hardpoint;
                    PopulateDetails(entity, hardpoint);
                }, true);
                    
                if (hardpoint == _selectedHardpoint) prop.Button.OnPointerClick(new PointerEventData(null){button = PointerEventData.InputButton.Left});
            }
        }
        
        if(!entity.Cargo.Any())
            Inventory.AddSection("No Cargo");
        else
        {
            Inventory.AddSection("Cargo");
            foreach (var x in cargoData)
            {
                if(x.Key is SimpleCommodity simpleCommodity)
                    Inventory.AddProperty(x.Value.Name, () => simpleCommodity.Quantity.ToString());
                else
                    Inventory.AddProperty(x.Value.Name);
            }
        }
        Inventory.RefreshValues();
    }
    
    void PopulateDetails(Entity entity, IEnumerable<BlueprintData> blueprints)
    {
        Details.Clear();
        foreach (var blueprint in blueprints)
        {
            var blueprintList = Details.AddList(blueprint.Name);
                                    
            foreach (var ingredient in blueprint.Ingredients)
            {
                var itemData = _context.Cache.Get<ItemData>(ingredient.Key);
                blueprintList.AddProperty(itemData.Name, () => ingredient.Value.ToString());
            }
                                    
            var bpItemData = _context.Cache.Get<ItemData>(blueprint.Item);

            if(entity.GetBlueprintIngredients(blueprint, out _, out _))
                blueprintList.AddButton("Build", _ => entity.Build(blueprint, 1, bpItemData.Name, true));
        }
        Details.RefreshValues();
    }
    
    void PopulateDetails(Entity entity, Hardpoint hardpoint)
    {
        Details.Clear();
        Details.AddProperty("Durability",
            () => $"{hardpoint.Gear.Durability.SignificantDigits(Context.GlobalData.SignificantDigits)}/{Context.Evaluate(hardpoint.ItemData.Durability, hardpoint.Gear).SignificantDigits(Context.GlobalData.SignificantDigits)}");
        foreach (var behavior in hardpoint.Behaviors)
        {
            if (behavior is Factory factory)
            {
                factory.OnProductionUpdate += () => PopulateDetails(entity, hardpoint);
                Details.AddField("Production Quality", () => factory.ProductionQuality, f => factory.ProductionQuality = f, 0, 1);
                var corporation = Context.Cache.Get<Corporation>(entity.Corporation);
                var compatibleBlueprints = corporation.UnlockedBlueprints
                    .Select(id => _context.Cache.Get<BlueprintData>(id))
                    .Where(bp => bp.FactoryItem == hardpoint.ItemData.ID).ToList();
                if (factory.RetoolingTime > 0)
                {
                    Details.AddProgressField("Retooling", () => (factory.ToolingTime - (float) factory.RetoolingTime) / factory.ToolingTime);
                }
                else
                {
                    Details.AddField("Item", 
                        () => compatibleBlueprints.FindIndex(bp=>bp.ID== factory.Blueprint) + 1, 
                        i => factory.Blueprint = i == 0 ? Guid.Empty : compatibleBlueprints[i - 1].ID,
                        new []{"None"}.Concat(compatibleBlueprints.Select(bp=>bp.Name)).ToArray());
                    if (factory.Blueprint != Guid.Empty)
                    {
                        if (factory.ItemUnderConstruction != Guid.Empty)
                        {
                            Details.AddProgressField("Production", () =>
                            {
                                var itemInstance = _context.Cache.Get<CraftedItemInstance>(factory.ItemUnderConstruction);
                                var blueprintData = _context.Cache.Get<BlueprintData>(itemInstance.Blueprint);
                                return (blueprintData.ProductionTime - (float) entity.IncompleteCargo[factory.ItemUnderConstruction]) / blueprintData.ProductionTime;
                            });
                        }
                        else
                        {
                            var ingredientsList = Details.AddList("Ingredients Needed");
                            var blueprintData = _context.Cache.Get<BlueprintData>(factory.Blueprint);
                            foreach (var ingredient in blueprintData.Ingredients)
                            {
                                var itemData = _context.Cache.Get<ItemData>(ingredient.Key);
                                ingredientsList.AddProperty(itemData.Name, () => ingredient.Value.ToString());
                            }
                        }
                    }
                }
            }
        }
        foreach (var behavior in hardpoint.ItemData.Behaviors)
        {
            var type = behavior.GetType();
            if (type.GetCustomAttribute(typeof(RuntimeInspectable)) != null)
            {
                foreach (var field in type.GetFields().Where(f => f.GetCustomAttribute<RuntimeInspectable>() != null))
                {
                    var fieldType = field.FieldType;
                    if (fieldType == typeof(float))
                        Details.AddProperty(field.Name, () => $"{((float) field.GetValue(behavior)).SignificantDigits(Context.GlobalData.SignificantDigits)}");
                    else if (fieldType == typeof(int))
                        Details.AddProperty(field.Name, () => $"{(int) field.GetValue(behavior)}");
                    else if (fieldType == typeof(PerformanceStat))
                    {
                        var stat = (PerformanceStat) field.GetValue(behavior);
                        Details.AddProperty(field.Name, () => $"{Context.Evaluate(stat, hardpoint.Gear, entity).SignificantDigits(Context.GlobalData.SignificantDigits)}");
                    }
                }
            }
        }
        Details.RefreshValues();
    }
}
