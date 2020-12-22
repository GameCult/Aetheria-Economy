/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CopyTransform : MonoBehaviour
{
    public Transform Target;
    public bool CopyXPosition = false;
    public bool CopyYPosition = false;
    public bool CopyZPosition = false;
    public bool CopyRotation = false;
    //public Vector3 RotationOffsetEulerAngles = Vector3.zero;
    public Camera PixelSnapCamera;
    public float PixelSnapIncrement = 1;

    private float _snapLength;

    void Update ()
    {
        var snap = PixelSnapCamera != null;
        if(snap)
            _snapLength = PixelSnapCamera.orthographicSize * PixelSnapIncrement / PixelSnapCamera.targetTexture.width;
        
        var x = CopyXPosition ? Target.position.x : transform.position.x;
        if (snap)
        {
            var x1 = (int) (x / _snapLength);
            x = x1 * _snapLength;
        }
        
        var z = CopyZPosition ? Target.position.z : transform.position.z;
        if (snap)
        {
            var z1 = (int) (z / _snapLength);
            z = z1 * _snapLength;
        }
        
        transform.position = new Vector3(x, CopyYPosition ? Target.position.y : transform.position.y, z);
        
        if (CopyRotation)
        {
            var flatForward = Target.forward;
            flatForward.y = 0;
            transform.rotation = Quaternion.LookRotation(Vector3.down, flatForward.normalized);
        }
    }
}
