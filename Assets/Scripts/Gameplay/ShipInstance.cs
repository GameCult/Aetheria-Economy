using System.Collections;
using System.Linq;
using UnityEngine;

public class ShipInstance : EntityInstance
{
    public (Thruster effect, ParticleSystem system, float baseEmission)[] Particles;
    
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
        Particles = ship.GetBehaviors<Thruster>()
            .Select<Thruster, (Thruster effect, ParticleSystem system, float baseEmission)>(x =>
            {
                var effectData = (ThrusterData) x.Data;
                var particles = Instantiate(UnityHelpers.LoadAsset<ParticleSystem>(effectData.ParticlesPrefab), transform, false);
                var particlesShape = particles.shape;
                particlesShape.meshRenderer = ThrusterHardpoints
                    .FirstOrDefault(t => t.name == x.Entity.Hardpoints[x.Item.Position.x, x.Item.Position.y].Transform)
                    ?.Emitter;
                return (x, particles, particles.emission.rateOverTimeMultiplier);
            })
            .ToArray();

        foreach (var particle in Particles)
        {
            particle.system.gameObject.SetActive(false);
        }
    }

    protected override void ShowUnfadedElements()
    {
        base.ShowUnfadedElements();
        foreach (var particle in Particles)
        {
            particle.system.gameObject.SetActive(true);
        }
    }

    protected override void HideUnfadedElements()
    {
        base.HideUnfadedElements();
        foreach (var particle in Particles)
        {
            particle.system.gameObject.SetActive(false);
        }
    }

    public override void Update()
    {
        base.Update();
        
        foreach (var (effect, system, baseEmission) in Particles)
        {
            var emissionModule = system.emission;
            var item = effect.Item.EquippableItem;
            var data = Entity.ItemManager.GetData(item);
            emissionModule.rateOverTimeMultiplier = baseEmission * effect.Axis * (item.Durability / data.Durability);
        }

        transform.rotation = Ship.Rotation;
    }
}