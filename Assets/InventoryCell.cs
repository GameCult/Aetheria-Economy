using System.Collections;
using System.Collections.Generic;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

public class InventoryCell : MonoBehaviour
{
    public Image Icon;

    public ObservablePointerClickTrigger PointerClickTrigger;
    public ObservableBeginDragTrigger BeginDragTrigger;
    public ObservableDragTrigger DragTrigger;
    public ObservableEndDragTrigger EndDragTrigger;
    public ObservablePointerEnterTrigger PointerEnterTrigger;
    public ObservablePointerExitTrigger PointerExitTrigger;
}
