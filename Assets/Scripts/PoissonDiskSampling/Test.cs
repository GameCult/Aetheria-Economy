using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    public float radius = 1;
    public Vector2 regionSize = Vector2.one;
    public int rejectionSamples = 30;
    public float displayRadius = 1;

    private List<Vector2> points;

    private void OnValidate()
    {
        points = PoissonDiskSampling.PoissonGenerate(radius, regionSize, rejectionSamples);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(regionSize/2, regionSize);
        
        if (points != null)
        {
            foreach (Vector2 p in points)
            {
                Gizmos.DrawSphere(p, displayRadius);
            }
        }
    }
}
