using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class HoverCursorChange : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public RectTransform CursorObject;
    private bool _active;
    public void OnPointerEnter(PointerEventData eventData)
    {
        CursorObject.gameObject.SetActive(true);
        _active = true;
    }

    private void Update()
    {
        if (_active)
            CursorObject.anchoredPosition = Mouse.current.position.ReadValue();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _active = false;
        CursorObject.gameObject.SetActive(false);
    }
}
