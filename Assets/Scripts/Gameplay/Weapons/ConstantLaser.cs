using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstantLaser : MonoBehaviour
{
    public AnimationCurve StartCurve;
    public AnimationCurve EndCurve;
    public AnimationCurve IntensityCurve;
    public float StartDuration;
    public float FadeDuration;
    public float CycleDuration;
    public LineRenderer LineRenderer;
    
    public float Damage { get; set; }
    public float Penetration { get; set; }
    public float Spread { get; set; }
    public DamageType DamageType { get; set; }
    public Entity SourceEntity { get; set; }
    public float Range { get; set; }

    private float _intensity;
    private float _stopIntensity;
    private bool _starting;
    private bool _stopping;
    private float _startTime;
    private float _cycleStartTime;
    private readonly Vector3[] _zeros = {Vector3.zero, Vector3.zero};

    private void OnEnable()
    {
        _stopping = false;
        _starting = true;
        _startTime = Time.time;
        LineRenderer.SetPositions(_zeros);
    }

    private void Update()
    {
        if (_stopping)
        {
            var lerp = (Time.time - _startTime) / FadeDuration;
            LineRenderer.widthMultiplier = EndCurve.Evaluate(lerp) * _stopIntensity;
            
            if (lerp > 1)
            {
                GetComponent<Prototype>().ReturnToPool();
                return;
            }
        }
        else
        {
            var lerp = (Time.time - _startTime) / StartDuration;
            if (lerp > 1)
            {
                if (_starting)
                {
                    _starting = false;
                    _cycleStartTime = Time.time;
                }
                _intensity = IntensityCurve.Evaluate((Time.time - _cycleStartTime) / CycleDuration % CycleDuration);
                LineRenderer.widthMultiplier = _intensity;
            }
            else
            {
                LineRenderer.widthMultiplier = StartCurve.Evaluate(lerp);
            }
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
                    hull.SendHit(Damage * Time.deltaTime, Penetration, Spread, DamageType, SourceEntity, hit, transform.forward);
                }
            }
            LineRenderer.SetPosition(1, hit.point);
        }
        else
            LineRenderer.SetPosition(1, transform.position + transform.forward * Range);
    }

    public void Stop()
    {
        _stopping = true;
        _stopIntensity = _intensity;
        _startTime = Time.time;
    }
}
