using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FlatFlatButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public event Action OnClick;
    public float StateDamping;
//    public RectTransform Tab;
    public Image Fill;
    public FlatButtonState CurrentState = FlatButtonState.Unselected;
    public bool DisableClickWhenSelected = true;

    public Color UnselectedColor;
    public Color SelectedColor;
    public Color HoverColor;
    public Color ClickColor;

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
        if (!DisableClickWhenSelected || CurrentState != FlatButtonState.Selected)
        {
            if (OnClick != null)
            {
                Fill.color = ClickColor;
                OnClick();
            }
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
            default:
                appearance = Color.white;
                break;
        }

        Fill.color = Color.Lerp(Fill.color, appearance, StateDamping * Time.deltaTime);
    }
}