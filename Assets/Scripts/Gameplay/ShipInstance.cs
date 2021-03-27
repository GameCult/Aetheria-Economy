using System.Collections;
using System.Linq;
using UnityEngine;

public class ShipInstance : EntityInstance
{
    private class ThrusterInstance
    {
        public Thruster Thruster;
        public GameObject SfxSource;
        public ParticleSystem System;
        public float BaseEmission;
        public int MaxParticleCount;
    }
    
    private ThrusterInstance[] _thrusters;
    
    public Ship Ship { get; private set; }

    public override void SetEntity(ZoneRenderer zoneRenderer, Entity entity)
    {
        base.SetEntity(zoneRenderer, entity);
        var ship = entity as Ship;
        if (ship == null)
        {
            Debug.LogError($"Attempted to assign non-ship entity to {gameObject.name} ship instance prefab!");
            return;
        }
        Ship = ship;
        _thrusters = ship.GetBehaviors<Thruster>().Select(thruster =>
            {
                var effectData = (ThrusterData) thruster.Data;
                var particles = Instantiate(UnityHelpers.LoadAsset<ParticleSystem>(effectData.ParticlesPrefab), transform, false);
                var particlesShape = particles.shape;
                var thrusterHardpoint = ThrusterHardpoints
                    .FirstOrDefault(t => t.name == thruster.Entity.Hardpoints[thruster.Item.Position.x, thruster.Item.Position.y].Transform);
                particlesShape.meshRenderer = thrusterHardpoint?.Emitter;
                // if (!string.IsNullOrEmpty(thruster.Item.Data.SoundEffectTrigger) && thrusterHardpoint != null)
                // {
                //     AkSoundEngine.RegisterGameObj(thrusterHardpoint.gameObject);
                //     AkSoundEngine.PostEvent(thruster.Item.Data.SoundEffectTrigger, thrusterHardpoint.gameObject);
                // }

                return new ThrusterInstance
                {
                    Thruster = thruster,
                    SfxSource = thrusterHardpoint?.gameObject,
                    System = particles,
                    BaseEmission = particles.emission.rateOverTimeMultiplier,
                    MaxParticleCount = 0
                };
            })
            .ToArray();

        foreach (var particle in _thrusters)
        {
            particle.System.gameObject.SetActive(false);
        }
    }

    protected override void ShowUnfadedElements()
    {
        base.ShowUnfadedElements();
        foreach (var particle in _thrusters)
        {
            particle.System.gameObject.SetActive(true);
        }
    }

    protected override void HideUnfadedElements()
    {
        base.HideUnfadedElements();
        foreach (var particle in _thrusters)
        {
            particle.System.gameObject.SetActive(false);
        }
    }

    public override void Update()
    {
        base.Update();
        
        foreach (var thrusterInstance in _thrusters)
        {
            var emissionModule = thrusterInstance.System.emission;
            var item = thrusterInstance.Thruster.Item.EquippableItem;
            var data = Entity.ItemManager.GetData(item);
            thrusterInstance.MaxParticleCount = thrusterInstance.System.particleCount;
            emissionModule.rateOverTimeMultiplier = thrusterInstance.BaseEmission * thrusterInstance.Thruster.Axis * (item.Durability / data.Durability);
            // if (thrusterInstance.SfxSource)
            // {
            //     var throttle = 0f;
            //     if (thrusterInstance.System.particleCount > 0)
            //         throttle = (float) thrusterInstance.System.particleCount / thrusterInstance.MaxParticleCount;
            //     AkSoundEngine.SetObjectPosition(thrusterInstance.SfxSource, thrusterInstance.SfxSource.transform);
            //     AkSoundEngine.SetRTPCValue("thruster_throttle", throttle, thrusterInstance.SfxSource);
            //     AkSoundEngine.SetRTPCValue("performance_durability", thrusterInstance.Thruster.Item.DurabilityPerformance, thrusterInstance.SfxSource);
            //     AkSoundEngine.SetRTPCValue("performance_thermal", thrusterInstance.Thruster.Item.ThermalPerformance, thrusterInstance.SfxSource);
            //     AkSoundEngine.SetRTPCValue("performance_quality", thrusterInstance.Thruster.Item.EquippableItem.Quality, thrusterInstance.SfxSource);
            // }
        }

        transform.rotation = Ship.Rotation;
    }

    public override void OnDestroy()
    {
        foreach (var thrusterInstance in _thrusters)
        {
            if (thrusterInstance.SfxSource)
            {
                AkSoundEngine.PostEvent(thrusterInstance.Thruster.Item.Data.SoundEffectTrigger + "_stop", thrusterInstance.SfxSource);
                AkSoundEngine.UnregisterGameObj(thrusterInstance.SfxSource);
            }
        }
    }
}