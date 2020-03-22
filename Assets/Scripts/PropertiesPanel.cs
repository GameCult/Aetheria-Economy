using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using JM.LinqFaster;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PropertiesPanel : MonoBehaviour
{
	public Prototype LabelField;
	public Prototype FreeFloatField;
	public Prototype RangedFloatField;
	public Prototype EnumField;
	public Prototype BoolField;
	public Prototype ColorField;
	public Prototype Divider;

	public int ResolutionExponentMinumum = 6;
	public int ResolutionExponentCount = 5;
	
	private readonly List<Prototype> _propertyFields = new List<Prototype>();
	private event EventHandler RefreshPropertyValues;

	public void Clear()
	{
		foreach (var property in _propertyFields.ReverseF())
		{
			property.ReturnToPool();
		}
		
		RefreshPropertyValues = null;
	}

	public void Inspect(object obj, bool inspectablesOnly = false, bool readWrite = false, bool topLevel = false)
	{
		if(topLevel)
			Clear();

		var fields = obj.GetType().GetFields();
		foreach (var field in fields)
			Inspect(obj, field, inspectablesOnly, readWrite);
		
		if(topLevel)
			foreach (var field in _propertyFields) 
				field.transform.SetSiblingIndex(_propertyFields.IndexOf(field));

		RefreshValues();
	}

	public void Inspect(object obj, FieldInfo field, bool inspectablesOnly = false, bool readWrite = false)
	{
		var inspectable = field.GetCustomAttribute<InspectableFieldAttribute>();
		if (inspectable == null && inspectablesOnly) return;
		var type = field.FieldType;
		
		if (type == typeof(float))
		{
			if (readWrite)
			{
				var ranged = inspectable as RangedFloatInspectableAttribute;
				if (ranged != null)
					Inspect(field.Name.SplitCamelCase(), () => (float) field.GetValue(obj), f => field.SetValue(obj, f),
						ranged.Min, ranged.Max);
				else
					Inspect(field.Name.SplitCamelCase(), () => (float) field.GetValue(obj), f => field.SetValue(obj, f));
			} else Inspect(field.Name.SplitCamelCase(), () => ((float) field.GetValue(obj)).ToString("0.##"));
		} 
		else if (type == typeof(int))
		{
			if (readWrite)
			{
				var ranged = inspectable as RangedIntInspectableAttribute;
				if (ranged != null)
					Inspect(field.Name.SplitCamelCase(), () => (int) field.GetValue(obj), f => field.SetValue(obj, f),
						ranged.Min, ranged.Max);
				else
					Inspect(field.Name.SplitCamelCase(), () => (int) field.GetValue(obj), f => field.SetValue(obj, f));
			} else Inspect(field.Name.SplitCamelCase(), () => ((int) field.GetValue(obj)).ToString());
		}
		else if (type.IsEnum)
		{
			if (readWrite)
				Inspect(field.Name.SplitCamelCase(), () => (int) field.GetValue(obj), i => field.SetValue(obj, i),
					Enum.GetNames(field.FieldType));
			else
				Inspect(field.Name.SplitCamelCase(), () => Enum.GetName(type, field.GetValue(obj)).SplitCamelCase());
		}
		else if (type == typeof(string))
		{
			if (readWrite)
				Inspect(field.Name.SplitCamelCase(), () => (string) field.GetValue(obj), i => field.SetValue(obj, i));
			else
				Inspect(field.Name.SplitCamelCase(), () => (string) field.GetValue(obj));
		}
		//else if (field.FieldType == typeof(Color)) Inspect(field.Name, () => (Color) field.GetValue(obj), c => field.SetValue(obj, c));
		else if (field.FieldType == typeof(bool)) Inspect(field.Name, () => (bool) field.GetValue(obj), b => field.SetValue(obj, b));
		else if (type.GetCustomAttribute<InspectableFieldAttribute>() != null)
		{
			if(!_propertyFields.Any() || !_propertyFields.Last().gameObject.name.ToUpper().Contains("DIVIDER"))
				_propertyFields.Add(Divider.Instantiate<Prototype>());
			Inspect(field.GetValue(obj), inspectablesOnly, readWrite);
		}
		else 
			Debug.Log($"Field \"{field.Name}\" has unknown type {field.FieldType.Name}. No inspector was generated.");
	}

	public void Inspect(string name, Func<string> read)
	{
		var labelFieldInstance = LabelField.Instantiate<Prototype>();
		_propertyFields.Add(labelFieldInstance);
		labelFieldInstance.transform.Find("Label").GetComponent<TextMeshProUGUI>().text = name;
		var field = labelFieldInstance.transform.Find("Field").GetComponent<TextMeshProUGUI>();
		field.text = read();
		RefreshPropertyValues += (sender, args) => field.text = read();
	}

	public void Inspect(string name, Func<float> read, Action<float> write)
	{
		var freeFloatFieldInstance = FreeFloatField.Instantiate<Prototype>();
		_propertyFields.Add(freeFloatFieldInstance);
		freeFloatFieldInstance.transform.Find("Label").GetComponent<TextMeshProUGUI>().text = name;
		var inputField = freeFloatFieldInstance.GetComponentInChildren<TMP_InputField>();
		inputField.contentType = TMP_InputField.ContentType.DecimalNumber;
		inputField.onValueChanged.AddListener(val => write(float.Parse(val)));
		RefreshPropertyValues += (sender, args) => inputField.text = read().ToString(CultureInfo.InvariantCulture);
	}

	public void Inspect(string name, Func<string> read, Action<string> write)
	{
		var freeFloatFieldInstance = FreeFloatField.Instantiate<Prototype>();
		_propertyFields.Add(freeFloatFieldInstance);
		freeFloatFieldInstance.transform.Find("Label").GetComponent<TextMeshProUGUI>().text = name;
		var inputField = freeFloatFieldInstance.GetComponentInChildren<TMP_InputField>();
		inputField.contentType = TMP_InputField.ContentType.Standard;
		inputField.onValueChanged.AddListener(val => write(val));
		RefreshPropertyValues += (sender, args) => inputField.text = read();
	}
	
	public void Inspect(string name, Func<float> read, Action<float> write, float min, float max)
	{
		var rangedFloatFieldInstance = RangedFloatField.Instantiate<Prototype>();
		_propertyFields.Add(rangedFloatFieldInstance);
		rangedFloatFieldInstance.transform.Find("Label").GetComponent<TextMeshProUGUI>().text = name;
		var slider = rangedFloatFieldInstance.GetComponentInChildren<Slider>();
		slider.minValue = min;
		slider.maxValue = max;
		slider.onValueChanged.AddListener(val => write(val));
		RefreshPropertyValues += (sender, args) => slider.value = read();
	}

	public void Inspect(string name, Func<int> read, Action<int> write)
	{
		var freeFloatFieldInstance = FreeFloatField.Instantiate<Prototype>();
		_propertyFields.Add(freeFloatFieldInstance);
		freeFloatFieldInstance.transform.Find("Label").GetComponent<TextMeshProUGUI>().text = name;
		var inputField = freeFloatFieldInstance.GetComponentInChildren<TMP_InputField>();
		inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
		inputField.onValueChanged.AddListener(val => write(int.Parse(val)));
		RefreshPropertyValues += (sender, args) => inputField.text = read().ToString(CultureInfo.InvariantCulture);
	}
	
	public void Inspect(string name, Func<int> read, Action<int> write, int min, int max)
	{
		var rangedFloatFieldInstance = RangedFloatField.Instantiate<Prototype>();
		_propertyFields.Add(rangedFloatFieldInstance);
		rangedFloatFieldInstance.transform.Find("Label").GetComponent<TextMeshProUGUI>().text = name;
		var slider = rangedFloatFieldInstance.GetComponentInChildren<Slider>();
		slider.wholeNumbers = true;
		slider.minValue = min;
		slider.maxValue = max;
		slider.onValueChanged.AddListener(val => write(Mathf.RoundToInt(val)));
		RefreshPropertyValues += (sender, args) => slider.value = read();
	}
	
	// public void Inspect(string name, Func<Color> read, Action<Color> write)
	// {
	// 	var colorFieldInstance = ColorField.Instantiate<Prototype>();
	// 	_propertyFields.Add(colorFieldInstance);
	// 	colorFieldInstance.transform.Find("Label").GetComponent<TextMeshProUGUI>().text = name;
	// 	var colorButton = colorFieldInstance.GetComponentInChildren<ColorButton>();
	// 	colorButton.OnColorChanged.AddListener(col => write(col));
	// 	RefreshPropertyValues += (sender, args) => colorButton.GetComponent<Image>().color = read();
	// }
	
	public void Inspect(string name, Func<bool> read, Action<bool> write)
	{
		var boolFieldInstance = BoolField.Instantiate<Prototype>();
		_propertyFields.Add(boolFieldInstance);
		boolFieldInstance.transform.Find("Label").GetComponent<TextMeshProUGUI>().text = name;
		var toggle = boolFieldInstance.GetComponentInChildren<Toggle>();
		toggle.onValueChanged.AddListener(val => write(val));
		RefreshPropertyValues += (sender, args) => toggle.isOn = read();
	}
	
	public void Inspect(string name, Func<int> read, Action<int> write, string[] enumOptions)
	{
		var enumFieldInstance = EnumField.Instantiate<Prototype>();
		_propertyFields.Add(enumFieldInstance);
		enumFieldInstance.transform.Find("Label").GetComponent<TextMeshProUGUI>().text = name;
		var dropDown = enumFieldInstance.GetComponentInChildren<TMP_Dropdown>();
		dropDown.options = enumOptions.Select(s => new TMP_Dropdown.OptionData(s)).ToList();
		dropDown.onValueChanged.AddListener(val => write(val));
		RefreshPropertyValues += (sender, args) => dropDown.value = read();
	}

	public void RefreshValues()
	{
		RefreshPropertyValues?.Invoke(this, EventArgs.Empty);
	}
}