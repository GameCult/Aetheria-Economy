using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate : MonoBehaviour
{
    public Vector3 Axis;

    public float Speed;

    void Update()
    {
        transform.localRotation *= Quaternion.AngleAxis(Speed * Time.deltaTime, Axis);
    }
}
