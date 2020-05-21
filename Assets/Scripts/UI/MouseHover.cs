using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MouseHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public event Action<PointerEventData> OnEnter;
    public event Action<PointerEventData> OnExit;

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnEnter?.Invoke(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnExit?.Invoke(eventData);
    }

    public void ClearListeners()
    {
        OnEnter = null;
        OnExit = null;
    }
}
