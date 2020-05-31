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
    public FlatFlatButton Button;

    private void Update()
    {
        if(ValueFunction != null)
            Value.text = ValueFunction();
    }
}
