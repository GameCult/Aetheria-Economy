using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrototypeParticleReturn : MonoBehaviour
{
    private void OnParticleSystemStopped()
    {
        GetComponent<Prototype>().ReturnToPool();
    }
}
