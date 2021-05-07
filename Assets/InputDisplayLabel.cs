using System.Collections;
using System.Collections.Generic;
using TMPro;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

public class InputDisplayLabel : MonoBehaviour
{
    public TextMeshProUGUI Label;
    public ObservableBeginDragTrigger BeginDragTrigger;
    public ObservableEndDragTrigger EndDragTrigger;
}
