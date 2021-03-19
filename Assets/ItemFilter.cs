using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemFilter : MonoBehaviour
{
    public TextMeshProUGUI Label;
    public Button DisableButton;

    public event Action OnDisable;
    // Start is called before the first frame update
    void Start()
    {
        DisableButton.onClick.AddListener(() =>
        {
            GetComponent<Prototype>().ReturnToPool();
            OnDisable?.Invoke();
            OnDisable = null;
        });
    }
}
