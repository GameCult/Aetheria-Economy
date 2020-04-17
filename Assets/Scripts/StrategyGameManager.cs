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
    public TabButton TechTabButton;
    public TechTreeMsagl TechTree;
    public Prototype GalaxyZonePrototype;
    public Prototype GalaxyZoneLinkPrototype;
    public MeshRenderer GalaxyBackground;
    public float GalaxyScale;
    public ZoneObject ZoneObjectPrefab;
    public ParticleSystem BeltPrefab;
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
    public MeshRenderer ZoneBackground;
    public MeshRenderer ZoneBoundary;
    public bool TestMode;

    private DatabaseCache _cache;
    private GameContext _context;
    private GalaxyResponseMessage _galaxy;
    private TabButton _currentTab;
    private bool _galaxyPopulated;
    private Guid _populatedZone;
    private Guid _selectedZone;
    private GalaxyZone _selectedGalaxyZone;
    private Dictionary<Guid, GalaxyResponseZone> _galaxyResponseZones;
    private Dictionary<Guid, GalaxyZone> _galaxyZoneObjects = new Dictionary<Guid, GalaxyZone>();
    private Dictionary<OrbitalEntity, ZoneObject> _zoneObjects = new Dictionary<OrbitalEntity, ZoneObject>();
    private Dictionary<OrbitalEntity, ParticleSystem> _zoneBelts = new Dictionary<OrbitalEntity, ParticleSystem>();
    private List<ZoneObject> _wormholes = new List<ZoneObject>();
    private Vector3 _galaxyCameraPos = -Vector3.forward;
    private float _galaxyOrthoSize = 50;
    private Material _boundaryMaterial;
    private Material _backgroundMaterial;
    private bool _techLayoutGenerated;
    
    void Start()
    {
        _galaxyOrthoSize = GalaxyScale / 2;
        _cache = new DatabaseCache();

        if (TestMode)
        {
            
        }
        else
        {
            // Request galaxy description from the server
            CultClient.Send(new GalaxyRequestMessage());
        
            // Request blueprints from the server
            CultClient.Send(new BlueprintsRequestMessage());
        
            // Listen for the server's galaxy description
            CultClient.AddMessageListener<GalaxyResponseMessage>(galaxy =>
            {
                _galaxy = galaxy;
                _galaxyResponseZones = _galaxy.Zones.ToDictionary(z => z.ZoneID);
                _cache.Add(_galaxy.GlobalData, true);
                _context = new GameContext(_cache, Debug.Log);
                if (_currentTab == GalaxyTabButton && !_galaxyPopulated) PopulateGalaxy();
            });
        
            CultClient.AddMessageListener<BlueprintsResponseMessage>(response => _cache.AddAll(response.Blueprints, true));
        
            // Listen for zone descriptions from the server
            CultClient.AddMessageListener<ZoneResponseMessage>(zone =>
            {
                // Cache zone data and contents
                _cache.Add(zone.Zone);
                foreach (var entry in zone.Contents) _cache.Add(entry);
            
                if (_currentTab == ZoneTabButton && _populatedZone != zone.Zone.ID) PopulateZone();
            });
        }
        
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
                if (_populatedZone != _selectedZone && _cache.Get<ZoneData>(_selectedZone) != null)
                    PopulateZone();
                Camera.transform.position = -Vector3.forward;
            }

            if (_currentTab == TechTabButton)
            {
                if (!_techLayoutGenerated)
                {
                    TechTree.Initialize();
                    TechTree.Blueprints = _cache.GetAll<BlueprintData>().ToArray();
                    TechTree.GenerateTechs();
                }
            }
        };
        
        _boundaryMaterial = ZoneBoundary.material;
        _backgroundMaterial = ZoneBackground.material;
    }

    private void Update()
    {
        if (_context != null)
        {
            _context.Time = Time.time;
            _context.Update();
        }
        if (_currentTab == GalaxyTabButton)
        {
            _galaxyCameraPos = Camera.transform.position;
            _galaxyOrthoSize = Camera.orthographicSize;
        }
        if (_currentTab == ZoneTabButton && _populatedZone == _selectedZone)
        {
            foreach (var planet in _zoneObjects)
            {
                planet.Value.transform.position = (Vector2) planet.Key.Position * ZoneSizeScale;
            }

            foreach (var belt in _zoneBelts)
            {
                belt.Value.transform.position = float3(belt.Key.Position * ZoneSizeScale, .15f);
            }
        }
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
            _galaxyZoneObjects[zone.ZoneID] = instanceZone;
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
        foreach (var belt in _zoneBelts.Values) Destroy(belt.gameObject);
        _zoneBelts.Clear();

        if(_populatedZone!=Guid.Empty)
            _context.UnloadZone(_cache.Get<ZoneData>(_populatedZone));
        
        _populatedZone = _selectedZone;

        var zoneData = _cache.Get<ZoneData>(_populatedZone);
        var zoneContents = _context.InitializeZone(_populatedZone);
        
        float zoneDepth = 0;

        foreach (var kvp in zoneContents)
        {
            var entity = kvp.Value;
            if (kvp.Key is PlanetData planetData)
            {
                if (!planetData.Belt)
                {
                    var planetObject = Instantiate(ZoneObjectPrefab, ZoneRoot);
                    _zoneObjects[entity as OrbitalEntity] = planetObject;
                    planetObject.Label.text = planetData.Name;
                    planetObject.GravityMesh.gameObject.SetActive(true);
                    planetObject.GravityMesh.transform.localScale =
                        Vector3.one * (pow(planetData.Mass, _context.GlobalData.GravityRadiusExponent) *
                                       _context.GlobalData.GravityRadiusMultiplier * 2 * ZoneSizeScale);
                    var depth = pow(planetData.Mass, ZoneMassPower) * ZoneMassScale;
                    if (depth > zoneDepth)
                        zoneDepth = depth;
                    planetObject.GravityMesh.material.SetFloat("_Depth", depth);
                    planetObject.Icon.material.SetTexture("_MainTex",
                        planetData.Mass > _context.GlobalData.SunMass ? SunSprite :
                        planetData.Mass > _context.GlobalData.GasGiantMass ? GasGiantSprite :
                        PlanetSprite);
                }
                else
                {
                    var beltObject = Instantiate(BeltPrefab, ZoneRoot);
                    var orbit = _cache.Get<OrbitData>(planetData.Orbit);
                    _zoneBelts[entity as OrbitalEntity] = beltObject;
                    var beltEmission = beltObject.emission;
                    beltEmission.rateOverTime = 
                        new ParticleSystem.MinMaxCurve(
                            pow(planetData.Mass,_context.GlobalData.BeltMassExponent)
                            / _context.GlobalData.BeltMassRatio * orbit.Distance);
                    var beltShape = beltObject.shape;
                    beltShape.radius = orbit.Distance * ZoneSizeScale;
                    beltShape.donutRadius = orbit.Distance * ZoneSizeScale / 2;
                    var beltVelocity = beltObject.velocityOverLifetime;
                    beltVelocity.orbitalZ = orbit.Distance * ZoneSizeScale / orbit.Period;
                    beltObject.Simulate(60);
                    beltObject.Play();
                }
            }
        }

        var radius = (zoneData.Radius * ZoneSizeScale * 2);
        _backgroundMaterial.SetFloat("_ClipDistance", radius);
        _backgroundMaterial.SetFloat("_HeightRange", zoneDepth + _boundaryMaterial.GetFloat("_Depth"));
        ZoneBoundary.transform.localScale = Vector3.one * radius;
        
        foreach (var wormhole in zoneData.Wormholes)
        {
            var otherZone = _galaxyResponseZones[wormhole];
            var wormholeObject = Instantiate(ZoneObjectPrefab, ZoneRoot);
            _wormholes.Add(wormholeObject);
            var direction = otherZone.Position - zoneData.Position;
            wormholeObject.transform.position = (Vector2) (normalize(direction) * zoneData.Radius * .95f * ZoneSizeScale);
            wormholeObject.GravityMesh.gameObject.SetActive(false);
            wormholeObject.Icon.material.SetTexture("_MainTex", WormholeSprite);
            wormholeObject.Label.text = otherZone.Name;
            wormholeObject.Icon.GetComponent<ClickableCollider>().OnClick += (_, pointer) =>
            {
                // If the user double clicks on a wormhole, switch to that zone
                if (pointer.clickCount == 2)
                {
                    _selectedGalaxyZone.Background.material.SetColor("_TintColor", UnselectedColor);
                    if(_selectedZone != wormhole)
                        CultClient.Send(new ZoneRequestMessage{ZoneID = wormhole});
                    _selectedZone = wormhole;
                    _selectedGalaxyZone = _galaxyZoneObjects[wormhole];
                    _selectedGalaxyZone.Background.material.SetColor("_TintColor", SelectedColor);
                    
                }
            };
        }
    }
}
