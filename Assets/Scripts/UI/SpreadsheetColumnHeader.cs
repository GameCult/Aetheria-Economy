using System.Collections;
using System.Collections.Generic;
using TMPro;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

public class SpreadsheetColumnHeader : MonoBehaviour
{
    public TextMeshProUGUI Title;
    public RectTransform Rect;

    public Button Button;
    public Image SortIcon;

    public ObservableBeginDragTrigger ResizeBeginDragTrigger;
    public ObservableDragTrigger ResizeDragTrigger;
    public ObservableEndDragTrigger ResizeEndDragTrigger;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
