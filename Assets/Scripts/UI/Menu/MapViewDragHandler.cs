/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Mathematics.math;

public class MapViewDragHandler : MonoBehaviour
{
    public Camera Camera;
    public float Scaling;
    public ClickCatcher Background;
    private Vector2 _previousPosition;
    private bool _isDragging;

    void Update()
    {
        var newPosition = Camera.ScreenToWorldPoint(Input.mousePosition);
        
        if (!Input.GetMouseButton(0)) _isDragging = false;
        
        if (_isDragging) Camera.transform.position -= (Vector3) ((Vector2) newPosition - _previousPosition);

        if (Background.PointerIsInside)
        {
            if (Input.GetMouseButtonDown(0))
            {
                _previousPosition = newPosition;
                _isDragging = true;
            }

            if (abs(Input.mouseScrollDelta.y) > .01f)
            {
                var previousPosition = Camera.ScreenToWorldPoint(Input.mousePosition);
                Camera.orthographicSize *= 1 + Input.mouseScrollDelta.y * Scaling;
                Camera.transform.position -= Camera.ScreenToWorldPoint(Input.mousePosition) - previousPosition;
            }
        }
    }
}
