using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using static Unity.Mathematics.noise;
using Random = UnityEngine.Random;

public class Lightning : MonoBehaviour
{
    public LightningCompute LightningCompute;
    public float HitRadius;
    
    public float Damage { get; set; }
    public float Penetration { get; set; }
    public float Spread { get; set; }
    public DamageType DamageType { get; set; }
    public EntityInstance Source { get; set; }
    public float Range { get; set; }
    public EntityInstance Target { get; set; }
    public Transform Barrel { get; set; }

    private bool _colliderHit;
    private Vector3 _colliderLocalPosition;
    private Transform _colliderTransform;
    private Vector3 _endpoint;

    public void Fire()
    {
        _colliderHit = false;
        LightningCompute.OnLeaderComplete = null;
        LightningCompute.FixedEndpoint = false;
        var hits = Physics.SphereCastAll(Barrel.position, HitRadius, Barrel.forward, Range, 1);
        foreach (var hit in hits)
        {
            var hull = hit.collider.GetComponent<HullCollider>();
            if (hull)
            {
                if (hull.Entity == Source.Entity) continue;
                LightningCompute.OnLeaderComplete = () => 
                    hull.SendHit(Damage, Penetration, Spread, DamageType, Source.Entity, hit, Barrel.forward);
            }

            _colliderHit = true;
            _colliderLocalPosition = hit.collider.transform.InverseTransformPoint(hit.point);
            _colliderTransform = hit.collider.transform;
            LightningCompute.FixedEndpoint = true;
            break;
        }
        LightningCompute.OnPulseComplete = () => 
            GetComponent<Prototype>().ReturnToPool();
        LightningCompute.StartAnimation();
        _endpoint = Barrel.position + Barrel.forward * Range;
    }

    private void Update()
    {
        if (Barrel == null) return;

        if (_colliderHit)
        {
            if (_colliderTransform) _endpoint = _colliderTransform.TransformPoint(_colliderLocalPosition);
        }

        LightningCompute.EndPosition = _endpoint;
        LightningCompute.StartPosition = Barrel.position;
    }
}
