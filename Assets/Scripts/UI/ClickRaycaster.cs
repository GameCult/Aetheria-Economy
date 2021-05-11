/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickRaycaster : MonoBehaviour
{
    public LayerMask Layers;
    public event Action<PointerEventData> OnClickMiss;
    public Camera RayCamera;
    private ClickCatcher _clickCatcher;
    
    void Start()
    {
        _clickCatcher = GetComponent<ClickCatcher>();
        _clickCatcher.OnClick.Subscribe(pointer =>
        {
            RaycastHit hit;
            Physics.Raycast(RayCamera.ScreenPointToRay(pointer.position), out hit, 1000, Layers.value);
            if (hit.collider != null)
            {
                var clickable = hit.collider.GetComponent<ClickableCollider>();
                if (clickable != null)
                    clickable.Click(pointer);
            }
            else OnClickMiss?.Invoke(pointer);
        });
    }
}
