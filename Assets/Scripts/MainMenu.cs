using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public TextMeshProUGUI Title;
    public Prototype ButtonPrototype;

    private List<Prototype> _contents = new List<Prototype>();
    
    void Start()
    {
        ShowMain();
    }

    private void ShowMain()
    {
        Title.text = "aetheria\n<smallcaps><size=50%>terminus";
        Clear();
        AddButton("Continue", null);
        AddButton("New Game", null);
        AddButton("Load Game", null);
        AddButton("Settings", null);
        AddButton("Quit", Application.Quit);
    }

    private void Clear()
    {
        foreach(var item in _contents)
            item.ReturnToPool();
        _contents.Clear();
    }

    private void AddButton(string label, Action onPress)
    {
        var button = ButtonPrototype.Instantiate<Button>();
        button.GetComponent<TextMeshProUGUI>().text = label;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onPress?.Invoke());
        _contents.Add(button.GetComponent<Prototype>());
    }
}
