using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ConstantParticleWeapon : MonoBehaviour
{
    public ParticleSystem[] Particles;
    
    public float Damage { get; set; }
    public DamageType DamageType { get; set; }
    public EntityInstance Source { get; set; }
    public EntityInstance Target { get; set; }

    private bool _stopping;
    private float _emission;
    private List<ParticleSystem.Particle> _collisionParticles = new List<ParticleSystem.Particle>();
    private HullCollider _hull;
    private Transform _simulationSpace;

    private void Start()
    {
        _emission = Particles[0].emission.rateOverTime.constant;
    }

    private void OnEnable()
    {
        foreach(var p in Particles)
            p.enableEmission = true;
        _stopping = false;
    }

    public void Initialize()
    {
        _simulationSpace = new GameObject($"{gameObject.name} Sim Space").transform;
        foreach(var p in Particles)
        {
            var main = p.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Custom;
            main.customSimulationSpace = _simulationSpace;
        }
        
        var trigger = Particles[0].trigger;
        while(trigger.colliderCount > 0)
            trigger.RemoveCollider(0);
        
        if (Target == null) return;
        
        foreach (var collider in Target.Prefab.HullColliders)
        {
            _hull = collider;
            trigger.AddCollider(collider.GetComponent<Collider>());
        }
    }

    private void Update()
    {
        if (Source == null) return;
        _simulationSpace.position = Source.Prefab.transform.position;
        if (_stopping && Particles.All(p=>p.particleCount == 0))
        {
            GetComponent<Prototype>().ReturnToPool();
            Destroy(_simulationSpace.gameObject);
            return;
        }
    }

    private void OnParticleTrigger()
    {
        if (Target == null || !_hull) return;
        _collisionParticles.Clear();
        Particles[0].GetTriggerParticles(ParticleSystemTriggerEventType.Enter, _collisionParticles);
        if(_collisionParticles.Count > 0)
        {
            _hull.SendSplash(
                Damage * Time.deltaTime,
                DamageType,
                Source.Entity,
                (transform.position - Target.Prefab.transform.position).normalized);
        }
    }

    public void Stop()
    {
        foreach(var p in Particles)
            p.enableEmission = false;
        _stopping = true;
    }
}
