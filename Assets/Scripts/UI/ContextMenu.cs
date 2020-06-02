using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ContextMenu : MonoBehaviour
{
    public Canvas Canvas;
    public RectTransform Root;
    public ClickCatcher CancelClickCatcher;
    public ContextMenuOption OptionPrefab;
    public ContextMenu DropMenuPrefab;
    public ContextMenu Parent;
    public bool ForceDirectionRight;
    public Vector3 ForcePosition;

    public RectTransform ContentRoot;

    public float OptionHeight;
    public float PaddingHeight;
    
    private List<GameObject> _options = new List<GameObject>();
    private bool _hasDropdown;
    private bool _dropdownRight;
    private GameObject _dropdown;
    private ContextMenuOption _currentDropdown;

    private void Start()
    {
        if (CancelClickCatcher != null)
            CancelClickCatcher.OnClick += data =>
            {
                End();
            };
    }

    private void End()
    {
        if (_dropdown != null)
            Destroy(_dropdown);
        CancelClickCatcher.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    public void Clear()
    {
        // Destroy previous options
        foreach(var option in _options)
            Destroy(option);
        _options.Clear();
        _hasDropdown = false;
    }

    public void AddOption(string text, Action action, bool enabled = true)
    {
        var optionButton = Instantiate(OptionPrefab, ContentRoot);
        optionButton.Label.text = text;
        optionButton.Button.onClick.AddListener(() =>
        {
            action();
            if(Parent!=null)
                Parent.End();
            else
                End();
        });
        optionButton.Hover.OnEnter += data =>
        {
            if (_dropdown != null)
                Destroy(_dropdown);
        };
        optionButton.Arrow.gameObject.SetActive(false);
        optionButton.Button.interactable = enabled;
        _options.Add(optionButton.gameObject);
    }

    public void AddDropdown(string text, IEnumerable<(string text, Action action, bool enabled)> options)
    {
        _hasDropdown = true;
        var optionButton = Instantiate(OptionPrefab, ContentRoot);
        optionButton.Label.text = text;
        optionButton.Hover.OnEnter += data =>
        {
            if (_currentDropdown != optionButton)
            {
                if(_dropdown!=null)
                    Destroy(_dropdown);
                var drop = Instantiate(DropMenuPrefab, Root);
                _dropdown = drop.gameObject;
                foreach (var option in options)
                    drop.AddOption(option.text, option.action, option.enabled);
                drop.Parent = this;
                drop.ForceDirectionRight = _dropdownRight;
                Vector3[] corners = new Vector3[4];
                ((RectTransform) optionButton.transform).GetWorldCorners(corners);
                drop.ForcePosition = corners[_dropdownRight ? 2 : 1];
                drop.Show();
            }
        };
        _options.Add(optionButton.gameObject);
    }

    public void Show()
    {
        var rect = transform as RectTransform;
        var pivot = rect.pivot;
        var canvas = Parent != null ? Parent.Canvas : Canvas;
        if (Parent!=null)
            pivot.x = ForceDirectionRight ? 0 : 1;
        else
            pivot.x = Input.mousePosition.x > Screen.width - rect.sizeDelta.x * canvas.scaleFactor ? 1 : 0;
        var scaleFactor = canvas.scaleFactor;
        _dropdownRight = Input.mousePosition.x < Screen.width - rect.sizeDelta.x * scaleFactor * 2;
        var pivotTop = (Parent==null ? Input.mousePosition.y : ForcePosition.y) > (PaddingHeight + _options.Count * OptionHeight) * scaleFactor;
        pivot.y = pivotTop ? 1 : 0;
        rect.pivot = pivot;
        rect.position = Parent!=null ? ForcePosition - (pivotTop ? Vector3.zero : Vector3.up * (OptionHeight * scaleFactor)) : Input.mousePosition;
        if (Parent==null)
            CancelClickCatcher.gameObject.SetActive(true);
    }
}
