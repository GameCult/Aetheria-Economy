using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PropertyLabel : MonoBehaviour
{
    public TextMeshProUGUI Name;
    public TextMeshProUGUI Value;
    public Func<string> ValueFunction;

    private void Update()
    {
        Value.text = ValueFunction?.Invoke() ?? "";
    }
}
