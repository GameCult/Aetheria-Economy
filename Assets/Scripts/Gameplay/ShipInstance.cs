using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class ShipInstance : EntityInstance
{
    private static int _shipIndex;
    public TractorBeam TractorBeam;
    private class ThrusterInstance
    {
        public Thruster Thruster;
        public ParticleSystem System;
        public float BaseEmission;
        public int MaxParticleCount;
    }
    
    private ThrusterInstance[] _thrusters;
    
    public Ship Ship { get; private set; }

    private void Start()
    {
        var tractorBeamMain = TractorBeam.ParticleSystem.main;
        tractorBeamMain.customSimulationSpace = LocalSpace;
    }

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
                    .FirstOrDefault(t => t.name == ship.Hardpoints[thruster.Item.Position.x, thruster.Item.Position.y].Transform);
                particlesShape.meshRenderer = thrusterHardpoint?.Emitter;
                // if (!string.IsNullOrEmpty(thruster.Item.Data.SoundEffectTrigger) && thrusterHardpoint != null)
                // {
                //     AkSoundEngine.RegisterGameObj(thrusterHardpoint.gameObject);
                //     AkSoundEngine.PostEvent(thruster.Item.Data.SoundEffectTrigger, thrusterHardpoint.gameObject);
                // }

                return new ThrusterInstance
                {
                    Thruster = thruster,
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

        TractorBeam.Power = Entity.TractorPower;
        TractorBeam.Direction = Entity.LookDirection;
        
        foreach (var thrusterInstance in _thrusters)
        {
            var emissionModule = thrusterInstance.System.emission;
            var item = thrusterInstance.Thruster.Item.EquippableItem;
            var data = Entity.ItemManager.GetData(item);
            thrusterInstance.MaxParticleCount = thrusterInstance.System.particleCount;
            emissionModule.rateOverTimeMultiplier = thrusterInstance.BaseEmission * thrusterInstance.Thruster.Axis * (item.Durability / data.Durability);
        }

        transform.rotation = Ship.Rotation;
    }
}