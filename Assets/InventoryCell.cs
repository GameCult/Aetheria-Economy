/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

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
