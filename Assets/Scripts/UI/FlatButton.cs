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

public class FlatButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public event Action OnClick;
    public float StateDamping;
//    public RectTransform Tab;
    public Image Fill;
    public Image Outline;
    public VerticalLayoutGroup LabelLayout;
    public FlatButtonState CurrentState = FlatButtonState.Unselected;
    public bool DisableClickWhenSelected = true;

    public FlatButtonAppearance UnselectedAppearance;
    public FlatButtonAppearance SelectedAppearance;
    public FlatButtonAppearance HoverAppearance;
    public FlatButtonAppearance ClickAppearance;
    public FlatButtonAppearance DisabledAppearance;

    private RectOffset _defaultPadding;
    private float _currentPadding;

    private void Start()
    {
        _defaultPadding = LabelLayout.padding;
        _currentPadding = _defaultPadding.left;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (CurrentState == FlatButtonState.Unselected)
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
            Fill.color = ClickAppearance.FillColor;
            Outline.color = ClickAppearance.OutlineColor;
            OnClick?.Invoke();
        }
    }

    private void Update()
    {
        FlatButtonAppearance appearance;
        switch (CurrentState)
        {
            case FlatButtonState.Unselected:
                appearance = UnselectedAppearance;
                break;
            case FlatButtonState.Selected:
                appearance = SelectedAppearance;
                break;
            case FlatButtonState.Hover:
                appearance = HoverAppearance;
                break;
            case FlatButtonState.Disabled:
                appearance = DisabledAppearance;
                break;
            default:
                appearance = new FlatButtonAppearance();
                break;
        }

        Fill.color = Color.Lerp(Fill.color, appearance.FillColor, StateDamping * Time.deltaTime);
        Outline.color = Color.Lerp(Outline.color, appearance.OutlineColor, StateDamping * Time.deltaTime);
        _currentPadding = Mathf.Lerp(_currentPadding, appearance.LayoutPadding, StateDamping * Time.deltaTime);
        LabelLayout.padding = new RectOffset
        {
            bottom = _defaultPadding.bottom,
            top = _defaultPadding.top,
            left = (int) _currentPadding,
            right = (int) _currentPadding
        };
    }
}

public enum FlatButtonState
{
    Unselected,
    Selected,
    Hover,
    Disabled
}

[Serializable]
public struct FlatButtonAppearance
{
    public Color FillColor;
    public Color OutlineColor;
    public int LayoutPadding;
}