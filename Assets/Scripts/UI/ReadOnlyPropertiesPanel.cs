using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;

public class ReadOnlyPropertiesPanel : MonoBehaviour
{
    public TextMeshProUGUI Title;
    public RectTransform SectionPrefab;
    public PropertiesList ListPrefab;
    public PropertyLabel PropertyPrefab;
    public AttributeProperty AttributePrefab;
    public InputField InputField;
    public RangedFloatField RangedFloatField;
    public EnumField EnumField;
    public BoolField BoolField;
    public bool ChildRadioSelection;

    protected List<GameObject> Properties = new List<GameObject>();
    protected List<FlatFlatButton> Buttons = new List<FlatFlatButton>();
    protected FlatFlatButton SelectedChild;
    protected event Action RefreshPropertyValues;

    public void Clear()
    {
        foreach(var property in Properties)
            Destroy(property);
        Properties.Clear();
        Buttons.Clear();
        SelectedChild = null;
    }

    public PropertyLabel AddProperty(string name, Func<string> value = null, Action onClick = null)
    {
        var property = Instantiate(PropertyPrefab, transform);
        property.Name.text = name;
        property.Value.text = value?.Invoke() ?? "";
        property.ValueFunction = value;
        if (ChildRadioSelection)
        {
            Buttons.Add(property.Button);
            property.Button.OnClick += () =>
            {
                if (SelectedChild != null)
                    SelectedChild.CurrentState = FlatButtonState.Unselected;
                SelectedChild = property.Button;
                SelectedChild.CurrentState = FlatButtonState.Selected;
                onClick?.Invoke();
            };
        }
        else if(onClick!=null) property.Button.OnClick += onClick;
        Properties.Add(property.gameObject);
        return property;
    }

    public RectTransform AddSection(string name)
    {
        var section = Instantiate(SectionPrefab, transform);
        section.GetComponentInChildren<TextMeshProUGUI>().text = name;
        Properties.Add(section.gameObject);
        return section;
    }

    public PropertiesList AddList(string name) //, IEnumerable<(string, Func<string>)> elements)
    {
        var list = Instantiate(ListPrefab, transform);
        list.Title.text = name;
        // foreach (var element in elements)
        // {
        //     var item = Instantiate(PropertyPrefab, list);
        //     item.Name.text = element.Item1;
        //     item.Value.text = element.Item2();
        //     item.ValueFunction = element.Item2;
        //     Properties.Add(item.gameObject);
        // }
        Properties.Add(list.gameObject);
        return list;
    }

    public AttributeProperty AddAttribute(PersonalityAttribute attribute, float value)
    {
        var attributeInstance = Instantiate(AttributePrefab, transform);
        attributeInstance.Slider.value = value;
        attributeInstance.Title.text = attribute.Name;
        attributeInstance.HighLabel.text = attribute.HighName;
        attributeInstance.LowLabel.text = attribute.LowName;
        return attributeInstance;
    }
	
	public void AddProperty(string name, Func<string> read, Action<string> write)
	{
		var field = Instantiate(InputField, transform);
		field.Label.text = name;
		field.Field.contentType = TMP_InputField.ContentType.Standard;
		field.Field.onValueChanged.AddListener(val => write(val));
		RefreshPropertyValues += () => field.Field.text = read();
		Properties.Add(field.gameObject);
	}

	public void AddProperty(string name, Func<float> read, Action<float> write)
	{
		var field = Instantiate(InputField, transform);
		field.Label.text = name;
		field.Field.contentType = TMP_InputField.ContentType.DecimalNumber;
		field.Field.onValueChanged.AddListener(val => write(float.Parse(val)));
		RefreshPropertyValues += () => field.Field.text = read().ToString(CultureInfo.InvariantCulture);
		Properties.Add(field.gameObject);
	}
	
	public void AddProperty(string name, Func<int> read, Action<int> write)
	{
		var field = Instantiate(InputField, transform);
		field.Label.text = name;
		field.Field.contentType = TMP_InputField.ContentType.IntegerNumber;
		field.Field.onValueChanged.AddListener(val => write(int.Parse(val)));
		RefreshPropertyValues += () => field.Field.text = read().ToString(CultureInfo.InvariantCulture);
		Properties.Add(field.gameObject);
	}
	
	public void AddProperty(string name, Func<float> read, Action<float> write, float min, float max)
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
	
	public void AddProperty(string name, Func<int> read, Action<int> write, int min, int max)
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
	
	public void AddProperty(string name, Func<bool> read, Action<bool> write)
	{
		var field = Instantiate(BoolField, transform);
		field.Label.text = name;
		field.Toggle.onValueChanged.AddListener(val => write(val));
		RefreshPropertyValues += () => field.Toggle.isOn = read();
		Properties.Add(field.gameObject);
	}
	
	public void AddProperty(string name, Func<int> read, Action<int> write, string[] enumOptions)
	{
		var field = Instantiate(EnumField, transform);
		field.Label.text = name;
		field.Dropdown.options = enumOptions.Select(s => new TMP_Dropdown.OptionData(s)).ToList();
		field.Dropdown.onValueChanged.AddListener(val => write(val));
		RefreshPropertyValues += () => field.Dropdown.value = read();
		Properties.Add(field.gameObject);
	}

	public void RefreshValues()
	{
		RefreshPropertyValues?.Invoke();
	}
}
