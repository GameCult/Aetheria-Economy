using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatSheet : MonoBehaviour
{
    public PropertyLabel StatPrefab;

    private List<(PropertyLabel property, Func<string> value)> _stats = new List<(PropertyLabel property, Func<string> value)>();

    public void AddStat(string label, Func<string> value)
    {
        var instance = Instantiate(StatPrefab, transform);
        instance.Label.text = label;
        _stats.Add((instance, value));
    }

    public void RefreshValues()
    {
        foreach(var stat in _stats)
            stat.property.Value.text = stat.value();
    }
}
