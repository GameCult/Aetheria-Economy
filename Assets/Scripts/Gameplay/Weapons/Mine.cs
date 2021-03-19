using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using UnityEngine;
using Random = UnityEngine.Random;

public class Mine : MonoBehaviour
{
    public MeshRenderer MeshRenderer;
    public int EmissionSubmesh;
    public AnimationCurve EmissionCurve;
    public float ActiveCycleDuration;
    public float ArmedCycleDuration;
    public float ActiveEmission;
    public float ArmedEmission;
    public float RotationSpeed;
    public float GridOffset;
    public float GridAttraction;
    public float Gravity;
    public float Drag = .1f;
    public float LaunchDrag;
    public Prototype HitEffect;
    public float Lifetime;

    public float ActivationDelay;
    public float BlastRange;
    public float BlastDelay;

    private float2 _selfVelocity;
    private float _startTime;
    private float _blastTime;
    private bool _blastCountdown;
    private bool _active;
    private Material _material;
    private float _pulseLerp;
    private float _emission;
    
    public Zone Zone { get; set; }
    public Vector3 Velocity { get; set; }
    public float Damage { get; set; }
    public DamageType DamageType { get; set; }
    public EntityInstance Source { get; set; }
    public float Range { get; set; }

    private void Start()
    {
        _material = MeshRenderer.materials[EmissionSubmesh];
    }

    private void OnEnable()
    {
        _startTime = Time.time;
        _blastCountdown = false;
        _active = false;
        _emission = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (Zone == null) return;

        var t = transform;
        var position = t.position;
        var gridHeight = Zone.GetHeight(position.Flatland()) + GridOffset;
        Velocity += Vector3.up * (sign(gridHeight - position.y) * GridAttraction * Time.deltaTime);
        Velocity *= max(0, 1 - LaunchDrag * Time.deltaTime);
        var normal = Zone.GetNormal(position.Flatland());
        var force = new float2(normal.x, normal.z);
        var forceMagnitude = lengthsq(force);
        if (forceMagnitude > .001f)
        {
            var fa = 1 / (1 - forceMagnitude) - 1;
            _selfVelocity += normalize(force) * Zone.Settings.GravityStrength * fa * Gravity;
        }
        _selfVelocity *= max(0, 1 - Drag * Time.deltaTime);
        t.position = position + (Velocity + new Vector3(_selfVelocity.x,0,_selfVelocity.y)) * Time.deltaTime;
        t.localRotation = Quaternion.Euler(sin(Time.time-_startTime * RotationSpeed) * 90, 0, cos(Time.time-_startTime * RotationSpeed) * 90);

        if (Time.time - _startTime > ActivationDelay && !_active)
        {
            _active = true;
        }
        
        if(_active)
        {
            _emission = lerp(_emission, _blastCountdown ? ArmedEmission : ActiveEmission, Time.deltaTime * 10);
            _pulseLerp += Time.deltaTime / (_blastCountdown ? ArmedCycleDuration : ActiveCycleDuration);
            _pulseLerp %= 1;
            if(!_blastCountdown)
            {
                foreach (var collider in Physics.OverlapSphere(t.position, BlastRange, 1))
                {
                    var hull = collider.GetComponent<HullCollider>();
                    if (hull)
                    {
                        _blastCountdown = true;
                        _blastTime = Time.time + BlastDelay;
                    }
                }
            }

            if ((position - Source.transform.position).magnitude > Range ||
                Time.time - _startTime > Lifetime ||
                _blastCountdown && Time.time > _blastTime)
            {
                foreach (var collider in Physics.OverlapSphere(t.position, BlastRange, 1 | (1 << 17)))
                {
                    var shield = collider.GetComponent<ShieldManager>();
                    if (shield && (shield.Entity.Shield != null && shield.Entity.Shield.Item.Active.Value && shield.Entity.Shield.CanTakeHit(DamageType, Damage)))
                    {
                        shield.Entity.Shield.TakeHit(DamageType, Damage);
                        shield.ShowHit(t.position, sqrt(Damage));
                    }
                    var hull = collider.GetComponent<HullCollider>();
                    if (hull && !(hull.Entity.Shield != null && hull.Entity.Shield.Item.Active.Value && hull.Entity.Shield.CanTakeHit(DamageType, Damage)))
                    {
                        hull.SendSplash(Damage, DamageType, Source.Entity, (collider.transform.position - t.position).normalized);
                    }
                }

                var ht = HitEffect.Instantiate<Transform>();
                ht.position = t.position;
                GetComponent<Prototype>().ReturnToPool();
            }
        }
        _material.SetFloat("_Emission", _emission * EmissionCurve.Evaluate(_pulseLerp));
    }
}
