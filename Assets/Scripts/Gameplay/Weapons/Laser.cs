using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Laser : MonoBehaviour
{
    public AnimationCurve IntensityCurve;
    public float Duration;
    public LineRenderer LineRenderer;
    
    public float Damage { get; set; }
    public float Penetration { get; set; }
    public float Spread { get; set; }
    public DamageType DamageType { get; set; }
    public Entity SourceEntity { get; set; }
    public float Range { get; set; }

    private float _startTime;
    private readonly Vector3[] _zeros = {Vector3.zero, Vector3.zero};

    private void OnEnable()
    {
        _startTime = Time.time;
        LineRenderer.SetPositions(_zeros);
    }

    private void Update()
    {
        var lerp = (Time.time - _startTime) / Duration;
        if (lerp > 1)
        {
            GetComponent<Prototype>().ReturnToPool();
            return;
        }
        
        LineRenderer.SetPosition(0, transform.position);
        RaycastHit hit;
        if (Physics.Raycast(new Ray(transform.position, transform.forward), out hit, Range, 1))
        {
            var hull = hit.collider.GetComponent<HullCollider>();
            if (hull)
            {
                if (hull.Entity != SourceEntity)
                {
                    hull.SendHit(Damage * (Time.deltaTime / Duration), Penetration, Spread, DamageType, SourceEntity, hit, transform.forward);
                }
            }
            LineRenderer.SetPosition(1, hit.point);
        }
        else
            LineRenderer.SetPosition(1, transform.position + transform.forward * Range);

        LineRenderer.widthMultiplier = IntensityCurve.Evaluate(lerp);
    }
}
