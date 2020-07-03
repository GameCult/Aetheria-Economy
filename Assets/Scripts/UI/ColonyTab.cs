using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public class ColonyTab : MonoBehaviour
{
    public TextMeshProUGUI Title;
    public PropertiesPanel General;
    public PropertiesPanel Inventory;
    public PropertiesPanel ChildInventory;
    public PropertiesPanel Details;
    public ContextMenu ContextMenu;
    public ConfirmationDialog ConfirmationDialog;

    public GameContext Context
    {
        get => _context;
        set
        {
            _context = value;
            General.Context = value;
            Inventory.Context = value;
            ChildInventory.Context = value;
            Details.Context = value;
        }
    }

    private GameContext _context;
    private Hardpoint _selectedHardpoint;
    private Entity _parentEntity;
    private Entity _selectedChild;
    
    private IChangeSource _colonyGearChange;
    private IChangeSource _colonyCargoChange;
    private Action _onColonyInventoryChanged;
    
    private IChangeSource _childGearChange;
    private IChangeSource _childCargoChange;
    private Action _onChildInventoryChanged;

    public void Open(Guid colony)
    {
        _selectedHardpoint = null;

        _parentEntity = Context.Cache.Get<Entity>(colony);
        
        Title.text = _parentEntity.Name;

        UpdateGeneral(_parentEntity);
        
        UpdateInventory(_parentEntity, Inventory);
    }

    private void OnDisable()
    {
        if (_onColonyInventoryChanged == null)
            return;
        _colonyGearChange.OnChanged -= _onColonyInventoryChanged;
        _colonyCargoChange.OnChanged -= _onColonyInventoryChanged;
        Details.Clear();
        DeselectChild();
    }

    private void DeselectChild()
    {
        if (_onChildInventoryChanged == null)
            return;
        _childGearChange.OnChanged -= _onChildInventoryChanged;
        _childGearChange.OnChanged -= _onChildInventoryChanged;
        ChildInventory.Clear();
    }

    public void UpdateGeneral(Entity entity)
    {
        General.Clear();
        
        General.AddEntityProperties(entity);
        General.AddProperty("Assigned Population", () => $"{entity.AssignedPopulation}/{entity.Population}");
        
        var personalityList = General.AddList("Personality");
        foreach (var attribute in entity.Personality.Keys)
            personalityList.AddPersonalityProperty(Context.Cache.Get<PersonalityAttribute>(attribute),
                () => entity.Personality[attribute]);
        // personalityList.SetExpanded(false, true);

        var childList = General.AddList("Children");
        childList.InspectChildren(entity, child =>
        {
            _selectedChild = child;
            UpdateInventory(child, ChildInventory);
        });
        // childList.SetExpanded(false, true);
        
        General.RefreshValues();
    }

    public void UpdateInventory(Entity entity, PropertiesPanel panel)
    {
        if (panel == Inventory)
        {
            _colonyGearChange = entity.GearEvent;
            _colonyCargoChange = entity.CargoEvent;
            _onColonyInventoryChanged = () => RefreshInventory(entity, panel);
            _colonyGearChange.OnChanged += _onColonyInventoryChanged;
            _colonyCargoChange.OnChanged += _onColonyInventoryChanged;
            RefreshInventory(entity, panel);
        }
        else
        {
            DeselectChild();
            _childGearChange = entity.GearEvent;
            _childCargoChange = entity.CargoEvent;
            _onChildInventoryChanged = () => RefreshInventory(entity, panel, true);
            _childGearChange.OnChanged += _onChildInventoryChanged;
            _childCargoChange.OnChanged += _onChildInventoryChanged;
            RefreshInventory(entity, panel, true);
        }
        
    }
    
    void RefreshInventory(Entity entity, PropertiesPanel panel, bool child = false)
    {
        //Debug.Log("Refreshing Inventory!");
        var corporation = Context.Cache.Get<Corporation>(entity.Corporation);
        var blueprints = corporation.UnlockedBlueprints.Select(id => Context.Cache.Get<BlueprintData>(id));

        // Create item data mapping for cargo
        var cargoData = entity.Cargo.ToDictionary(id => _context.Cache.Get<ItemInstance>(id),
            id => Context.Cache.Get<ItemData>(_context.Cache.Get<ItemInstance>(id).Data));
        
        panel.Clear();

        if (child)
        {
            panel.Title.text = entity.Name;
            panel.AddField("Active", () => entity.Active, active => entity.Active = active);
            panel.AddSection("General");
            panel.AddEntityProperties(entity);
        }
        
        panel.AddSection("Hardpoints");
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
                    panel.AddProgressField(incompleteGear.Name, () =>
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
                    panel.AddProperty($"Empty {Enum.GetName(typeof(HardpointType), hardpoint.HardpointData.Type)} Hardpoint", null, onClick, matchingBlueprints.Any());
                }
            }
            else
            {
                var prop = panel.AddProperty(hardpoint.Gear.Name, null, data =>
                {
                    if (data.button == PointerEventData.InputButton.Left)
                    {
                        if (data.clickCount == 1)
                        {
                            _selectedHardpoint = hardpoint;
                            Details.Inspect(entity, hardpoint);
                        }

                        if (data.clickCount == 2)
                        {
                            entity.Unequip(hardpoint);
                        }
                    }
                }, true);
                prop.Button.DisableClickWhenSelected = false;

                if (hardpoint == _selectedHardpoint)
                {
                    panel.SelectedChild = prop.Button;
                    prop.Button.CurrentState = FlatButtonState.Selected;
                }
            }
        }
        
        if(!entity.Cargo.Any())
            panel.AddSection("No Cargo");
        else
        {
            Action<Guid, int> showHaulingDialog = (id, maxQuantity) =>
            {
                var itemData = Context.Cache.Get<ItemData>(id);
                ConfirmationDialog.gameObject.SetActive(true);
                ConfirmationDialog.Clear();
                ConfirmationDialog.Title.text = $"Haul {itemData.Name}";
                var quantity = maxQuantity;
                var colonies = Context.Cache.GetAll<Entity>()
                    .Where(e => e.Corporation == entity.Corporation && e is OrbitalEntity && e != entity).ToArray();
                var colonyNames = colonies.Select(c => c.Name).ToArray();
                ConfirmationDialog.AddField("Quantity", () => quantity, i => quantity = clamp(i, 1, maxQuantity));
                var selectedColony = 0;
                ConfirmationDialog.AddField("Target Colony", () => selectedColony, i => selectedColony = i,
                    colonyNames);
                ConfirmationDialog.Show(() =>
                {
                    var haulingTask = new HaulingTask
                    {
                        Context = Context,
                        ItemType = id,
                        Origin = entity.ID,
                        Quantity = quantity,
                        Target = colonies[selectedColony].ID,
                        Zone = entity.Zone
                    };
                    Context.Cache.Add(haulingTask);
                    var corp = Context.Cache.Get<Corporation>(entity.Corporation);
                    corp.Tasks.Add(haulingTask.ID);
                });
            };
            
            panel.AddSection("Cargo");
            foreach (var itemID in entity.Cargo.Where(id => Context.Cache.Get<ItemInstance>(id) is SimpleCommodity))
            {
                var simpleCommodity = Context.Cache.Get<SimpleCommodity>(itemID);
                var data = simpleCommodity.ItemData;
                panel.AddProperty(data.Name, () => simpleCommodity.Quantity.ToString(), eventData =>
                {
                    if (eventData.button == PointerEventData.InputButton.Left)
                    {
                        Details.Clear();
                        Details.RemoveListener();
                        Details.AddItemProperties(entity, simpleCommodity);
                        Details.RefreshValues();
                    }
                    else if (eventData.button == PointerEventData.InputButton.Right)
                    {
                        ContextMenu.gameObject.SetActive(true);
                        ContextMenu.Clear();
                        ContextMenu.AddOption("Haul", () => showHaulingDialog(simpleCommodity.Data, simpleCommodity.Quantity));
                        ContextMenu.Show();
                    }
                }, true).Button.DragSuccess = eventData =>
                {
                    Entity targetEntity = null;
                    if (panel == Inventory &&
                        _selectedChild != null &&
                        ((RectTransform) ChildInventory.transform).ContainsWorldPoint(eventData.position))
                        targetEntity = _selectedChild;
                    else if (panel == ChildInventory &&
                             ((RectTransform) Inventory.transform).ContainsWorldPoint(eventData.position))
                        targetEntity = _parentEntity;

                    if (targetEntity != null)
                    {
                        int maxQuantity = min((int) ((targetEntity.Capacity - targetEntity.OccupiedCapacity) / simpleCommodity.ItemData.Size), simpleCommodity.Quantity);
                        if (maxQuantity > 0)
                        {
                            ConfirmationDialog.gameObject.SetActive(true);
                            ConfirmationDialog.Clear();
                            ConfirmationDialog.Title.text = $"Move {simpleCommodity.ItemData.Name}";
                            var quantity = maxQuantity;
                            ConfirmationDialog.AddField("Quantity", () => quantity, i => quantity = clamp(i, 1, maxQuantity));
                            ConfirmationDialog.Show(() =>
                            {
                                var newItem = entity.RemoveCargo(simpleCommodity, quantity);
                                targetEntity.AddCargo(newItem);
                            });
                        }
                    }

                    return false;
                };
            }

            foreach (var group in entity.Cargo
                .Select(id => Context.Cache.Get<ItemInstance>(id))
                .Where(item => item is CraftedItemInstance)
                .Cast<CraftedItemInstance>()
                .GroupBy(craftedItem =>
                {
                    var corp = "GameCult";
                    if (craftedItem.SourceEntity != Guid.Empty)
                        corp = Context.Cache.Get<Corporation>(Context.Cache.Get<Entity>(craftedItem.SourceEntity).Corporation).Name;
                    return (craftedItem.Data, craftedItem.Name, corp);
                }))
            {
                Action<CraftedItemInstance, PointerEventData> onClick = (item, data) =>
                {
                    if (data.button == PointerEventData.InputButton.Left)
                    {
                        if (data.clickCount == 1)
                        {
                            Details.Clear();
                            Details.RemoveListener();
                            Details.AddItemProperties(entity, item);
                            Details.RefreshValues();
                        }
                        else if (data.clickCount == 2 && item is Gear gear)
                        {
                            // Item is a hull, create a ship entity and make it a child
                            if (gear.ItemData is HullData)
                            {
                                entity.RemoveCargo(gear);
                                var ship = Context.CreateShip(gear.ID, Enumerable.Empty<Guid>(),
                                    Enumerable.Empty<Guid>(), entity.Zone, entity.Corporation, _parentEntity.ID, gear.Name);
                                _context.SetParent(ship, entity);
                            }
                            else // Equip Item
                                entity.Equip(gear.ID, true);
                        }
                    }
                    else if (data.button == PointerEventData.InputButton.Right)
                    {
                        ContextMenu.gameObject.SetActive(true);
                        ContextMenu.Clear();
                        ContextMenu.AddOption("Haul", () => showHaulingDialog(item.Data, group.Count()));
                        ContextMenu.Show();
                    }
                };
                Func<PointerEventData, CraftedItemInstance, bool> dragSuccess = (data, item) =>
                {
                    Entity targetEntity = null;
                    if (panel == Inventory &&
                        _selectedChild != null &&
                        ((RectTransform) ChildInventory.transform).ContainsWorldPoint(data.position))
                        targetEntity = _selectedChild;
                    else if (panel == ChildInventory &&
                             ((RectTransform) Inventory.transform).ContainsWorldPoint(data.position))
                        targetEntity = _parentEntity;

                    if (targetEntity != null)
                    {
                        if (targetEntity.Capacity - targetEntity.OccupiedCapacity >
                            Context.Cache.Get<ItemData>(item.Data).Size)
                        {
                            entity.RemoveCargo(item);
                            targetEntity.AddCargo(item);
                            return true;
                        }
                    }

                    return false;
                };
                if (group.Count() == 1)
                {
                    var item = group.First();
                    var button = panel.AddProperty(item.Name, null, eventData => onClick(item, eventData), true).Button;
                    button.DisableClickWhenSelected = false;
                    button.DragSuccess = data => dragSuccess(data, item);
                }
                else
                {
                    var instanceList = panel.AddList($"{group.Count().ToString()} {group.Key.Name}");
                    foreach (var item in group)
                    {
                        var button = instanceList.AddProperty(item.Name, null, eventData => onClick(item, eventData), true).Button;
                        button.DisableClickWhenSelected = false;
                        button.DragSuccess = data => dragSuccess(data, item);
                    }
                    // instanceList.SetExpanded(false, true);
                }
            }
        }
        panel.RefreshValues();
    }
    
    void PopulateDetails(Entity entity, IEnumerable<BlueprintData> blueprints)
    {
        Details.RemoveListener();
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
            // blueprintList.SetExpanded(false, true);
        }
        Details.RefreshValues();
    }
}
