using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PropertiesList : ReadOnlyPropertiesPanel
{
    public Image FoldoutIcon;
    public float FoldoutRotationDamping;
    public FlatFlatButton Button;
    public VerticalLayoutGroup LayoutGroup;
    public int FoldedPadding = -8;
    public int ExpandedPadding = 8;

    private bool _expanded = false;
    private float _targetFoldoutRotation = 0;
    private float _foldoutRotation = 0;

    public bool Expanded
    {
        set
        {
            _expanded = value;
            var padding = LayoutGroup.padding;
            padding = new RectOffset(padding.left, padding.right, padding.top, _expanded ? ExpandedPadding : FoldedPadding);
            LayoutGroup.padding = padding;
            foreach (var property in Properties) property.SetActive(_expanded);
            _targetFoldoutRotation = _expanded ? -90 : 0;
        }
    }

    public void Start()
    {
        Button.OnClick += ToggleExpand;
    }

    public void ToggleExpand() => Expanded = !_expanded;

    private void Update()
    {
        _foldoutRotation =
            Mathf.Lerp(_foldoutRotation, _targetFoldoutRotation, FoldoutRotationDamping * Time.deltaTime);
        FoldoutIcon.transform.localRotation = Quaternion.Euler(0,0, _foldoutRotation);
    }

    public new PropertyLabel AddProperty(string name, Func<string> value = null, Action onClick = null)
    {
        var prop = base.AddProperty(name, value, onClick);
        prop.gameObject.SetActive(false);
        return prop;
    }

    public new PropertiesList AddList(string name)
    {
        var list = base.AddList(name);
        list.gameObject.SetActive(false);
        return list;
    }

    public new RectTransform AddSection(string name)
    {
        var section = base.AddSection(name);
        section.gameObject.SetActive(false);
        return section;
    }
}
