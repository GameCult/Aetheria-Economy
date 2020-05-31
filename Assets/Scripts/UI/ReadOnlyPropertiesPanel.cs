using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ReadOnlyPropertiesPanel : MonoBehaviour
{
    public TextMeshProUGUI Title;
    public RectTransform SectionPrefab;
    public PropertiesList ListPrefab;
    public PropertyLabel PropertyPrefab;
    public bool ChildRadioSelection;

    protected List<GameObject> Properties = new List<GameObject>();
    protected List<FlatFlatButton> Buttons = new List<FlatFlatButton>();
    protected FlatFlatButton SelectedChild;

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
}
