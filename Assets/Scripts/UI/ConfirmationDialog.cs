/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ConfirmationDialog : PropertiesPanel
{
    public ClickCatcher CancelClickCatcher;
    public GameObject ButtonGroup;
    public Button Cancel;
    public Button Confirm;

    private Action _onCancel;
    private Action _onConfirm;

    private void Start()
    {
        Cancel.onClick.AddListener(() =>
        {
            _onCancel?.Invoke();
            End();
        });
        Confirm.onClick.AddListener(() =>
        {
            _onConfirm?.Invoke();
            End();
        });
        if (CancelClickCatcher != null)
            CancelClickCatcher.OnClick += data =>
            {
                _onCancel?.Invoke();
                End();
            };

        OnPropertyAdded += go =>
        {
            go.SetActive(true);
            go.transform.SetSiblingIndex(Title.transform.GetSiblingIndex() + 1);
        };
        gameObject.SetActive(false);
    }
    
    private void End()
    {
        CancelClickCatcher.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    public void Show(Action onConfirm = null, Action onCancel = null)
    {
        gameObject.SetActive(true);
        
        _onConfirm = onConfirm;
        Confirm.gameObject.SetActive(onConfirm!=null);
        
        _onCancel = onCancel;
        Cancel.gameObject.SetActive(onCancel!=null);
        
        ButtonGroup.SetActive(onConfirm!=null || onCancel!=null);
        
        transform.position = Mouse.current.position.ReadValue();
        CancelClickCatcher.gameObject.SetActive(true);
    }
}
