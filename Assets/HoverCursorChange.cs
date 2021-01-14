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
        Cursor.visible = false;
    }

    private void Update()
    {
        if (_active)
            CursorObject.anchoredPosition = Mouse.current.position.ReadValue();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Cursor.visible = true;
        _active = false;
        CursorObject.gameObject.SetActive(false);
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}
