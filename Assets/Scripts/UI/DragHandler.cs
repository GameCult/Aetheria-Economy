using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Mathematics.math;

public class DragHandler : MonoBehaviour
{
    public Camera Camera;
    public float Scaling;
    private Vector2 _previousPosition;

    void Update()
    {
        var newPosition = Camera.ScreenToWorldPoint(Input.mousePosition);
        if(Input.GetMouseButtonDown(0))
            _previousPosition = newPosition;
        if (Input.GetMouseButton(0))
        {
            Camera.transform.position -= (Vector3)((Vector2) newPosition - _previousPosition);
        }

        if (abs(Input.mouseScrollDelta.y) > .01f)
        {
            var previousPosition = Camera.ScreenToWorldPoint(Input.mousePosition);
            Camera.orthographicSize *= 1 + Input.mouseScrollDelta.y * Scaling;
            Camera.transform.position -= Camera.ScreenToWorldPoint(Input.mousePosition) - previousPosition;
        }
    }
}
