using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpriteCursor : MonoBehaviour
{
    public Canvas Canvas;
    private RectTransform _rectTransform;
    
    void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        _rectTransform.anchoredPosition = Mouse.current.position.ReadValue() / Canvas.scaleFactor;
    }
}
