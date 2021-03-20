/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public class PropertiesPanel : MonoBehaviour
{
	public DropdownMenu Dropdown;
    public TextMeshProUGUI Title;
    public RectTransform Section;
    public PropertiesList List;
    public Property Property;
    public PropertyLabel PropertyLabel;
    public AttributeProperty Attribute;
    public InputField InputField;
    public RangedFloatField RangedFloatField;
    public RangedFloatField ProgressField;
    public EnumField EnumField;
    public BoolField BoolField;
    public PropertyButton PropertyButton;
    public ButtonField ButtonField;
    public IncrementField IncrementField;
    public RectTransform Content;
    [HideInInspector] public FlatFlatButton SelectedChild;
    [HideInInspector] public ItemManager Context;

    protected List<GameObject> Properties = new List<GameObject>();
    protected List<FlatFlatButton> Buttons = new List<FlatFlatButton>();
    protected event Action RefreshPropertyValues;
    protected bool RadioSelection = false;

    protected event Action<GameObject> OnPropertyAdded;
    protected Action OnPropertiesChanged;

    public int Children => Properties.Count + Buttons.Count;
    
    public void Awake()
    {
	    OnPropertyAdded += go => go.SetActive(true);
    }

    public virtual void Update()
    {
	    RefreshValues();
    }

    private void OnDestroy()
    {
    }

    // private void OnDisable()
    // {
	   //  RemoveListener();
    // }

    public void Clear()
    {
	    //RemoveListener();
        foreach(var property in Properties)
            Destroy(property);
        Title.text = "Properties";
        Properties.Clear();
        Buttons.Clear();
        SelectedChild = null;
        RefreshPropertyValues = null;
        RadioSelection = false;
    }

    public void Deselect()
    {
	    if (SelectedChild != null)
		    SelectedChild.CurrentState = FlatButtonState.Unselected;
	    SelectedChild = null;
    }

    public RectTransform AddSection(string name)
    {
        var section = Instantiate(Section, Content ?? transform);
        section.GetComponentInChildren<TextMeshProUGUI>().text = name;
        Properties.Add(section.gameObject);
        OnPropertyAdded?.Invoke(section.gameObject);
        return section;
    }

    public Property AddProperty(Func<string> read)
    {
	    if (read == null) throw new ArgumentException("Attempted to add property with null read function!");
	    
	    var property = Instantiate(Property, Content ?? transform);
	    property.Label.text = read();

		RefreshPropertyValues += () => property.Label.text = read();
	    Properties.Add(property.gameObject);
	    OnPropertyAdded?.Invoke(property.gameObject);
	    return property;
    }

    public Property AddProperty(string name, Func<string> read = null)
    {
	    Property property;
	    if(read != null)
			property = Instantiate(PropertyLabel, Content ?? transform);
	    else
		    property = Instantiate(Property, Content ?? transform);
        property.Label.text = name;

        if (read != null)
	        RefreshPropertyValues += () => ((PropertyLabel) property).Value.text = read.Invoke();
        Properties.Add(property.gameObject);
        OnPropertyAdded?.Invoke(property.gameObject);
        return property;
    }

    public PropertiesList AddList(string name) //, IEnumerable<(string, Func<string>)> elements)
    {
        var list = Instantiate(List, Content ?? transform);
        list.Context = Context;
        list.Dropdown = Dropdown;
        list.Title.text = name;
        // foreach (var element in elements)
        // {
        //     var item = Instantiate(PropertyPrefab, list);
        //     item.Name.text = element.Item1;
        //     item.Value.text = element.Item2();
        //     item.ValueFunction = element.Item2;
        //     Properties.Add(item.gameObject);
        // }
        //RefreshPropertyValues += () => list.RefreshValues();
        Properties.Add(list.gameObject);
        OnPropertyAdded?.Invoke(list.gameObject);
        return list;
    }

    public AttributeProperty AddPersonalityProperty(PersonalityAttribute attribute, Func<float> read)
    {
        var attributeInstance = Instantiate(Attribute, Content ?? transform);
        attributeInstance.Title.text = attribute.Name;
        attributeInstance.HighLabel.text = attribute.HighName;
        attributeInstance.LowLabel.text = attribute.LowName;
        RefreshPropertyValues += () => attributeInstance.Slider.value = read();
        Properties.Add(attributeInstance.gameObject);
        OnPropertyAdded?.Invoke(attributeInstance.gameObject);
        return attributeInstance;
    }

    public virtual PropertyButton AddButton(string name, Action onClick)
    {
	    var button = Instantiate(PropertyButton, Content ?? transform);
	    button.Label.text = name;
	    button.Button.interactable = onClick != null;
	    button.Button.onClick.AddListener(() => onClick());
	    Properties.Add(button.gameObject);
	    OnPropertyAdded?.Invoke(button.gameObject);
	    return button;
    }

    public void AddButton(string name, string label, Action onClick)
    {
	    var button = Instantiate(ButtonField, Content ?? transform);
	    button.Label.text = name;
	    button.ButtonLabel.text = label;
	    button.Button.interactable = onClick != null;
	    button.Button.onClick.AddListener(() => onClick());
	    Properties.Add(button.gameObject);
	    OnPropertyAdded?.Invoke(button.gameObject);
    }
	
	public void AddField(string name, Func<string> read, Action<string> write)
	{
		var field = Instantiate(InputField, Content ?? transform);
		field.Label.text = name;
		field.Field.contentType = TMP_InputField.ContentType.Standard;
		field.Field.onValueChanged.AddListener(val => write(val));
		// field.Field.text = read();
		RefreshPropertyValues += () =>
		{
			var s = read();
			if (field.Field.text != s)
			{
				field.Field.text = s;
				field.gameObject.SetActive(false);
				Observable.NextFrame().Subscribe(_ =>
				{
					if(field != null)
						OnPropertyAdded?.Invoke(field.gameObject);
				});
			}
		};
		Properties.Add(field.gameObject);
		OnPropertyAdded?.Invoke(field.gameObject);
	}

	public void AddField(string name, Func<float> read, Action<float> write)
	{
		var field = Instantiate(InputField, Content ?? transform);
		field.Label.text = name;
		field.Field.contentType = TMP_InputField.ContentType.DecimalNumber;
		field.Field.onValueChanged.AddListener(val => write(float.Parse(val)));
		RefreshPropertyValues += () =>
		{
			var s = read().ToString(CultureInfo.InvariantCulture);
			if (field.Field.text != s)
			{
				field.Field.text = s;
				field.gameObject.SetActive(false);
				Observable.NextFrame().Subscribe(_ => OnPropertyAdded?.Invoke(field.gameObject));
			}
		};
		Properties.Add(field.gameObject);
		OnPropertyAdded?.Invoke(field.gameObject);
	}
	
	public void AddField(string name, Func<int> read, Action<int> write)
	{
		var field = Instantiate(InputField, Content ?? transform);
		field.Label.text = name;
		field.Field.contentType = TMP_InputField.ContentType.IntegerNumber;
		field.Field.onValueChanged.AddListener(val => write(int.Parse(val)));
		RefreshPropertyValues += () =>
		{
			var s = read().ToString(CultureInfo.InvariantCulture);
			if (field.Field.text != s)
			{
				field.Field.text = s;
				field.gameObject.SetActive(false);
				Observable.NextFrame().Subscribe(_ => OnPropertyAdded?.Invoke(field.gameObject));
			}
		};
		Properties.Add(field.gameObject);
		OnPropertyAdded?.Invoke(field.gameObject);
	}
	
	public void AddIncrementField(string name, Func<int> read, Action<int> write, Func<int> min, Func<int> max)
	{
		var field = Instantiate(IncrementField, Content ?? transform);
		field.Label.text = name;
		field.Increment.onClick.AddListener(() => write(read() + 1));
		field.Decrement.onClick.AddListener(() => write(read() - 1));
		RefreshPropertyValues += () =>
		{
			var val = read();
			var minval = min();
			var maxval = max();
			if (val < minval || val > maxval)
				write(val = clamp(val, minval, maxval));
			field.Value.text = val.ToString(CultureInfo.InvariantCulture);
			field.Increment.interactable = val == maxval;
			field.Decrement.interactable = val == minval;
		};
		Properties.Add(field.gameObject);
		OnPropertyAdded?.Invoke(field.gameObject);
	}
	
	public void AddField(string name, Func<float> read, Action<float> write, float min, float max)
	{
		var field = Instantiate(RangedFloatField, Content ?? transform);
		field.Label.text = name;
		field.Slider.wholeNumbers = false;
		field.Slider.minValue = min;
		field.Slider.maxValue = max;
		field.Slider.onValueChanged.AddListener(val => write(val));
		RefreshPropertyValues += () => field.Slider.value = read();
		Properties.Add(field.gameObject);
		OnPropertyAdded?.Invoke(field.gameObject);
	}
	
	public void AddProgressField(string name, Func<float> read)
	{
		var field = Instantiate(ProgressField, Content ?? transform);
		field.Label.text = name;
		field.Slider.wholeNumbers = false;
		field.Slider.minValue = 0;
		field.Slider.maxValue = 1;
		//field.Slider.onValueChanged.AddListener(val => write(val));
		RefreshPropertyValues += () => field.Slider.value = read();
		Properties.Add(field.gameObject);
		OnPropertyAdded?.Invoke(field.gameObject);
	}
	
	public void AddField(string name, Func<int> read, Action<int> write, int min, int max)
	{
		var field = Instantiate(RangedFloatField, Content ?? transform);
		field.Label.text = name;
		field.Slider.wholeNumbers = true;
		field.Slider.minValue = min;
		field.Slider.maxValue = max;
		field.Slider.onValueChanged.AddListener(val => write(Mathf.RoundToInt(val)));
		RefreshPropertyValues += () => field.Slider.value = read();
		Properties.Add(field.gameObject);
		OnPropertyAdded?.Invoke(field.gameObject);
	}
	
	public void AddField(string name, Func<bool> read, Action<bool> write)
	{
		var field = Instantiate(BoolField, Content ?? transform);
		field.Label.text = name;
		field.Toggle.onValueChanged.AddListener(val => write(val));
		RefreshPropertyValues += () => field.Toggle.isOn = read();
		Properties.Add(field.gameObject);
		OnPropertyAdded?.Invoke(field.gameObject);
	}
	
	public void AddField(string name, Func<int> read, Action<int> write, string[] enumOptions)
	{
		var field = Instantiate(EnumField, Content ?? transform);
		field.Label.text = name;
		field.Dropdown.onClick.AddListener(() =>
		{
			var selected = read();
			Dropdown.gameObject.SetActive(true);
			Dropdown.Clear();
			for (int i = 0; i < enumOptions.Length; i++)
			{
				var index = i;
				Dropdown.AddOption(enumOptions[i], () => write(index), index == selected);
			}

			Dropdown.Show((RectTransform) field.Dropdown.transform);
		});
		RefreshPropertyValues += () => field.DropdownLabel.text = enumOptions[read()];
		Properties.Add(field.gameObject);
		OnPropertyAdded?.Invoke(field.gameObject);
	}
	
	public void Inspect(Entity entity)
	{
        Clear();
        Title.text = entity.Name;
        var hullData = Context.GetData(entity.Hull) as HullData;
        AddSection(
            hullData.HullType == HullType.Ship ? "Ship" :
            hullData.HullType == HullType.Station ? "Station" :
            "Platform");
        //AddList(hullData.Name).Inspect(hull, entity);
        //PropertiesPanel.AddProperty("Hull", () => $"{hullData.Name}");
        AddEntityProperties(entity);
        //cargoList.SetExpanded(false,true);
        
        RefreshValues();
	}

	public void AddEntityProperties(Entity entity)
	{
		AddField("Name", () => entity.Name, name => entity.Name = name);
		AddProperty("Mass", () => $"{entity.Mass.SignificantDigits(Context.GameplaySettings.SignificantDigits)}");
	}

	public void AddItemProperties(Entity entity, ItemInstance item)
	{
		var data = Context.ItemData.Get<ItemData>(item.Data);
		AddProperty(data.Description);//.Label.fontStyle = FontStyles.Normal;
		var manufacturer = Context.ItemData.Get<Faction>(data.Manufacturer);
		if (manufacturer != null)
		{
			AddProperty("Manufacturer", () => manufacturer.Name);
		}
		else
		{
			AddProperty("Manufacturer", () => "GameCult");
		}
		if (item is SimpleCommodity simpleCommodity)
			AddProperty("Quantity", () => simpleCommodity.Quantity.ToString());
		AddProperty("Mass", () => Context.GetMass(item).SignificantDigits(Context.GameplaySettings.SignificantDigits));
		AddProperty("Thermal Mass", () => Context.GetThermalMass(item).SignificantDigits(Context.GameplaySettings.SignificantDigits));
		if (item is EquippableItem gear)
		{
			var tier = entity.ItemManager.GetTier(gear);
			Title.text =
				$"<color=#{ColorUtility.ToHtmlStringRGB(tier.tier.Color.ToColor())}>{gear.Name}</color><smallcaps><size=60%> ({tier.tier.Name}{new string('+', tier.upgrades)})";
			var gearData = Context.GetData(gear);
			AddProperty("Durability", () =>
				$"{gear.Durability.SignificantDigits(Context.GameplaySettings.SignificantDigits)}/{gearData.Durability.SignificantDigits(Context.GameplaySettings.SignificantDigits)}");
			foreach (var behavior in gearData.Behaviors)
			{
				var type = behavior.GetType();
				if (type.GetCustomAttribute(typeof(RuntimeInspectable)) != null)
				{
					foreach (var field in type.GetFields().Where(f => f.GetCustomAttribute<RuntimeInspectable>() != null))
					{
						var fieldType = field.FieldType;
						if (fieldType == typeof(float))
							AddProperty(field.Name, () => $"{((float) field.GetValue(behavior)).SignificantDigits(Context.GameplaySettings.SignificantDigits)}");
						else if (fieldType == typeof(int))
							AddProperty(field.Name, () => $"{(int) field.GetValue(behavior)}");
						else if (fieldType == typeof(PerformanceStat))
						{
							var stat = (PerformanceStat) field.GetValue(behavior);
							AddProperty(field.Name, () => $"{Context.Evaluate(stat, gear).SignificantDigits(Context.GameplaySettings.SignificantDigits)}");
						}
					}
				}
			}
		}
	}

	public void AddItemDataProperties(ItemData data)
	{
		AddProperty("Type", () => data.Name);
		AddProperty(data.Description).Label.fontStyle = FontStyles.Normal;
		if (data is EquippableItemData gearData)
		{
			AddProperty("Durability", () => gearData.Durability.SignificantDigits(Context.GameplaySettings.SignificantDigits));
			foreach (var behavior in gearData.Behaviors)
			{
				if (behavior is StatModifierData statMod)
				{
					if(Math.Abs(statMod.Modifier.Min - statMod.Modifier.Max) < .001f)
						AddProperty("Stat Mod", () => $"{statMod.Stat.Target}:{statMod.Stat.Stat}{(statMod.Type == StatModifierType.Constant ? "+" : "x")}{statMod.Modifier.Min.SignificantDigits(Context.GameplaySettings.SignificantDigits)}");
					else
						AddProperty("Stat Mod", () => $"{statMod.Stat.Target}:{statMod.Stat.Stat}{(statMod.Type == StatModifierType.Constant ? "+" : "x")}{statMod.Modifier.Min.SignificantDigits(Context.GameplaySettings.SignificantDigits)}-{statMod.Modifier.Max.SignificantDigits(Context.GameplaySettings.SignificantDigits)}");
				}
				var type = behavior.GetType();
				if (type.GetCustomAttribute(typeof(RuntimeInspectable)) != null)
				{
					foreach (var field in type.GetFields().Where(f => f.GetCustomAttribute<RuntimeInspectable>() != null))
					{
						var fieldType = field.FieldType;
						if (fieldType == typeof(float))
							AddProperty(field.Name, () => $"{((float) field.GetValue(behavior)).SignificantDigits(Context.GameplaySettings.SignificantDigits)}");
						else if (fieldType == typeof(int))
							AddProperty(field.Name, () => $"{(int) field.GetValue(behavior)}");
						else if (fieldType == typeof(PerformanceStat))
						{
							var stat = (PerformanceStat) field.GetValue(behavior);
							if(Math.Abs(stat.Min - stat.Max) < .001f)
								AddProperty(field.Name, () => $"{stat.Min.SignificantDigits(Context.GameplaySettings.SignificantDigits)}");
							else
								AddProperty(field.Name, () => $"{stat.Min.SignificantDigits(Context.GameplaySettings.SignificantDigits)}-{stat.Max.SignificantDigits(Context.GameplaySettings.SignificantDigits)}");
						}
					}
				}
			}
		}
	}

	public void AddItemProperties(ItemInstance item)
	{
		var data = Context.ItemData.Get<ItemData>(item.Data);
		AddItemDataProperties(data);
		AddProperty("Mass", () => Context.GetMass(item).SignificantDigits(Context.GameplaySettings.SignificantDigits));
		AddProperty("Thermal Mass", () => Context.GetThermalMass(item).SignificantDigits(Context.GameplaySettings.SignificantDigits));
		if (item is EquippableItem gear)
		{
			var gearData = Context.GetData(gear);
			AddProperty("Durability", () =>
				$"{gear.Durability.SignificantDigits(Context.GameplaySettings.SignificantDigits)}/{gearData.Durability.SignificantDigits(Context.GameplaySettings.SignificantDigits)}");

		}
	}

	public void Inspect(Entity entity, EquippedItem item)
	{
		if (this is PropertiesList list)
		{
			if (list.Expanded)
				InspectGearInternal();
			else
				list.OnExpand += b =>
				{
					if (b) InspectGearInternal();
				};
		}
		else
			InspectGearInternal();

		void InspectGearInternal()
		{
			//Debug.Log($"Refreshing {hardpoint.Gear.Name} properties");
			Clear();
			if (item.EquippableItem != null)
			{
				AddField("Override Shutdown", () => item.EquippableItem.OverrideShutdown, b => item.EquippableItem.OverrideShutdown = b);
				AddProperty("Temperature", () => (item.Temperature - 273.15f).SignificantDigits(Context.GameplaySettings.SignificantDigits));
				AddItemProperties(entity, item.EquippableItem);
		        foreach (var behavior in item.Behaviors)
		        {
			        if(behavior is IPopulationAssignment populationAssignment)
				        AddIncrementField("Assigned Population", 
					        () => populationAssignment.AssignedPopulation, 
					        p => populationAssignment.AssignedPopulation = p,
					        () => 0, () => entity.Population - entity.AssignedPopulation + populationAssignment.AssignedPopulation);
			        if (behavior is Thermotoggle thermotoggle)
				        AddField("Target Temperature", () => thermotoggle.TargetTemperature, temp => thermotoggle.TargetTemperature = temp);
		        }
			}
			RefreshValues();
		}
	}

	// public void Inspect(object obj, bool inspectablesOnly = false, bool readWrite = false, bool topLevel = true)
	// {
	// 	if(topLevel)
	// 		Clear();
	//
	// 	var fields = obj.GetType().GetFields();
	// 	foreach (var field in fields)
	// 		Inspect(obj, field, inspectablesOnly, readWrite);
	// 	
	// 	if(topLevel)
	// 		foreach (var field in _propertyFields) 
	// 			field.transform.SetSiblingIndex(_propertyFields.IndexOf(field));
	//
	// 	RefreshValues();
	// }
	//
	// public void Inspect(object obj, FieldInfo field, bool inspectablesOnly = false, bool readWrite = false)
	// {
	// 	var inspectable = field.GetCustomAttribute<InspectableFieldAttribute>();
	// 	if (inspectable == null && inspectablesOnly) return;
	// 	var type = field.FieldType;
	// 	
	// 	if (type == typeof(float))
	// 	{
	// 		if (readWrite)
	// 		{
	// 			var ranged = inspectable as RangedFloatInspectableAttribute;
	// 			if (ranged != null)
	// 				Inspect(field.Name.SplitCamelCase(), () => (float) field.GetValue(obj), f => field.SetValue(obj, f),
	// 					ranged.Min, ranged.Max);
	// 			else
	// 				Inspect(field.Name.SplitCamelCase(), () => (float) field.GetValue(obj), f => field.SetValue(obj, f));
	// 		} else Inspect(field.Name.SplitCamelCase(), () => ((float) field.GetValue(obj)).ToString("0.##"));
	// 	} 
	// 	else if (type == typeof(int))
	// 	{
	// 		if (readWrite)
	// 		{
	// 			var ranged = inspectable as RangedIntInspectableAttribute;
	// 			if (ranged != null)
	// 				Inspect(field.Name.SplitCamelCase(), () => (int) field.GetValue(obj), f => field.SetValue(obj, f),
	// 					ranged.Min, ranged.Max);
	// 			else
	// 				Inspect(field.Name.SplitCamelCase(), () => (int) field.GetValue(obj), f => field.SetValue(obj, f));
	// 		} else Inspect(field.Name.SplitCamelCase(), () => ((int) field.GetValue(obj)).ToString());
	// 	}
	// 	else if (type.IsEnum)
	// 	{
	// 		if (readWrite)
	// 			Inspect(field.Name.SplitCamelCase(), () => (int) field.GetValue(obj), i => field.SetValue(obj, i),
	// 				Enum.GetNames(field.FieldType));
	// 		else
	// 			Inspect(field.Name.SplitCamelCase(), () => Enum.GetName(type, field.GetValue(obj)).SplitCamelCase());
	// 	}
	// 	else if (type == typeof(string))
	// 	{
	// 		if (readWrite)
	// 			Inspect(field.Name.SplitCamelCase(), () => (string) field.GetValue(obj), i => field.SetValue(obj, i));
	// 		else
	// 			Inspect(field.Name.SplitCamelCase(), () => (string) field.GetValue(obj));
	// 	}
	// 	//else if (field.FieldType == typeof(Color)) Inspect(field.Name, () => (Color) field.GetValue(obj), c => field.SetValue(obj, c));
	// 	else if (field.FieldType == typeof(bool)) Inspect(field.Name, () => (bool) field.GetValue(obj), b => field.SetValue(obj, b));
	// 	else if (type.GetCustomAttribute<InspectableFieldAttribute>() != null)
	// 	{
	// 		if(!_propertyFields.Any() || !_propertyFields.Last().gameObject.name.ToUpper().Contains("DIVIDER"))
	// 			_propertyFields.Add(Divider.Instantiate<Prototype>());
	// 		Inspect(field.GetValue(obj), inspectablesOnly, readWrite, false);
	// 	}
	// 	else 
	// 		Debug.Log($"Field \"{field.Name}\" has unknown type {field.FieldType.Name}. No inspector was generated.");
	// }

	public void RefreshValues()
	{
		RefreshPropertyValues?.Invoke();
	}
}
