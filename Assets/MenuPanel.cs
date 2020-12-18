using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuPanel : MonoBehaviour
{
    public RectTransform TabButtons;
    public Color ActiveTabColor;
    public Color InactiveTabColor;

    public event Action<MenuTab> TabChanged;

    private Dictionary<MenuTab, MenuTabButton> _tabs = new Dictionary<MenuTab, MenuTabButton>();
    private MenuTabButton _current;
    
    public MenuTab CurrentTab { get; private set; }

    public void ShowTab(MenuTab tab)
    {
        gameObject.SetActive(true);
        
        if (_current == _tabs[tab]) return;
        
        if(_current != null)
        {
            _current.TabContents.SetActive(false);
            _current.Text.color = InactiveTabColor;
        }

        CurrentTab = tab;
        _current = _tabs[tab];
        
        _current.TabContents.SetActive(true);
        _current.Text.color = ActiveTabColor;
        
        TabChanged?.Invoke(tab);
    }
    
    void Start()
    {
        foreach (var tabButton in TabButtons.GetComponentsInChildren<MenuTabButton>())
        {
            tabButton.TabContents.SetActive(false);
            tabButton.Text.color = InactiveTabColor;
            _tabs.Add(tabButton.Tab, tabButton);
            tabButton.Button.onClick.AddListener(() => ShowTab(tabButton.Tab));
        }
        ShowTab(MenuTab.Inventory);
    }
}

public enum MenuTab
{
    Map,
    Inventory
}