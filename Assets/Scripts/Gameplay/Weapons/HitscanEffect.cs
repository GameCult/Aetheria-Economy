using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitscanEffect : MonoBehaviour
{
    public float Duration;
    public AnimationCurve IntensityCurve;
    public LineRenderer Line;
    public ParticleSystem LineEffect;
    public Prototype HitEffect;
    
    public Zone Zone { get; set; }
    
    public float Damage { get; set; }
    public float Penetration { get; set; }
    public float Spread { get; set; }
    public DamageType DamageType { get; set; }
    public Entity SourceEntity { get; set; }
    public float Range { get; set; }
    
    private float _startTime;
    private bool _active = false;
    
    public void Fire()
    {
        _startTime = Time.time;
        RaycastHit hit;
        if (Physics.Raycast(new Ray(transform.position, transform.forward), out hit, Range, 1))
        {
            var hull = hit.collider.GetComponent<HullCollider>();
            if (hull)
            {
                if (hull.Entity != SourceEntity)
                {
                    hull.SendHit(Damage, Penetration, Spread, DamageType, SourceEntity, hit, transform.forward);
                }
            }
            var length = (hit.point - transform.position).magnitude;
            Line.SetPosition(1, Vector3.forward * length);
            var emission = LineEffect.emission;
            emission.rateOverTimeMultiplier = length;
            var shape = LineEffect.shape;
            shape.position = Vector3.forward * (length / 2);
            shape.scale = Vector3.one * (length / 2);
        }
        else
        {
            Line.SetPosition(1, Vector3.forward * Range);
            var emission = LineEffect.emission;
            emission.rateOverTimeMultiplier = Range;
            var shape = LineEffect.shape;
            shape.position = Vector3.forward * (Range / 2);
            shape.scale = Vector3.one * (Range / 2);
        }
        LineEffect.Play(true);
        _active = true;
    }

    void Update()
    {
        if (!_active) return;
        var lerp = (Time.time - _startTime) / Duration;
        if (lerp > 1)
        {
            GetComponent<Prototype>().ReturnToPool();
            return;
        }
        
        Line.widthMultiplier = IntensityCurve.Evaluate(lerp);
    }
}
