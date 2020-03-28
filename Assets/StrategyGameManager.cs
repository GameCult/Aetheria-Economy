using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public class StrategyGameManager : MonoBehaviour
{
    public TabGroup PrimaryTabGroup;
    public TabButton GalaxyTabButton;
    public TabButton ZoneTabButton;
    public Prototype GalaxyZonePrototype;
    public Prototype GalaxyZoneLinkPrototype;
    public MeshRenderer GalaxyBackground;
    public float GalaxyScale;
    public Prototype ZoneObjectPrototype;
    public Texture2D PlanetSprite;
    public Texture2D GasGiantSprite;
    public Texture2D OrbitalSprite;
    public Texture2D SunSprite;
    public Texture2D WormholeSprite;

    private DatabaseCache _cache;
    private GameContext _context;
    private GalaxyResponseMessage _galaxy;
    private TabButton _currentTab;
    private bool _galaxyPopulated;
    
    void Start()
    {
        PrimaryTabGroup.OnTabChange += button =>
        {
            _currentTab = button;
            if (button == GalaxyTabButton && !_galaxyPopulated)
            {
                PopulateGalaxy();
            }
        };
        CultClient.AddMessageListener<GalaxyResponseMessage>(galaxy =>
        {
            _galaxy = galaxy;
            if (_currentTab == GalaxyTabButton && !_galaxyPopulated)
            {
                PopulateGalaxy();
            }
        });
        CultClient.Send(new GalaxyRequestMessage());
    }

    void PopulateGalaxy()
    {
        _galaxyPopulated = true;
        
        GalaxyBackground.transform.localScale = Vector3.one * GalaxyScale;
        
        var galaxyMat = GalaxyBackground.material;
        galaxyMat.SetFloat("Arms", _galaxy.GlobalData.Arms);
        galaxyMat.SetFloat("Twist", _galaxy.GlobalData.Twist);
        galaxyMat.SetFloat("TwistPower", _galaxy.GlobalData.TwistPower);
        galaxyMat.SetFloat("SpokeOffset", _galaxy.StarDensity.SpokeOffset);
        galaxyMat.SetFloat("SpokeScale", _galaxy.StarDensity.SpokeScale);
        galaxyMat.SetFloat("CoreBoost", _galaxy.StarDensity.CoreBoost);
        galaxyMat.SetFloat("CoreBoostOffset", _galaxy.StarDensity.CoreBoostOffset);
        galaxyMat.SetFloat("CoreBoostPower", _galaxy.StarDensity.CoreBoostPower);
        galaxyMat.SetFloat("EdgeReduction", _galaxy.StarDensity.EdgeReduction);
        galaxyMat.SetFloat("NoisePosition", _galaxy.StarDensity.NoisePosition);
        galaxyMat.SetFloat("NoiseAmplitude", _galaxy.StarDensity.NoiseAmplitude);
        galaxyMat.SetFloat("NoiseOffset", _galaxy.StarDensity.NoiseOffset);
        galaxyMat.SetFloat("NoiseGain", _galaxy.StarDensity.NoiseGain);
        galaxyMat.SetFloat("NoiseLacunarity", _galaxy.StarDensity.NoiseLacunarity);
        galaxyMat.SetFloat("NoiseFrequency", _galaxy.StarDensity.NoiseFrequency);
        
        var zones = _galaxy.Zones.ToDictionary(z=>z.ZoneID);
        var linkedZones = new List<Guid>();
        foreach (var zone in _galaxy.Zones)
        {
            linkedZones.Add(zone.ZoneID);
            var instance = GalaxyZonePrototype.Instantiate<Transform>();
            instance.position = float3((Vector2) zone.Position - Vector2.one * .5f,0) * GalaxyScale;
            instance.GetComponent<GalaxyZone>().Label.text = zone.Name;
            foreach (var linkedZone in zone.Links.Where(l=>!linkedZones.Contains(l)))
            {
                var link = GalaxyZoneLinkPrototype.Instantiate<Transform>();
                var diff = zones[linkedZone].Position - zone.Position;
                link.position = instance.position + Vector3.forward*.1f;
                link.rotation = Quaternion.Euler(0,0,atan2(diff.y, diff.x) * Mathf.Rad2Deg);
                link.localScale = new Vector3(length(diff) * GalaxyScale, 1, 1);
            }
        }
    }
}
