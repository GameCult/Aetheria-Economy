using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SizeFilter : MonoBehaviour
{
    public TMP_InputField Width;
    public TMP_InputField Height;
    public Button DisableButton;

    public event Action OnDisable;
    
    void Start()
    {
        DisableButton.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
            OnDisable?.Invoke();
            OnDisable = null;
        });
        //gameObject.SetActive(false);
    }
}
