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
    private HardpointData _selectedHardpoint;

    public void Open(Guid colony)
    {
        _selectedColony = colony;
        _selectedHardpoint = null;

        var entity = Context.Cache.Get<Entity>(_selectedColony);
        entity.OnInventoryUpdate += () => PopulateScreen(entity);
        PopulateScreen(entity);
    }

    private void OnDisable()
    {
        var entity = Context.Cache.Get<Entity>(_selectedColony);
        entity.ClearInventoryListeners();
    }

    public void PopulateScreen(Entity entity){
        Title.text = entity.Name;
        
        General.Clear();
        Inventory.Clear();
        Details.Clear();

        var corporation = Context.Cache.Get<Corporation>(entity.Corporation);
        var blueprints = corporation.UnlockedBlueprints.Select(id => Context.Cache.Get<BlueprintData>(id));
        var hull = Context.Cache.Get<Gear>(entity.Hull);
        var hullData = Context.Cache.Get<HullData>(hull.Data);
        var capacity = Context.Evaluate(hullData.Capacity, hull, entity);

        General.AddProperty("Capacity", () => $"{entity.OccupiedCapacity}/{capacity:0}");
        General.AddProperty("Mass", () => $"{entity.Mass.SignificantDigits(Context.GlobalData.SignificantDigits)}");
        General.AddProperty("Temperature", () => $"{entity.Temperature:0}°K");
        General.AddProperty("Energy", () => $"{entity.Energy:0}/{entity.GetBehaviors<Reactor>().First().Capacitance:0}");
        General.AddProperty("Population", () => $"{entity.Population}");
        General.AddSection("Personality");
        foreach (var attribute in entity.Personality)
            General.AddPersonalityProperty(Context.Cache.Get<PersonalityAttribute>(attribute.Key),
                () => attribute.Value);
        General.RefreshValues();

        var cargoData = entity.Cargo.ToDictionary(id => _context.Cache.Get<ItemInstance>(id),
            id => Context.Cache.Get<ItemData>(_context.Cache.Get<ItemInstance>(id).Data));
        //var incompleteGear = entity.IncompleteGear.Keys.Select(id => _context.Cache.Get<Gear>(id));
        // var incompleteGear = entity.IncompleteGear.Select(x =>
        // {
        //     var gear = _context.Cache.Get<Gear>(x.Key);
        //     var productionTime = _context.Cache.Get<BlueprintData>(gear.Blueprint).ProductionTime;
        //     var progress = (productionTime - (float) x.Value) / productionTime;
        //     return (gear, progress);
        // });
        Inventory.AddSection("Gear");
        var gearList = new List<Gear>(entity.GearData.Keys.Concat(entity.IncompleteGear.Keys.Select(id => _context.Cache.Get<Gear>(id))));
        foreach (var hardpoint in hullData.Hardpoints)
        {
            var match = gearList.FirstOrDefault(g => g.ItemData.HardpointType == hardpoint.Type);
            var matchingCargoGear = cargoData
                .Where(x => x.Value is EquippableItemData equippableItemData &&
                            equippableItemData.HardpointType == hardpoint.Type).Select(x=>x.Key as Gear)
                .ToArray();
            if (match == null)
            {
                var matchingBlueprints = blueprints.Where(bp =>
                    bp.FactoryItem == Guid.Empty &&
                    Context.Cache.Get<ItemData>(bp.Item) is GearData gearData &&
                    gearData.HardpointType == hardpoint.Type).ToArray();
                Action<PointerEventData> onClick = null;
                if (matchingCargoGear.Any() || matchingBlueprints.Any())
                    onClick = data =>
                    {
                        if (data.button == PointerEventData.InputButton.Left)
                        {
                            Details.Clear();
                            foreach (var blueprint in matchingBlueprints)
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
                        else if (data.button == PointerEventData.InputButton.Right)
                        {
                            ContextMenu.gameObject.SetActive(true);
                            ContextMenu.Clear();
                            if (matchingCargoGear.Any())
                                ContextMenu.AddDropdown("Install", matchingCargoGear
                                    .Select<Gear, (string, Action, bool)>(gear => (gear.Name, () => entity.Equip(gear), true)));
                            if(matchingBlueprints.Any())
                                ContextMenu.AddDropdown("Build", matchingBlueprints
                                    .Select<BlueprintData, (string, Action, bool)>(blueprint => (blueprint.Name, () => entity.Build(blueprint, 1, _context.Cache.Get<ItemData>(blueprint.Item).Name, true), entity.GetBlueprintIngredients(blueprint, out _, out _))));
                            ContextMenu.Show();
                        }
                    };
                Inventory.AddProperty($"Empty {Enum.GetName(typeof(HardpointType), hardpoint.Type)} Hardpoint", null, onClick, matchingBlueprints.Any());
            }
            else
            {
                if (entity.IncompleteGear.ContainsKey(match.ID))
                {
                    Inventory.AddProgressField(match.Name, () =>
                    {
                        var productionTime = _context.Cache.Get<BlueprintData>(match.Blueprint).ProductionTime;
                        return (productionTime - (float) entity.IncompleteGear[match.ID]) / productionTime;
                    });
                }
                else
                {
                    var prop = Inventory.AddProperty(match.Name, null, _ =>
                    {
                        _selectedHardpoint = hardpoint;
                        PopulateDetails(match, entity);
                    }, true);
                    
                    if (hardpoint == _selectedHardpoint) prop.Button.OnPointerClick(new PointerEventData(null){button = PointerEventData.InputButton.Left});
                }
                

                gearList.Remove(match);
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
    
    void PopulateDetails(ItemInstance item, Entity entity)
    {
        Details.Clear();
        if (item is Gear gear)
        {
            var data = gear.ItemData;
            Details.AddProperty("Durability",
                () =>
                    $"{gear.Durability.SignificantDigits(Context.GlobalData.SignificantDigits)}/{Context.Evaluate(data.Durability, gear).SignificantDigits(Context.GlobalData.SignificantDigits)}");
            foreach (var behavior in entity.ItemBehaviors[gear])
            {
                if (behavior is Factory factory)
                {
                    factory.OnToolingUpdate += () => PopulateDetails(item, entity);
                    Details.AddField("Production Quality", () => factory.ProductionQuality, f => factory.ProductionQuality = f, 0, 1);
                    var corporation = Context.Cache.Get<Corporation>(entity.Corporation);
                    var compatibleBlueprints = corporation.UnlockedBlueprints
                        .Select(id => _context.Cache.Get<BlueprintData>(id))
                        .Where(bp => bp.FactoryItem == data.ID).ToList();
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
                            if (factory.ItemsUnderConstruction.Any())
                            {
                                Details.AddProgressField("Production", () =>
                                {
                                    var itemUnderConstruction = factory.ItemsUnderConstruction[0];
                                    var itemInstance = _context.Cache.Get<CraftedItemInstance>(itemUnderConstruction);
                                    var blueprintData = _context.Cache.Get<BlueprintData>(itemInstance.Blueprint);
                                    return (blueprintData.ProductionTime - (float) entity.IncompleteCargo[itemUnderConstruction]) / blueprintData.ProductionTime;
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
            foreach (var behavior in data.Behaviors)
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
                            Details.AddProperty(field.Name, () => $"{Context.Evaluate(stat, gear, entity).SignificantDigits(Context.GlobalData.SignificantDigits)}");
                        }
                    }
                }
            }
        }
        Details.RefreshValues();
    }
}
