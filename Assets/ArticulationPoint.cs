using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public class ArticulationPoint : MonoBehaviour
{
    public Transform Target;
    
    public float YawMin;
    public float YawMax;

    public float PitchMin;
    public float PitchMax;

    void Update()
    {
        if (Target)
        {
            var targetLocal = transform.parent.InverseTransformPoint(Target.position);
            var yaw = Vector2.SignedAngle(new Vector2(0, 1), new Vector2(targetLocal.x, targetLocal.z));
            var pitch = Vector2.SignedAngle(new Vector2(1, 0), new Vector2(targetLocal.z, targetLocal.y));
            transform.localRotation = Quaternion.Euler(clamp(-pitch, -PitchMin, -PitchMax), clamp(yaw, YawMin, YawMax), 0);
        }
    }
}
