using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FitToCamera : MonoBehaviour
{
    public Camera Camera;

    void Update()
    {
        transform.localScale = Camera.orthographicSize * 2 * Vector3.one;
    }
}
