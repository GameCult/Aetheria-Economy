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
    public float GalaxyScale;
    public Prototype ZoneObjectPrototype;

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
                var linkedZones = new List<Guid>();
                foreach (var zone in galaxy.Zones)
                {
                    var instance = GalaxyZonePrototype.Instantiate<Transform>();
                    instance.position = float3(zone.Position,0) * GalaxyScale;
                    foreach (var linkedZone in zone.Links.Where(l=>linkedZones.Contains(l)))
                    {
                        var link = GalaxyZoneLinkPrototype.Instantiate<Transform>();
                        var diff = _galaxy[linkedZone].Position - zone.Position;
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
