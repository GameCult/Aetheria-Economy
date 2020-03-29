using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;

public class StrategyGameManager : MonoBehaviour
{
    public Camera Camera;
    public TabGroup PrimaryTabGroup;
    public TabButton GalaxyTabButton;
    public TabButton ZoneTabButton;
    public Prototype GalaxyZonePrototype;
    public Prototype GalaxyZoneLinkPrototype;
    public MeshRenderer GalaxyBackground;
    public float GalaxyScale;
    public ZoneObject ZoneObjectPrefab;
    public Transform ZoneRoot;
    public Texture2D PlanetSprite;
    public Texture2D GasGiantSprite;
    public Texture2D OrbitalSprite;
    public Texture2D SunSprite;
    public Texture2D WormholeSprite;
    public Color SelectedColor;
    public Color UnselectedColor;
    public float ZoneSizeScale = .01f;
    public float ZoneMassScale = .01f;
    public float ZoneMassPower = .5f;
    public float ZoneOrbitSpeed = 10;
    public MeshRenderer ZoneBackground;

    private DatabaseCache _cache;
    private GameContext _context;
    private GalaxyResponseMessage _galaxy;
    private TabButton _currentTab;
    private bool _galaxyPopulated;
    private Guid _populatedZone;
    private Guid _selectedZone;
    private GalaxyZone _selectedGalaxyZone;
    private ZoneResponseMessage _zoneResponse;
    private Dictionary<Guid, GalaxyResponseZone> _galaxyResponseZones;
    private Dictionary<Guid, ZoneObject> _zoneObjects = new Dictionary<Guid, ZoneObject>();
    private List<ZoneObject> _wormholes = new List<ZoneObject>();
    private Dictionary<Guid, float2> _orbitPositions = new Dictionary<Guid, float2>();
    private Vector3 _galaxyCameraPos = -Vector3.forward;
    private float _galaxyOrthoSize = 50;
    
    void Start()
    {
        _cache = new DatabaseCache();
        
        // Request galaxy description from the server
        CultClient.Send(new GalaxyRequestMessage());
        
        // Listen for the server's galaxy description
        CultClient.AddMessageListener<GalaxyResponseMessage>(galaxy =>
        {
            _galaxy = galaxy;
            _galaxyResponseZones = _galaxy.Zones.ToDictionary(z => z.ZoneID);
            _cache.Add(_galaxy.GlobalData, true);
            _context = new GameContext(_cache);
            if (_currentTab == GalaxyTabButton && !_galaxyPopulated) PopulateGalaxy();
        });
        
        // Listen for zone descriptions from the server
        CultClient.AddMessageListener<ZoneResponseMessage>(zone =>
        {
            _zoneResponse = zone;
            
            // Cache zone data and contents
            _cache.Add(zone.Zone);
            foreach (var entry in zone.Contents) _cache.Add(entry);
            
            if (_currentTab == ZoneTabButton && _populatedZone != zone.Zone.ID) PopulateZone();
        });
        
        PrimaryTabGroup.OnTabChange += button =>
        {
            _currentTab = button;
            if (_currentTab == GalaxyTabButton)
            {
                if (!_galaxyPopulated) PopulateGalaxy();
                Camera.transform.position = _galaxyCameraPos;
                Camera.orthographicSize = _galaxyOrthoSize;
            }

            if (_currentTab == ZoneTabButton)
            {
                if (_populatedZone != _selectedZone && _zoneResponse?.Zone.ID == _selectedZone)
                    PopulateZone();
                Camera.transform.position = -Vector3.forward;
            }
        };
    }

    private void Update()
    {
        if (_currentTab == GalaxyTabButton)
        {
            _galaxyCameraPos = Camera.transform.position;
            _galaxyOrthoSize = Camera.orthographicSize;
        }
        if (_currentTab == ZoneTabButton && _populatedZone == _selectedZone)
        {
            _orbitPositions.Clear();
            foreach (var planet in _zoneObjects)
            {
                var planetData = _cache.Get<PlanetData>(planet.Key);
                
                planet.Value.transform.position = (Vector2) GetOrbitPosition(planetData.Orbit) * ZoneSizeScale;
            }
        }
    }

    // Determine orbital position recursively, caching parent positions to avoid repeated calculations
    private float2 GetOrbitPosition(Guid orbit)
    {
        // Root orbit is fixed at center
        if(orbit==Guid.Empty)
            return float2.zero;
        
        if (!_orbitPositions.ContainsKey(orbit))
        {
            var orbitData = _cache.Get<OrbitData>(orbit);
            _orbitPositions[orbit] = GetOrbitPosition(orbitData.Parent) + (orbitData.Period < .01f ? float2.zero : 
                                     OrbitData.Evaluate(Time.time * ZoneOrbitSpeed / orbitData.Period + orbitData.Phase) *
                                     orbitData.Distance);
        }

        var position = _orbitPositions[orbit];
        if (float.IsNaN(position.x))
        {
            Debug.Log("Orbit position is NaN, something went very wrong!");
            return float2.zero;
        }
        return _orbitPositions[orbit];
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
            var instanceZone = instance.GetComponent<GalaxyZone>();
            instanceZone.Label.text = zone.Name;
            instanceZone.Background.GetComponent<ClickableCollider>().OnClick += (_, pointer) =>
            {
                if (_selectedGalaxyZone != null)
                    _selectedGalaxyZone.Background.material.SetColor("_TintColor", UnselectedColor);
                if(_selectedZone != zone.ZoneID)
                    CultClient.Send(new ZoneRequestMessage{ZoneID = zone.ZoneID});
                _selectedZone = zone.ZoneID;
                _selectedGalaxyZone = instanceZone;
                _selectedGalaxyZone.Background.material.SetColor("_TintColor", SelectedColor);
                _selectedZone = zone.ZoneID;
                
                // If the user double clicks on a zone, switch to the zone tab
                if(pointer.clickCount == 2) ZoneTabButton.OnPointerClick(pointer);
            };
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

    void PopulateZone()
    {
        // Delete all the existing zone contents
        foreach (var zoneObject in _zoneObjects.Values) Destroy(zoneObject.gameObject);//zoneObject.GetComponent<Prototype>().ReturnToPool();
        _zoneObjects.Clear();
        foreach (var zoneObject in _wormholes) Destroy(zoneObject.gameObject); //zoneObject.GetComponent<Prototype>().ReturnToPool();
        _wormholes.Clear();

        _populatedZone = _selectedZone;

        float zoneDepth = 0;
        foreach (var entry in _zoneResponse.Contents)
        {
            if (entry is PlanetData planet)
            {
                var planetObject = Instantiate(ZoneObjectPrefab, ZoneRoot);
                _zoneObjects[planet.ID] = planetObject;
                planetObject.Label.text = planet.Name;
                planetObject.GravityMesh.gameObject.SetActive(true);
                planetObject.GravityMesh.transform.localScale = Vector3.one * (planet.GravityRadius * 2 * ZoneSizeScale);
                var depth = pow(planet.Mass, ZoneMassPower) * ZoneMassScale;
                if (depth > zoneDepth)
                    zoneDepth = depth;
                planetObject.GravityMesh.material.SetFloat("_Depth", depth);
                planetObject.Icon.material.SetTexture("_MainTex",
                    planet.Mass > _context.GlobalData.SunMass ? SunSprite :
                    planet.Mass > _context.GlobalData.GasGiantMass ? GasGiantSprite :
                    PlanetSprite);
            }
        }
        ZoneBackground.material.SetFloat("_HeightRange", zoneDepth);
        
        foreach (var wormhole in _zoneResponse.Zone.Wormholes)
        {
            var otherZone = _galaxyResponseZones[wormhole];
            var wormholeObject = Instantiate(ZoneObjectPrefab);
            _wormholes.Add(wormholeObject);
            var direction = otherZone.Position - _zoneResponse.Zone.Position;
            wormholeObject.transform.position = (Vector2) (normalize(direction) * _zoneResponse.Zone.Radius * .95f * ZoneSizeScale);
            wormholeObject.GravityMesh.gameObject.SetActive(false);
            wormholeObject.Icon.material.SetTexture("_MainTex", WormholeSprite);
            wormholeObject.Label.text = otherZone.Name;
        }
    }
}
