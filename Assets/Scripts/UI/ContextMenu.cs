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
    public RectTransform OptionPrefab;

    public RectTransform ContentRoot;

    public float OptionHeight;
    public float PaddingHeight;
    
    private List<GameObject> _options = new List<GameObject>();

    public void SetOptions(IEnumerable<(string text, Action action)> options)
    {
        var rect = transform as RectTransform;
        var pivot = rect.pivot;
        pivot.x = Input.mousePosition.x > Screen.width - rect.sizeDelta.x * Canvas.scaleFactor ? 1 : 0;
        pivot.y = Input.mousePosition.y < (PaddingHeight + options.Count() * OptionHeight) * Canvas.scaleFactor ? 0 : 1;
        rect.pivot = pivot;
        rect.position = Input.mousePosition;
        
        // Destroy previous options
        foreach(var option in _options)
            Destroy(option);
        _options.Clear();
        
        foreach (var option in options)
        {
            var optionButton = Instantiate(OptionPrefab, ContentRoot);
            optionButton.GetComponentInChildren<TextMeshProUGUI>().text = option.text;
            optionButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                option.action();
                gameObject.SetActive(false);
            });
            _options.Add(optionButton.gameObject);
        }
    }
}
