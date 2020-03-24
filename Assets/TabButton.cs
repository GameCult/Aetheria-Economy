using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TabButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public event Action<TabButton> OnClick;
    public float StateDamping;
    public RectTransform Tab;
    public Image Fill;
    public Image Outline;
    public TextMeshProUGUI Label;
    public VerticalLayoutGroup LabelLayout;
    public TabButtonState CurrentState = TabButtonState.Unselected;

    public TabButtonAppearance UnselectedAppearance;
    public TabButtonAppearance SelectedAppearance;
    public TabButtonAppearance HoverAppearance;
    public TabButtonAppearance ClickAppearance;

    private RectOffset _defaultPadding;
    private float _currentPadding;

    private void Start()
    {
        _defaultPadding = LabelLayout.padding;
        _currentPadding = _defaultPadding.left;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (CurrentState == TabButtonState.Unselected)
            CurrentState = TabButtonState.Hover;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (CurrentState == TabButtonState.Hover)
            CurrentState = TabButtonState.Unselected;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (CurrentState != TabButtonState.Selected)
        {
            Fill.color = ClickAppearance.FillColor;
            Outline.color = ClickAppearance.OutlineColor;
            OnClick?.Invoke(this);
        }
    }

    private void Update()
    {
        TabButtonAppearance appearance;
        switch (CurrentState)
        {
            case TabButtonState.Unselected:
                appearance = UnselectedAppearance;
                break;
            case TabButtonState.Selected:
                appearance = SelectedAppearance;
                break;
            case TabButtonState.Hover:
                appearance = HoverAppearance;
                break;
            default:
                appearance = new TabButtonAppearance();
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

public enum TabButtonState
{
    Unselected,
    Selected,
    Hover
}

[Serializable]
public struct TabButtonAppearance
{
    public Color FillColor;
    public Color OutlineColor;
    public int LayoutPadding;
}