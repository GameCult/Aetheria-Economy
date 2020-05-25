using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ReadOnlyPropertiesPanel : MonoBehaviour
{
    public TextMeshProUGUI Title;
    public RectTransform SectionPrefab;
    public RectTransform ListPrefab;
    public RectTransform ListItemPrefab;
    public PropertyLabel ListPropertyPrefab;
    public PropertyLabel PropertyPrefab;

    private List<GameObject> _properties = new List<GameObject>();

    public void Clear()
    {
        foreach(var property in _properties)
            Destroy(property);
    }

    public void AddProperty(string name, Func<string> value)
    {
        var property = Instantiate(PropertyPrefab, transform);
        property.Name.text = name;
        property.ValueFunction = value;
        _properties.Add(property.gameObject);
    }

    public void AddSection(string name)
    {
        var section = Instantiate(SectionPrefab, transform);
        section.GetComponentInChildren<TextMeshProUGUI>().text = name;
        _properties.Add(section.gameObject);
    }

    public void AddList(string name, IEnumerable<string> elements)
    {
        var list = Instantiate(ListPrefab, transform);
        list.GetComponentInChildren<TextMeshProUGUI>().text = name;
        foreach (var element in elements)
        {
            var item = Instantiate(ListItemPrefab, list);
            item.GetComponentInChildren<TextMeshProUGUI>().text = element;
            _properties.Add(item.gameObject);
        }
        _properties.Add(list.gameObject);
    }

    public void AddList(string name, IEnumerable<Tuple<string, string>> elements)
    {
        var list = Instantiate(ListPrefab, transform);
        list.GetComponentInChildren<TextMeshProUGUI>().text = name;
        foreach (var element in elements)
        {
            var item = Instantiate(ListPropertyPrefab, list);
            item.Name.text = element.Item1;
            item.Value.text = element.Item2;
            _properties.Add(item.gameObject);
        }
        _properties.Add(list.gameObject);
    }
}
