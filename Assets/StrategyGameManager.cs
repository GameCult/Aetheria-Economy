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
    private Dictionary<Guid, GalaxyResponseZone> _galaxy;
    private TabButton _currentTab;
    private bool _galaxyPopulated;
    
    void Start()
    {
        PrimaryTabGroup.OnTabChange += button =>
        {
            _currentTab = button;
            if (button == GalaxyTabButton)
            {
                
            }
        };
        CultClient.AddMessageListener<GalaxyResponseMessage>(galaxy =>
        {
            _galaxy = galaxy.Zones.ToDictionary(z=>z.ZoneID);
            if (_currentTab == GalaxyTabButton && !_galaxyPopulated)
            {
                _galaxyPopulated = true;
                
                GalaxyBackground.transform.localScale = Vector3.one * GalaxyScale;
                
                var galaxyMat = GalaxyBackground.material;
                galaxyMat.SetFloat("Arms", galaxy.GlobalData.Arms);
                galaxyMat.SetFloat("Twist", galaxy.GlobalData.Twist);
                galaxyMat.SetFloat("TwistPower", galaxy.GlobalData.TwistPower);
                galaxyMat.SetFloat("SpokeOffset", galaxy.StarDensity.SpokeOffset);
                galaxyMat.SetFloat("SpokeScale", galaxy.StarDensity.SpokeScale);
                galaxyMat.SetFloat("CoreBoost", galaxy.StarDensity.CoreBoost);
                galaxyMat.SetFloat("CoreBoostOffset", galaxy.StarDensity.CoreBoostOffset);
                galaxyMat.SetFloat("CoreBoostPower", galaxy.StarDensity.CoreBoostPower);
                galaxyMat.SetFloat("EdgeReduction", galaxy.StarDensity.EdgeReduction);
                galaxyMat.SetFloat("NoisePosition", galaxy.StarDensity.NoisePosition);
                galaxyMat.SetFloat("NoiseAmplitude", galaxy.StarDensity.NoiseAmplitude);
                galaxyMat.SetFloat("NoiseOffset", galaxy.StarDensity.NoiseOffset);
                galaxyMat.SetFloat("NoiseGain", galaxy.StarDensity.NoiseGain);
                galaxyMat.SetFloat("NoiseLacunarity", galaxy.StarDensity.NoiseLacunarity);
                galaxyMat.SetFloat("NoiseFrequency", galaxy.StarDensity.NoiseFrequency);
                
                var linkedZones = new List<Guid>();
                foreach (var zone in galaxy.Zones)
                {
                    var instance = GalaxyZonePrototype.Instantiate<Transform>();
                    instance.position = float3((Vector2) zone.Position - Vector2.one * (GalaxyScale * .5f),0) * GalaxyScale;
                    foreach (var linkedZone in zone.Links.Where(l=>linkedZones.Contains(l)))
                    {
                        var link = GalaxyZoneLinkPrototype.Instantiate<Transform>();
                        var diff = _galaxy[linkedZone].Position - zone.Position;
                        link.position = instance.position + Vector3.forward*.1f;
                        link.rotation = Quaternion.Euler(0,0,atan2(diff.y, diff.x) * Mathf.Rad2Deg);
                        link.localScale = new Vector3(length(diff) * GalaxyScale, 1, 1);
                    }
                }
            }
        });
        CultClient.Send(new GalaxyRequestMessage());
    }

    void Update()
    {
        
    }
}
