/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FlatFlatButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public event Action<PointerEventData> OnClick;
    public float StateDamping;
//    public RectTransform Tab;
    public Image Fill;
    public TextMeshProUGUI Label;
    public FlatButtonState CurrentState = FlatButtonState.Unselected;
    public bool DisableClickWhenSelected = true;
    public RectTransform DragObject;

    public Color UnselectedColor;
    public Color SelectedColor;
    public Color HoverColor;
    public Color ClickColor;
    public Color DisabledColor;
    public Color EnabledTextColor;
    public Color DisabledTextColor;

    public Func<PointerEventData, bool> DragSuccess;
    private int _siblingIndex;
    private Transform _dragParent;
    private Vector3 _dragPositionOffset;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (OnClick != null && CurrentState == FlatButtonState.Unselected)
            CurrentState = FlatButtonState.Hover;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (CurrentState == FlatButtonState.Hover)
            CurrentState = FlatButtonState.Unselected;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (CurrentState != FlatButtonState.Disabled && (!DisableClickWhenSelected || CurrentState != FlatButtonState.Selected))
        {
            if (OnClick != null)
            {
                Fill.color = ClickColor;
                OnClick(eventData);
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (DragSuccess == null || DragObject == null)
        {
            eventData.pointerDrag = null;
            return;
        }
        
        var canvas = gameObject.FindInParents<Canvas>();
        if (canvas == null)
            return;
        
        _siblingIndex = DragObject.GetSiblingIndex();
        _dragParent = DragObject.parent;
        
        DragObject.SetParent(canvas.transform, true);
        _dragPositionOffset = DragObject.position - (Vector3)eventData.position;
        DragObject.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        var targetPosition = (Vector3)eventData.position + _dragPositionOffset;
        //Debug.Log($"Moving drag object to {targetPosition}");
        DragObject.position = targetPosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if(DragSuccess(eventData))
            Destroy(DragObject.gameObject);
        else
        {
            DragObject.parent = _dragParent;
            DragObject.SetSiblingIndex(_siblingIndex);
        }
    }

    private void Update()
    {
        Color appearance;
        switch (CurrentState)
        {
            case FlatButtonState.Unselected:
                appearance = UnselectedColor;
                break;
            case FlatButtonState.Selected:
                appearance = SelectedColor;
                break;
            case FlatButtonState.Hover:
                appearance = HoverColor;
                break;
            case FlatButtonState.Disabled:
                appearance = DisabledColor;
                break;
            default:
                appearance = Color.white;
                break;
        }

        if (Label != null)
            Label.color = Color.Lerp(Label.color,
                CurrentState == FlatButtonState.Disabled ? DisabledTextColor : EnabledTextColor, StateDamping * Time.deltaTime);
        Fill.color = Color.Lerp(Fill.color, appearance, StateDamping * Time.deltaTime);
    }
}