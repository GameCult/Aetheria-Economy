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
    private IDynamicProperties HardpointChange;
    private IDynamicProperties CargoChange;
    private Action OnHardpointsChanged;
    private Action OnCargoChanged;

    public void Open(Guid colony)
    {
        _selectedColony = colony;
        _selectedHardpoint = null;

        var entity = Context.Cache.Get<Entity>(_selectedColony);
        
        Title.text = entity.Name;

        UpdateGeneral(entity);
        
        UpdateInventory(entity);
    }

    private void OnDisable()
    {
        HardpointChange.OnChanged -= OnHardpointsChanged;
        CargoChange.OnChanged -= OnCargoChanged;
        Details.Clear();
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
        personalityList.SetExpanded(false, true);
        
        General.RefreshValues();
    }

    public void UpdateInventory(Entity entity)
    {
        var corporation = Context.Cache.Get<Corporation>(entity.Corporation);
        var blueprints = corporation.UnlockedBlueprints.Select(id => Context.Cache.Get<BlueprintData>(id));
        var hull = Context.Cache.Get<Gear>(entity.Hull);
        var hullData = Context.Cache.Get<HullData>(hull.Data);
        
        OnHardpointsChanged = RefreshInventory;
        HardpointChange = entity.GearEvent;
        HardpointChange.OnChanged += OnHardpointsChanged;
        OnCargoChanged = RefreshInventory;
        CargoChange = entity.CargoEvent;
        CargoChange.OnChanged += OnCargoChanged;
        
        RefreshInventory();

        void RefreshInventory()
        {
            //Debug.Log("Refreshing Inventory!");

            // Create item data mapping for cargo
            var cargoData = entity.Cargo.ToDictionary(id => _context.Cache.Get<ItemInstance>(id),
                id => Context.Cache.Get<ItemData>(_context.Cache.Get<ItemInstance>(id).Data));
            
            Inventory.Clear();
            Inventory.AddSection("Hardpoints");
            var incompleteGearList = entity.IncompleteGear.Keys
                .Select(id => Context.Cache.Get<Gear>(id))
                .ToList();
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
                    var incompleteGear = incompleteGearList.FirstOrDefault(g => g.ItemData.HardpointType == hardpoint.HardpointData.Type);
                    
                    // We're building something for this hardpoint, show a progress bar!
                    if (incompleteGear != null)
                    {
                        incompleteGearList.Remove(incompleteGear);
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
                        Details.Inspect(entity, hardpoint);
                    }, true);

                    if (hardpoint == _selectedHardpoint)
                    {
                        Inventory.SelectedChild = prop.Button;
                        prop.Button.CurrentState = FlatButtonState.Selected;
                    }
                }
            }
            
            if(!entity.Cargo.Any())
                Inventory.AddSection("No Cargo");
            else
            {
                Inventory.AddSection("Cargo");
                foreach (var itemID in entity.Cargo.Where(id => Context.Cache.Get<ItemInstance>(id) is SimpleCommodity))
                {
                    var simpleCommodity = Context.Cache.Get<SimpleCommodity>(itemID);
                    var data = simpleCommodity.ItemData;
                    Inventory.AddProperty(data.Name, () => simpleCommodity.Quantity.ToString(), eventData =>
                    {
                        Details.Clear();
                        Details.AddItemProperties(simpleCommodity);
                        Details.RefreshValues();
                    });
                }

                foreach (var group in entity.Cargo
                    .Select(id => Context.Cache.Get<ItemInstance>(id))
                    .Where(item => item is CraftedItemInstance)
                    .Cast<CraftedItemInstance>()
                    .GroupBy(craftedItem => (craftedItem.Data, craftedItem.Name, Context.Cache.Get<Corporation>(Context.Cache.Get<Entity>(craftedItem.SourceEntity).Corporation))))
                {
                    if (group.Count() == 1)
                    {
                        var item = group.First();
                        Inventory.AddProperty(item.Name, null, eventData =>
                        {
                            Details.Clear();
                            Details.AddItemProperties(item);
                            Details.RefreshValues();
                        });
                    }
                    else
                    {
                        var instanceList = Inventory.AddList(group.Key.Name);
                        foreach (var item in group)
                        {
                            instanceList.AddProperty(item.Name, null, eventData =>
                            {
                                Details.Clear();
                                Details.AddItemProperties(item);
                                Details.RefreshValues();
                            });
                        }
                        instanceList.SetExpanded(false, true);
                    }
                }
            }
            Inventory.RefreshValues();
        }
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
                blueprintList.AddButton("Build", _ =>
                {
                    entity.Build(blueprint, 1, bpItemData.Name, true);
                    Details.Clear();
                });
            
            blueprintList.RefreshValues();
            blueprintList.SetExpanded(false, true);
        }
        Details.RefreshValues();
    }
}
