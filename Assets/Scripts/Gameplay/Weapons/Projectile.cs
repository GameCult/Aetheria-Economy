using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public TrailRenderer Trail;
    public float Gravity;
    public Prototype HitEffect;
    
    private bool _alive;
    
    public Zone Zone { get; set; }
    
    public Vector3 StartPosition { get; set; }
    public Vector3 Velocity { get; set; }
    public float Damage { get; set; }
    public float Penetration { get; set; }
    public float Spread { get; set; }
    public DamageType DamageType { get; set; }
    public Entity SourceEntity { get; set; }
    public float Range { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnEnable()
    {
        _alive = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (SourceEntity == null) return;
        
        if(_alive)
        {
            var t = transform;
            var position = t.position;
            Velocity += Vector3.up * (Gravity * Time.deltaTime);
            var forward = Velocity.normalized;
            t.forward = forward;
            var ray = new Ray(position, Velocity);
            if (Physics.Raycast(ray, out var hit, Velocity.magnitude * Time.deltaTime, 1))
            {
                var hull = hit.collider.GetComponent<HullCollider>();
                if (hull)
                {
                    if (hull.Entity != SourceEntity)
                    {
                        hull.SendHit(Damage, Penetration, Spread, DamageType, SourceEntity, hit, forward);
                        transform.position = hit.point;
                        StartCoroutine(Kill());
                    }
                }
                else// if (hit.transform.gameObject.layer == 1)
                {
                    StartCoroutine(Kill());
                    return;
                }
                
                if (HitEffect != null)
                {
                    var ht = HitEffect.Instantiate<Transform>();
                    ht.SetParent(hit.collider.transform);
                    ht.position = hit.point;
                    return;
                }
            }
            
            transform.position += Velocity * Time.deltaTime;
            if((transform.position - StartPosition).magnitude > Range)
                StartCoroutine(Kill());
        }
    }

    IEnumerator Kill()
    {
        _alive = false;
        var startTime = Time.time;
        var lifetime = Trail.time;
        while (Time.time - startTime < lifetime)
            yield return null;
        GetComponent<Prototype>().ReturnToPool();
    }
}
