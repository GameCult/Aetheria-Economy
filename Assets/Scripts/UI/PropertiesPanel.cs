using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class PropertiesPanel : MonoBehaviour
{
    public TextMeshProUGUI Title;
    public RectTransform SectionPrefab;
    public PropertiesList ListPrefab;
    public PropertyButton PropertyPrefab;
    public PropertyLabel PropertyLabelPrefab;
    public AttributeProperty AttributePrefab;
    public InputField InputField;
    public RangedFloatField RangedFloatField;
    public RangedFloatField ProgressField;
    public EnumField EnumField;
    public BoolField BoolField;
    public PropertyButton PropertyButton;
    public ButtonField ButtonField;
    [HideInInspector] public FlatFlatButton SelectedChild;
    [HideInInspector] public GameContext Context;

    protected List<GameObject> Properties = new List<GameObject>();
    protected List<FlatFlatButton> Buttons = new List<FlatFlatButton>();
    protected event Action RefreshPropertyValues;
    protected bool RadioSelection = false;

    protected IDynamicProperties ChangeSource;
    protected Action OnPropertiesChanged;

    protected void Update()
    {
	    RefreshValues();
    }

    private void OnDestroy()
    {
	    RemoveListener();
    }

    private void OnDisable()
    {
	    RemoveListener();
    }

    public void Clear()
    {
        foreach(var property in Properties)
            Destroy(property);
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
        var section = Instantiate(SectionPrefab, transform);
        section.GetComponentInChildren<TextMeshProUGUI>().text = name;
        Properties.Add(section.gameObject);
        return section;
    }

    public PropertyButton AddProperty(string name, Func<string> read = null, Action<PointerEventData> onClick = null, bool radio = false)
    {
	    PropertyButton property;
	    if(read != null)
			property = Instantiate(PropertyLabelPrefab, transform);
	    else
		    property = Instantiate(PropertyPrefab, transform);
        property.Label.text = name;
        if (radio)
        {
	        RadioSelection = true;
            Buttons.Add(property.Button);
            property.Button.OnClick += data =>
            {
	            if (data.button == PointerEventData.InputButton.Left)
	            {
		            if (SelectedChild != null)
			            SelectedChild.CurrentState = FlatButtonState.Unselected;
		            SelectedChild = property.Button;
		            SelectedChild.CurrentState = FlatButtonState.Selected;
	            }
                onClick?.Invoke(data);
            };
        }
        else if(onClick!=null) property.Button.OnClick += onClick;

        if (read != null)
	        RefreshPropertyValues += () => ((PropertyLabel) property).Value.text = read.Invoke();
        Properties.Add(property.gameObject);
        return property;
    }

    public PropertiesList AddList(string name) //, IEnumerable<(string, Func<string>)> elements)
    {
        var list = Instantiate(ListPrefab, transform);
        list.Context = Context;
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
        return list;
    }

    public AttributeProperty AddPersonalityProperty(PersonalityAttribute attribute, Func<float> read)
    {
        var attributeInstance = Instantiate(AttributePrefab, transform);
        attributeInstance.Title.text = attribute.Name;
        attributeInstance.HighLabel.text = attribute.HighName;
        attributeInstance.LowLabel.text = attribute.LowName;
        RefreshPropertyValues += () => attributeInstance.Slider.value = read();
        Properties.Add(attributeInstance.gameObject);
        return attributeInstance;
    }

    public virtual PropertyButton AddButton(string name, Action<PointerEventData> onClick)
    {
	    var button = Instantiate(PropertyButton, transform);
	    button.Label.text = name;
	    button.Button.OnClick += onClick;
	    Properties.Add(button.gameObject);
	    return button;
    }

    public void AddButton(string name, string label, Action<PointerEventData> onClick)
    {
	    var button = Instantiate(ButtonField, transform);
	    button.Label.text = name;
	    button.ButtonLabel.text = label;
	    button.Button.OnClick += onClick;
	    Properties.Add(button.gameObject);
    }
	
	public void AddField(string name, Func<string> read, Action<string> write)
	{
		var field = Instantiate(InputField, transform);
		field.Label.text = name;
		field.Field.contentType = TMP_InputField.ContentType.Standard;
		field.Field.onValueChanged.AddListener(val => write(val));
		RefreshPropertyValues += () => field.Field.text = read();
		Properties.Add(field.gameObject);
	}

	public void AddField(string name, Func<float> read, Action<float> write)
	{
		var field = Instantiate(InputField, transform);
		field.Label.text = name;
		field.Field.contentType = TMP_InputField.ContentType.DecimalNumber;
		field.Field.onValueChanged.AddListener(val => write(float.Parse(val)));
		RefreshPropertyValues += () => field.Field.text = read().ToString(CultureInfo.InvariantCulture);
		Properties.Add(field.gameObject);
	}
	
	public void AddField(string name, Func<int> read, Action<int> write)
	{
		var field = Instantiate(InputField, transform);
		field.Label.text = name;
		field.Field.contentType = TMP_InputField.ContentType.IntegerNumber;
		field.Field.onValueChanged.AddListener(val => write(int.Parse(val)));
		RefreshPropertyValues += () => field.Field.text = read().ToString(CultureInfo.InvariantCulture);
		Properties.Add(field.gameObject);
	}
	
	public void AddField(string name, Func<float> read, Action<float> write, float min, float max)
	{
		var field = Instantiate(RangedFloatField, transform);
		field.Label.text = name;
		field.Slider.wholeNumbers = false;
		field.Slider.minValue = min;
		field.Slider.maxValue = max;
		field.Slider.onValueChanged.AddListener(val => write(val));
		RefreshPropertyValues += () => field.Slider.value = read();
		Properties.Add(field.gameObject);
	}
	
	public void AddProgressField(string name, Func<float> read)
	{
		var field = Instantiate(ProgressField, transform);
		field.Label.text = name;
		field.Slider.wholeNumbers = false;
		field.Slider.minValue = 0;
		field.Slider.maxValue = 1;
		//field.Slider.onValueChanged.AddListener(val => write(val));
		RefreshPropertyValues += () => field.Slider.value = read();
		Properties.Add(field.gameObject);
	}
	
	public void AddField(string name, Func<int> read, Action<int> write, int min, int max)
	{
		var field = Instantiate(RangedFloatField, transform);
		field.Label.text = name;
		field.Slider.wholeNumbers = true;
		field.Slider.minValue = min;
		field.Slider.maxValue = max;
		field.Slider.onValueChanged.AddListener(val => write(Mathf.RoundToInt(val)));
		RefreshPropertyValues += () => field.Slider.value = read();
		Properties.Add(field.gameObject);
	}
	
	public void AddField(string name, Func<bool> read, Action<bool> write)
	{
		var field = Instantiate(BoolField, transform);
		field.Label.text = name;
		field.Toggle.onValueChanged.AddListener(val => write(val));
		RefreshPropertyValues += () => field.Toggle.isOn = read();
		Properties.Add(field.gameObject);
	}
	
	public void AddField(string name, Func<int> read, Action<int> write, string[] enumOptions)
	{
		var field = Instantiate(EnumField, transform);
		field.Label.text = name;
		field.Dropdown.options = enumOptions.Select(s => new TMP_Dropdown.OptionData(s)).ToList();
		field.Dropdown.onValueChanged.AddListener(val => write(val));
		RefreshPropertyValues += () => field.Dropdown.value = read();
		Properties.Add(field.gameObject);
	}

	void RemoveListener()
	{
		if (ChangeSource != null)
			ChangeSource.OnChanged -= OnPropertiesChanged;
	}
	
	public void Inspect(Entity entity)
	{
        Clear();
        Title.text = entity.Name;
        var hull = Context.Cache.Get<Gear>(entity.Hull);
        var hullData = Context.Cache.Get<HullData>(hull.Data);
        AddSection(
            hullData.HullType == HullType.Ship ? "Ship" :
            hullData.HullType == HullType.Station ? "Station" :
            "Platform");
        //AddList(hullData.Name).Inspect(hull, entity);
        //PropertiesPanel.AddProperty("Hull", () => $"{hullData.Name}");
        AddProperty("Capacity", () => $"{entity.OccupiedCapacity}/{entity.Capacity:0}");
        AddProperty("Mass", () => $"{entity.Mass.SignificantDigits(Context.GlobalData.SignificantDigits)}");
        AddProperty("Temperature", () => $"{entity.Temperature:0}°K");
        AddProperty("Energy", () => $"{entity.Energy:0}/{entity.GetBehaviors<Reactor>().First().Capacitance:0}");
        
        AddList("Gear").InspectHardpoints(entity);
        AddList("Cargo").InspectCargo(entity);
        
        RefreshValues();
	}

	public void InspectHardpoints(Entity entity)
	{
		RemoveListener();

		ChangeSource = entity.GearEvent;
		OnPropertiesChanged = InspectGearInternal;
		ChangeSource.OnChanged += OnPropertiesChanged;
		InspectGearInternal();

		void InspectGearInternal()
		{
			Clear();
			var equippedItems = entity.Hardpoints.Where(hp => hp.Gear != null);
			foreach(var hardpoint in equippedItems)
				AddList(hardpoint.ItemData.Name).Inspect(entity, hardpoint);
			RefreshValues();
		}
	}

	public void InspectCargo(Entity entity)
	{
		RemoveListener();

		ChangeSource = entity.CargoEvent;
		OnPropertiesChanged = InspectCargoInternal;
		ChangeSource.OnChanged += OnPropertiesChanged;
		InspectCargoInternal();
		
		void InspectCargoInternal()
		{
			Clear();
			foreach (var itemID in entity.Cargo)
			{
				var itemInstance = Context.Cache.Get<ItemInstance>(itemID);
				var data = Context.Cache.Get<ItemData>(itemInstance.Data);
				if(itemInstance is SimpleCommodity simpleCommodity)
					AddProperty(data.Name, () => simpleCommodity.Quantity.ToString());
				else
					AddProperty(data.Name);
			}
			RefreshValues();
		}
	}

	public void Inspect(Entity entity, Hardpoint hardpoint)
	{
		RemoveListener();

		ChangeSource = hardpoint.Gear;
		OnPropertiesChanged = InspectGearInternal;
		ChangeSource.OnChanged += OnPropertiesChanged;
		InspectGearInternal();

		void InspectGearInternal()
		{
			Clear();
			AddProperty("Durability", () => $"{hardpoint.Gear.Durability.SignificantDigits(Context.GlobalData.SignificantDigits)}/{Context.Evaluate(hardpoint.ItemData.Durability, hardpoint.Gear).SignificantDigits(Context.GlobalData.SignificantDigits)}");
			foreach (var behavior in hardpoint.ItemData.Behaviors)
			{
				var type = behavior.GetType();
				if (type.GetCustomAttribute(typeof(RuntimeInspectable)) != null)
				{
					foreach (var field in type.GetFields().Where(f => f.GetCustomAttribute<RuntimeInspectable>() != null))
					{
						var fieldType = field.FieldType;
						if (fieldType == typeof(float))
							AddProperty(field.Name, () => $"{((float) field.GetValue(behavior)).SignificantDigits(Context.GlobalData.SignificantDigits)}");
						else if (fieldType == typeof(int))
							AddProperty(field.Name, () => $"{(int) field.GetValue(behavior)}");
						else if (fieldType == typeof(PerformanceStat))
						{
							var stat = (PerformanceStat) field.GetValue(behavior);
							AddProperty(field.Name, () => $"{Context.Evaluate(stat, hardpoint.Gear, entity).SignificantDigits(Context.GlobalData.SignificantDigits)}");
						}
					}
				}
			}
	        foreach (var behavior in hardpoint.Behaviors)
	        {
	            if (behavior is Factory factory)
	            {
	                AddField("Production Quality", () => factory.ProductionQuality, f => factory.ProductionQuality = f, 0, 1);
	                var corporation = Context.Cache.Get<Corporation>(entity.Corporation);
	                var compatibleBlueprints = corporation.UnlockedBlueprints
	                    .Select(id => Context.Cache.Get<BlueprintData>(id))
	                    .Where(bp => bp.FactoryItem == hardpoint.ItemData.ID).ToList();
	                if (factory.RetoolingTime > 0)
	                {
	                    AddProgressField("Retooling", () => (factory.ToolingTime - (float) factory.RetoolingTime) / factory.ToolingTime);
	                }
	                else
	                {
	                    AddField("Item", 
	                        () => compatibleBlueprints.FindIndex(bp=>bp.ID== factory.Blueprint) + 1, 
	                        i => factory.Blueprint = i == 0 ? Guid.Empty : compatibleBlueprints[i - 1].ID,
	                        new []{"None"}.Concat(compatibleBlueprints.Select(bp=>bp.Name)).ToArray());
	                    if (factory.Blueprint != Guid.Empty)
	                    {
	                        if (factory.ItemUnderConstruction != Guid.Empty)
	                        {
	                            AddProgressField("Production", () =>
	                            {
		                            if (factory.ItemUnderConstruction == Guid.Empty) return 1;
		                            var itemInstance = Context.Cache.Get<CraftedItemInstance>(factory.ItemUnderConstruction);
		                            var blueprintData = Context.Cache.Get<BlueprintData>(itemInstance.Blueprint);
		                            return (blueprintData.ProductionTime - (float) entity.IncompleteCargo[factory.ItemUnderConstruction]) / blueprintData.ProductionTime;
	                            });
	                        }
	                        else
	                        {
	                            var ingredientsList = AddList("Ingredients Needed");
	                            var blueprintData = Context.Cache.Get<BlueprintData>(factory.Blueprint);
	                            foreach (var ingredient in blueprintData.Ingredients)
	                            {
	                                var itemData = Context.Cache.Get<ItemData>(ingredient.Key);
	                                ingredientsList.AddProperty(itemData.Name, () => ingredient.Value.ToString());
	                            }
	                            ingredientsList.RefreshValues();
	                        }
	                    }
	                }
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
