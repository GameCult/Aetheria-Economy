using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using static Unity.Mathematics.noise;
using Random = UnityEngine.Random;
using static Noise1D;

public class GuidedProjectile : MonoBehaviour
{
    public Prototype HitEffect;
    public Prototype ChildProjectile;
    public int Children;
    public float SplitTime;
    public float SplitSeparationForwardness;
    public float SplitSeparationVelocity;
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
    private List<GuidedProjectile> _children = new List<GuidedProjectile>();

    public float3 StartPosition { get; set; }
    public float Range { get; set; }
    public Vector3 Velocity { get; set; }
    public float Damage { get; set; }
    public float Penetration { get; set; }
    public float Spread { get; set; }
    public DamageType DamageType { get; set; }
    public Entity SourceEntity { get; set; }

    void OnEnable()
    {
        _active = _alive = true;
        _phase = Random.value * 100;
        _prevDist = Single.MaxValue;
        Particles.startColor = Color.white;
        //var main = Particles.main;
        //main.startColor = new ParticleSystem.MinMaxGradient { mode = ParticleSystemGradientMode.Color, color = Color.white };
        Particles.Clear(true);
        Particles.Play(true);
    }
	
    void Update ()
    {
        if (SourceEntity == null) return;

        
        var t = transform;
        
        if (_active)
        {
            if (!Target)
            {
                StartCoroutine(FadeOut());
                return;
            }

            var diff = Target.position - transform.position;
            var targetDist = diff.magnitude;
            var position = (float3) t.position;
            var sourceDist = length(StartPosition.xz - position.xz);
            
            if (sourceDist > Range || _prevDist < targetDist)
            {
                StartCoroutine(FadeOut());
                if (HitEffect != null)
                {
                    var ht = HitEffect.Instantiate<Transform>();
                    ht.position = t.position;
                }
                return;
            }
            _prevDist = targetDist;
            
            var targetDistFlat = diff.Flatland().magnitude;
            var curveLerp = 1 - targetDistFlat / (sourceDist + targetDistFlat);
            var dir = diff.normalized;
            var right = cross(dir, float3(0, 1, 0));
            var up = cross(dir, right);

            if (Children > 0 && SplitTime < curveLerp)
            {
                for (int i = 0; i < Children; i++)
                {
                    var child = ChildProjectile.Instantiate<GuidedProjectile>();
                    child.StartPosition = child.transform.position = t.position;
                    _children.Add(child);
                    var randomDirection = normalize(Random.insideUnitCircle);
                    var perpendicularRandom = randomDirection.x * right + randomDirection.y * up;
                    child.Velocity = normalize(lerp(perpendicularRandom, dir, SplitSeparationForwardness)) * length(Velocity) * SplitSeparationVelocity;
                    child.Range = Range;
                    child.Damage = Damage;
                    child.Penetration = Penetration;
                    child.Spread = Spread;
                    child.DamageType = DamageType;
                    child.GuidanceCurve = GuidanceCurve;
                    child.LiftCurve = LiftCurve;
                    child.ThrustCurve = ThrustCurve;
                    child.Thrust = Thrust;
                    child.TopSpeed = TopSpeed;
                    child.Source = Source;
                    child.Target = Target;
                    child.SourceEntity = SourceEntity;
                    child.Frequency = Frequency;
                    child.Thrust = Thrust;
                }
                StartCoroutine(Kill());
                if (HitEffect != null)
                {
                    var ht = HitEffect.Instantiate<Transform>();
                    ht.position = t.position;
                }
            }
            
            var dodge = normalize(lerp(
                normalize(right * noise(Time.time * Frequency + _phase) + up * noise(Time.time * Frequency + (100 + _phase))),
                Vector3.up, LiftCurve.Evaluate(curveLerp)));
            var desired = Vector3.Slerp(dodge, dir, GuidanceCurve.Evaluate(curveLerp)).normalized * TopSpeed;
            var thrustCurve = ThrustCurve.Evaluate(curveLerp);
            var thrust = Thrust * thrustCurve;
            var c = Color.white * thrustCurve;
            c.a = 1;
            Particles.startColor = c;
            Velocity += (desired-Velocity).normalized * (thrust * Time.deltaTime);
        }

        if(_alive)
        {
            var ray = new Ray(t.position, Velocity);
            if (Physics.Raycast(ray, out var hit, Velocity.magnitude * Time.deltaTime, 1))
            {
                var hull = hit.collider.GetComponent<HullCollider>();
                if (hull)
                {
                    hull.SendHit(Damage, Penetration, Spread, DamageType, SourceEntity, hit, Velocity.normalized);
                }

                StartCoroutine(Kill());
                if (HitEffect != null)
                {
                    var ht = HitEffect.Instantiate<Transform>();
                    ht.SetParent(hit.collider.transform);
                    ht.position = hit.point;
                    return;
                }
            }

            t.position += Velocity * Time.deltaTime;
        }
    }

    IEnumerator FadeOut()
    {
        _active = false;
        var startTime = Time.time;
        while (Time.time - startTime < FadeOutTime)
        {
            var lerp = 1 - (Time.time - startTime) / FadeOutTime;
            var c = Color.white * lerp;
            c.a = 1;
            Particles.startColor = c;
            //var main = Particles.main;
            //main.startColor = new ParticleSystem.MinMaxGradient { mode = ParticleSystemGradientMode.Color, color = Color.white * lerp };
            yield return null;
        }

        StartCoroutine(Kill());
    }

    IEnumerator Kill()
    {
        _active = false;
        _alive = false;
        Particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        var startTime = Time.time;
        var lifetime = Particles.main.startLifetime.constant;
        while (Time.time - startTime < lifetime || _children.Count > 0)
        {
            for (var index = 0; index < _children.Count; index++)
            {
                if (!_children[index].gameObject.activeSelf)
                    _children.RemoveAt(index--);
            }

            yield return null;
        }
        GetComponent<Prototype>().ReturnToPool();
    }
}