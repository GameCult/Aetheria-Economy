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

    public float Speed;

    private float _yaw;
    private float _pitch;

    void Update()
    {
        if (Target)
        {
            var targetLocal = transform.parent.InverseTransformPoint(Target.position);
            
            var yaw = Vector2.SignedAngle(new Vector2(0, 1), new Vector2(targetLocal.x, targetLocal.z));
            var pitch = Vector2.SignedAngle(new Vector2(1, 0), new Vector2(targetLocal.z, targetLocal.y));
            
            var targetYaw = clamp(-yaw, YawMin, YawMax);
            var targetPitch = clamp(-pitch, PitchMin, PitchMax);

            if (abs(targetYaw - _yaw) < Speed * Time.deltaTime)
                _yaw = targetYaw;
            else _yaw = _yaw + sign(targetYaw - _yaw) * Speed * Time.deltaTime;

            if (abs(targetPitch - _pitch) < Speed * Time.deltaTime)
                _pitch = targetPitch;
            else _pitch = _pitch + sign(targetPitch - _pitch) * Speed * Time.deltaTime;
            
            transform.localRotation = Quaternion.Euler(_pitch, _yaw, 0);
        }
    }
}
