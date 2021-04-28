/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

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
            thisButton.OnClick += () =>
            {
                if (_currentButton != null)
                {
                    _currentButton.CurrentState = FlatButtonState.Unselected;
                    _currentButton.Tab.gameObject.SetActive(false);
                }

                _currentButton = thisButton;
                _currentButton.CurrentState = FlatButtonState.Selected;
                _currentButton.Tab.gameObject.SetActive(true);
                OnTabChange?.Invoke(_currentButton);
            };
            thisButton.Tab.gameObject.SetActive(false);
        }

        _tabButtons.First().OnPointerClick(null);
    }
}
