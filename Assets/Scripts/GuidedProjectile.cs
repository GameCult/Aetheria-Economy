using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using static Unity.Mathematics.noise;
using Random = UnityEngine.Random;

public class GuidedProjectile : MonoBehaviour
{
    public ParticleSystem Particles;
    public float FadeOutTime;
    public AnimationCurve ThrustCurve;
    public AnimationCurve GuidanceCurve;
    public AnimationCurve LiftCurve;
    public float Frequency;
    public float Thrust;
    public float TopSpeed;
    public Transform Source;
    public Transform Target;
    
    private float _phase;
    private float _prevDist;
    private bool _active;
    private bool _alive;

    public Vector3 Velocity { get; set; }
    public float Damage { get; set; }
    public float Penetration { get; set; }
    public float Spread { get; set; }
    public DamageType DamageType { get; set; }
    public Entity SourceEntity { get; set; }

    void Awake()
    {
    }

    void OnEnable()
    {
        _active = _alive = true;
        _phase = Random.value * 100;
        _prevDist = Single.MaxValue;
        var main = Particles.main;
        main.startColor = Color.white;
        Particles.Clear(true);
        Particles.Play(true);
    }
	
    void Update ()
    {
        if (SourceEntity == null) return;
        
        var diff = Target.position - transform.position;
        var targetDist = diff.magnitude;
        
        if (_prevDist < targetDist)
        {
            StartCoroutine(FadeOut());
        }
        _prevDist = targetDist;
        
        var t = transform;
        
        if (_active)
        {
            var position = (float3) t.position;
            var sourceDist = length((float3) Source.position - position);
            var curveLerp = 1 - targetDist / (sourceDist + targetDist);
            var dir = diff.normalized;
            var right = cross(dir, float3(0, 1, 0));
            var up = cross(dir, right);
            var dodge = normalize(lerp(
                normalize(right * snoise(position * Frequency + float3(1) * _phase) +
                                up * snoise(position * Frequency + float3(1) * (100 + _phase))),
                Vector3.up, LiftCurve.Evaluate(curveLerp)));
            var desired = Vector3.Slerp(dodge, dir, GuidanceCurve.Evaluate(curveLerp)).normalized * TopSpeed;
            var thrust = Thrust * ThrustCurve.Evaluate(curveLerp);
            Velocity += (desired-Velocity) * (thrust * Time.deltaTime);
        }

        var ray = new Ray(t.position, Velocity);
        if (Physics.Raycast(ray, out var hit, Velocity.magnitude, LayerMask.NameToLayer("Combat")))
        {
            var hull = hit.collider.GetComponent<HullCollider>();
            if (hull)
            {
                hull.SendHit(Damage, Penetration, Spread, DamageType, SourceEntity, hit, Velocity.normalized);
            }
            StartCoroutine(Kill());
        }
        if(_alive)
            t.position += Velocity * Time.deltaTime;
    }

    IEnumerator FadeOut()
    {
        _active = false;
        var startTime = Time.time;
        while (Time.time - startTime < FadeOutTime)
        {
            var lerp = 1 - (Time.time - startTime) / FadeOutTime;
            var main = Particles.main;
            main.startColor = Color.white * lerp;
            yield return null;
        }

        StartCoroutine(Kill());
    }

    IEnumerator Kill()
    {
        _alive = false;
        Particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        var startTime = Time.time;
        var lifetime = Particles.main.startLifetime.constant;
        while (Time.time - startTime < lifetime)
            yield return null;
        GetComponent<Prototype>().ReturnToPool();
    }
}