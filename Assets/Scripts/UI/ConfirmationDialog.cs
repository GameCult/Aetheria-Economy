using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConfirmationDialog : PropertiesPanel
{
    public ClickCatcher CancelClickCatcher;
    public FlatButton Cancel;
    public FlatButton Confirm;

    private Action _onCancel;
    private Action _onConfirm;

    private new void Start()
    {
        Cancel.OnClick += () =>
        {
            _onCancel?.Invoke();
            End();
        };
        Confirm.OnClick += () =>
        {
            _onConfirm?.Invoke();
            End();
        };
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
    }
    
    private void End()
    {
        CancelClickCatcher.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    public void Show(Action onConfirm, Action onCancel = null)
    {
        _onConfirm = onConfirm;
        _onCancel = onCancel;
        transform.position = Input.mousePosition;
        CancelClickCatcher.gameObject.SetActive(true);
    }
}
