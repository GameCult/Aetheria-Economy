/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.noise;
using static Unity.Mathematics.math;

public class CameraPerlinOrbit : MonoBehaviour
{
    public Transform Target;
    public float Distance;
    public float MinElevation = .25f;
    public float MaxElevation = .5f;
    public float Frequency = .1f;
    public float VerticalLookOffset;
    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = normalize(float3(
            snoise(float2(0, Time.time * Frequency)),
            smoothstep(snoise(float2(10, Time.time * Frequency)), -1, 1) * (MaxElevation - MinElevation) + MinElevation,
            snoise(float2(20, Time.time * Frequency)))) * Distance + (float3) Target.position;
        transform.LookAt(Target.position + Vector3.up * VerticalLookOffset);
    }
}
