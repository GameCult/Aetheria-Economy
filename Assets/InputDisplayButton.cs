using System.Collections;
using System.Collections.Generic;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

public class InputDisplayButton : MonoBehaviour
{
    public Image Fill;
    public Image Outline;
    public ObservableBeginDragTrigger BeginDragTrigger;
    public ObservableEndDragTrigger EndDragTrigger;
    public ObservablePointerClickTrigger ClickTrigger;
    public ObservablePointerEnterTrigger EnterTrigger;
    public ObservablePointerExitTrigger ExitTrigger;
}
