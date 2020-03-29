using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickableCollider : MonoBehaviour
{
    public event Action<ClickableCollider, PointerEventData> OnClick;

    public void Click(PointerEventData eventData) => OnClick?.Invoke(this, eventData);
}
