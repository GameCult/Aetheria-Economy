using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TabGroup : MonoBehaviour
{
    public event Action<TabButton> OnTabChange;
    private TabButton[] _tabButtons;
    private TabButton _currentButton;

    private void Start()
    {
        _tabButtons = GetComponentsInChildren<TabButton>();
        foreach (var thisButton in _tabButtons)
        {
            thisButton.OnClick += button =>
            {
                if (_currentButton != null)
                {
                    _currentButton.CurrentState = TabButtonState.Unselected;
                    _currentButton.Tab.gameObject.SetActive(false);
                }

                _currentButton = thisButton;
                _currentButton.CurrentState = TabButtonState.Selected;
                _currentButton.Tab.gameObject.SetActive(true);
                OnTabChange?.Invoke(_currentButton);
            };
            thisButton.Tab.gameObject.SetActive(false);
        }

        _tabButtons.First().OnPointerClick(null);
    }
}
