using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Contexts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TradeMenu : MonoBehaviour
{
    public ActionGameManager GameManager;
    public ContextMenu ContextMenu;
    public Button NewFilterButton;
    public Prototype FilterPrototype;
    public SizeFilter MinimumSizeFilter;
    public SizeFilter MaximumSizeFilter;
    public PropertiesPanel Properties;
    public Spreadsheet Spreadsheet;
    public TextMeshProUGUI TargetCargoLabel;
    public Button FoldoutButton;

    private EquippedCargoBay _targetCargo;
    private (ItemFilter filter, HardpointType type) _hardpointFilter;
    private List<(ItemFilter filter, Type type)> _behaviorFilters = new List<(ItemFilter filter, Type type)>();
    
    private void OnEnable()
    {
        _targetCargo = GameManager.DockingBay;
        TargetCargoLabel.text = "Docking Bay";
        Properties.Context = GameManager.ItemManager;
        MinimumSizeFilter.Width.onEndEdit.AddListener(_ => Populate());
        MinimumSizeFilter.Height.onEndEdit.AddListener(_ => Populate());
        MaximumSizeFilter.Width.onEndEdit.AddListener(_ => Populate());
        MaximumSizeFilter.Height.onEndEdit.AddListener(_ => Populate());
        NewFilterButton.onClick.AddListener(() =>
        {
            ContextMenu.Clear();
            IEnumerable<HardpointType> hardpointTypes = (HardpointType[]) Enum.GetValues(typeof(HardpointType));
            if (_hardpointFilter.filter != null)
                hardpointTypes = hardpointTypes.Where(x => x != _hardpointFilter.type);
            ContextMenu.AddDropdown("Item Type", hardpointTypes
                .Select<HardpointType, (string, Action, bool)>(x => (Enum.GetName(typeof(HardpointType), x), () =>
                {
                    if (_hardpointFilter.filter == null)
                    {
                        _hardpointFilter.filter = FilterPrototype.Instantiate<ItemFilter>();
                        _hardpointFilter.filter.OnDisable += () =>
                        {
                            _hardpointFilter.filter = null;
                            Populate();
                        };
                    }

                    _hardpointFilter.filter.Label.text = $"Hardpoint: {Enum.GetName(typeof(HardpointType), x)}";
                    _hardpointFilter.type = x;
                    Populate();
                }, true)));
            ContextMenu.AddDropdown("Item Behavior", typeof(BehaviorData).GetAllChildClasses()
                .Where(x=> x.GetCustomAttribute<RuntimeInspectable>() != null && _behaviorFilters.All(f => f.type != x))
                .Select<Type, (string, Action, bool)>(x=> (x.Name.FormatTypeName(), () =>
                {
                    var matchingType = _behaviorFilters.FirstOrDefault(y => y.type.IsAssignableFrom(x) || x.IsAssignableFrom(y.type));
                    if (matchingType.filter != null) matchingType.filter.DisableButton.onClick.Invoke();
                    var filter = FilterPrototype.Instantiate<ItemFilter>();
                    filter.Label.text = x.Name.FormatTypeName();
                    filter.OnDisable += () =>
                    {
                        _behaviorFilters.Remove((filter, x));
                        Populate();
                    };
                    _behaviorFilters.Add((filter, x));
                    Populate();
                }, true)));
            if(!MinimumSizeFilter.gameObject.activeSelf)
                ContextMenu.AddOption("Minimum Size",
                    () =>
                    {
                        MinimumSizeFilter.gameObject.SetActive(true);
                        MinimumSizeFilter.OnDisable += () => Populate();
                    });
            if(!MaximumSizeFilter.gameObject.activeSelf)
                ContextMenu.AddOption("Maximum Size",
                    () =>
                    {
                        MaximumSizeFilter.gameObject.SetActive(true);
                        MaximumSizeFilter.OnDisable += () => Populate();
                    });
            ContextMenu.Show();
        });
        Populate();
    }

    void Populate()
    {
        var columns = new List<(string name, int size, Func<EquippableItemData, Func<string>> output, Func<EquippableItemData, IComparable> sortKey)>();
        
        columns.Add(("Name", 3,
            data => () => data.Name, 
            data => data.Name));
        if(_hardpointFilter.filter==null)
            columns.Add(("Type", 2,
                data => () => Enum.GetName(typeof(HardpointType), data.HardpointType), 
                data => data.HardpointType));
        columns.Add(("Mass", 1,
            data => () => data.Mass.SignificantDigits(3), 
            data => data.Mass));
        columns.Add(("Size", 1,
            data => () => $"{data.Shape.Width}x{data.Shape.Height}", 
            data => data.Shape.Width*data.Shape.Height));
        
        var items = GameManager.ItemManager.ItemData.GetAll<EquippableItemData>();
        
        if (MinimumSizeFilter.gameObject.activeSelf)
            items = items.Where(i =>
                !(MinimumSizeFilter.Width.text.Length > 0 && i.Shape.Width < int.Parse(MinimumSizeFilter.Width.text) ||
                 MinimumSizeFilter.Height.text.Length > 0 && i.Shape.Height < int.Parse(MinimumSizeFilter.Height.text)));
        
        if (MaximumSizeFilter.gameObject.activeSelf)
            items = items.Where(i =>
                !(MaximumSizeFilter.Width.text.Length > 0 && i.Shape.Width > int.Parse(MaximumSizeFilter.Width.text) ||
                 MaximumSizeFilter.Height.text.Length > 0 && i.Shape.Height > int.Parse(MaximumSizeFilter.Height.text)));
        
        if (_hardpointFilter.filter != null)
            items = items.Where(i => i.HardpointType == _hardpointFilter.type);
        
        foreach (var (_, type) in _behaviorFilters)
        {
            items = items.Where(i => i.Behaviors.Any(b => type.IsInstanceOfType(b)));
            
			foreach (var field in type.GetFields().Where(f => f.GetCustomAttribute<RuntimeInspectable>() != null))
			{
				var fieldType = field.FieldType;
				if (fieldType == typeof(float))
                    columns.Add((field.Name, 1, data =>
                    {
                        var behavior = data.Behaviors.FirstOrDefault(b => type.IsInstanceOfType(b));
                        return () => ((float) field.GetValue(behavior)).SignificantDigits(3);
                    }, data =>
                    {
                        var behavior = data.Behaviors.FirstOrDefault(b => type.IsInstanceOfType(b));
                        return (float) field.GetValue(behavior);
                    }));
				else if (fieldType == typeof(int))
                    columns.Add((field.Name, 1, data =>
                    {
                        var behavior = data.Behaviors.FirstOrDefault(b => type.IsInstanceOfType(b));
                        return () => ((int) field.GetValue(behavior)).ToString();
                    }, data =>
                    {
                        var behavior = data.Behaviors.FirstOrDefault(b => type.IsInstanceOfType(b));
                        return (int) field.GetValue(behavior);
                    }));
				else if (fieldType == typeof(PerformanceStat))
				{
                    columns.Add((field.Name, 1, data =>
                    {
                        var behavior = data.Behaviors.FirstOrDefault(b => type.IsInstanceOfType(b));
                        return () => ((PerformanceStat) field.GetValue(behavior)).Max.SignificantDigits(3);
                    }, data =>
                    {
                        var behavior = data.Behaviors.FirstOrDefault(b => type.IsInstanceOfType(b));
                        return ((PerformanceStat) field.GetValue(behavior)).Max;
                    }));
				}
			}
        }
        
        columns.Add(("Owned", 1,
            data => () => (_targetCargo.ItemsOfType.ContainsKey(data.ID) ? _targetCargo.ItemsOfType[data.ID].Count : 0).ToString(), 
            data => _targetCargo.ItemsOfType.ContainsKey(data.ID) ? _targetCargo.ItemsOfType[data.ID].Count : 0));
        
        Spreadsheet.ShowData(
            columns.Select(x => x.name).ToArray(),
            columns.Select(x => x.size).ToArray(),
            items.Select(i => new SpreadsheetEntryRow
            {
                Columns = columns.Select(x => new SpreadsheetEntryColumn
                {
                    Output = x.output(i),
                    SortKey = x.sortKey(i)
                }).ToArray(),
                OnClick = () =>
                {
                    Properties.Clear();
                    Properties.AddItemDataProperties(i);
                },
                OnDoubleClick = () =>
                {
                    _targetCargo.TryStore(GameManager.ItemManager.CreateInstance(i, .1f, 1));
                    Populate();
                }
            }));
    }

    void Start()
    {
        FoldoutButton.onClick.AddListener(() =>
        {
            ContextMenu.Clear();
            if(_targetCargo != GameManager.DockingBay)
                ContextMenu.AddOption("Docking Bay",
                    () =>
                    {
                        _targetCargo = GameManager.DockingBay;
                        TargetCargoLabel.text = "Docking Bay";
                    });
            foreach (var ship in GameManager.CurrentShip.Parent.Children.Where(GameManager.PlayerShips.Contains))
            {
                foreach (var bay in ship.CargoBays.Select((bay, index) => (bay, index)))
                    ContextMenu.AddOption($"{ship.Name} Bay {bay.index}",
                        () =>
                        {
                            _targetCargo = bay.bay;
                            TargetCargoLabel.text = $"{ship.Name} Bay {bay.index}";
                        });
            }
        });
    }

    void Update()
    {
        
    }
}
