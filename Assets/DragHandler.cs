using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    }
}
