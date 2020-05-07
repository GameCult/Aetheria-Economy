using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NaughtyAttributes;
using RethinkDb.Driver.Net;
using TMPro;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;
using Random = UnityEngine.Random;

public class StrategyGameManager : MonoBehaviour
{
    public bool TestMode;
    
    [Section("Scene Links")]
    public Camera Camera;
    public TechTreeMsagl TechTree;
    public MeshRenderer GalaxyBackground;
    public Transform ZoneRoot;
    public MeshRenderer ZoneBackground;
    public MeshRenderer ZoneBoundary;
    public ClickRaycaster ClickRaycaster;
    public ReadOnlyPropertiesPanel PropertiesPanel;
    
    [Section("Tab Links")]
    public TabGroup PrimaryTabGroup;
    public TabButton GalaxyTabButton;
    public TabButton ZoneTabButton;
    public TabButton TechTabButton;
    
    [Section("Prefabs & Prototypes")]
    public Prototype GalaxyZonePrototype;
    public Prototype GalaxyZoneLinkPrototype;
    public ZoneObject ZoneObjectPrefab;
    public ZoneShip ZoneShipPrefab;
    public MeshFilter BeltPrefab;
    
    [Section("Textures")]
    public Texture2D PlanetoidSprite;
    public Texture2D PlanetSprite;
    public Texture2D GasGiantSprite;
    public Texture2D OrbitalSprite;
    public Texture2D SunSprite;
    public Texture2D WormholeSprite;
    
    [Section("UI Properties")]
    public Color SelectedColor;
    public Color UnselectedColor;
    public float GalaxyScale;
    public float ZoneSizeScale = .01f;
    public float ZoneMassScale = .01f;
    public float ZoneMassPower = .5f;
    public int AsteroidSpritesheetWidth = 4;
    public int AsteroidSpritesheetHeight = 4;

    private DatabaseCache _cache;
    private GameContext _context;
    // private GalaxyResponseMessage _galaxyResponse;
    private TabButton _currentTab;
    private bool _galaxyPopulated;
    private Guid _populatedZone;
    private Guid _selectedZone;
    private GalaxyZone _selectedGalaxyZone;
    private Dictionary<Guid, GalaxyResponseZone> _galaxyResponseZones;
    private Dictionary<Guid, GalaxyZone> _galaxyZoneObjects = new Dictionary<Guid, GalaxyZone>();
    private Dictionary<Guid, ZoneObject> _zoneObjects = new Dictionary<Guid, ZoneObject>(); // Key is Orbit
    private Dictionary<Guid, AsteroidBelt> _zoneBelts = new Dictionary<Guid, AsteroidBelt>();
    private Dictionary<Ship, ZoneShip> _zoneShips = new Dictionary<Ship, ZoneShip>();
    private List<ZoneObject> _wormholes = new List<ZoneObject>();
    private Vector3 _galaxyCameraPos = -Vector3.forward;
    private float _galaxyOrthoSize = 50;
    private Material _boundaryMaterial;
    private Material _backgroundMaterial;
    private bool _techLayoutGenerated;
    private Connection _connection;
    
    void Start()
    {
        ConsoleController.MessageReceiver = this;
        _galaxyOrthoSize = GalaxyScale / 2;
        _cache = new DatabaseCache();

        if (TestMode)
        {
            _cache.Load(new DirectoryInfo(Application.dataPath).Parent.FullName);
            _context = new GameContext(_cache, Debug.Log);
            TechTree.Context = _context;
            _context.MapLayers = _cache.GetAll<GalaxyMapLayerData>().ToDictionary(ml => ml.Name);
            _galaxyResponseZones = _cache.GetAll<ZoneData>()
                .ToDictionary(z => z.ID, z => new GalaxyResponseZone
                {
                    Links = z.Wormholes.ToArray(),
                    Name = z.Name,
                    Position = z.Position,
                    ZoneID = z.ID
                });
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
                _galaxyResponseZones = galaxy.Zones.ToDictionary(z => z.ZoneID);
                _cache.Add(galaxy.GlobalData, true);
                _context = new GameContext(_cache, Debug.Log);
                TechTree.Context = _context;
                _context.MapLayers["StarDensity"] = galaxy.StarDensity;
                if (_currentTab == GalaxyTabButton && !_galaxyPopulated) PopulateGalaxy();
            });
        
            CultClient.AddMessageListener<BlueprintsResponseMessage>(response => _cache.AddAll(response.Blueprints, true));
        
            // Listen for zone descriptions from the server
            CultClient.AddMessageListener<ZoneResponseMessage>(zone =>
            {
                // Cache zone data and contents
                _cache.Add(zone.Zone);
                foreach (var entry in zone.Contents) _cache.Add(entry);
            
                if(_currentTab==GalaxyTabButton && _selectedZone == zone.Zone.ID)
                    PopulateZoneProperties(_selectedZone);
                    
                if (_currentTab == ZoneTabButton && _populatedZone != zone.Zone.ID) PopulateZone();
            });
        }
        
        PrimaryTabGroup.OnTabChange += button =>
        {
            _currentTab = button;
            if (_currentTab == GalaxyTabButton)
            {
                PropertiesPanel.gameObject.SetActive(true);
                PopulateGalaxyProperties();
                if (!_galaxyPopulated) PopulateGalaxy();
                Camera.transform.position = _galaxyCameraPos;
                Camera.orthographicSize = _galaxyOrthoSize;
            }

            else if (_currentTab == ZoneTabButton)
            {
                PropertiesPanel.gameObject.SetActive(true);
                var zone = _cache.Get<ZoneData>(_selectedZone);
                if (_populatedZone != _selectedZone && zone != null)
                    PopulateZone();
                PopulateZoneProperties(_populatedZone);
                Camera.transform.position = -Vector3.forward;
                if (zone != null)
                    Camera.orthographicSize = zone.Radius * ZoneSizeScale;
            }

            else if (_currentTab == TechTabButton)
            {
                PropertiesPanel.gameObject.SetActive(true);
                PopulateTechProperties();
                if (!_techLayoutGenerated)
                {
                    TechTree.Initialize();
                    TechTree.Blueprints = _cache.GetAll<BlueprintData>().ToArray();
                    TechTree.GenerateTechs();
                }
            }
            
            else PropertiesPanel.gameObject.SetActive(false);
        };
        
        _boundaryMaterial = ZoneBoundary.material;
        _backgroundMaterial = ZoneBackground.material;

        ClickRaycaster.OnClickMiss += () =>
        {
            if (_currentTab == GalaxyTabButton) PopulateGalaxyProperties();
            else if (_currentTab == ZoneTabButton) PopulateZoneProperties(_populatedZone);
            else if (_currentTab == TechTabButton) PopulateTechProperties();
        };
    }

    private void PopulateGalaxyProperties()
    {
        PropertiesPanel.Clear();
        PropertiesPanel.Title.text = _context.GlobalData.GalaxyName;
        PropertiesPanel.AddSection("Galaxy");
        PropertiesPanel.AddProperty("Sectors", $"{_galaxyResponseZones.Count}");
    }

    private void PopulateZoneProperties(Guid zone)
    {
        PropertiesPanel.Clear();
        var zoneData = _cache.Get<ZoneData>(zone);
        PropertiesPanel.Title.text = zoneData.Name;
        PropertiesPanel.AddSection("Sector");
        PropertiesPanel.AddProperty("Planets", $"{zoneData.Planets.Length}");
    }

    private void PopulateTechProperties()
    {
        PropertiesPanel.Clear();
        PropertiesPanel.Title.text = "Technologies";
        var count = _context.Cache.GetAll<BlueprintData>().Count();
        PropertiesPanel.AddProperty("Discovered Techs", $"{count}");
        PropertiesPanel.AddProperty("Available Techs", $"{count}");
        PropertiesPanel.AddProperty("Researched Techs", $"{count}");
    }

    private void Update()
    {
        if (_context != null)
        {
            _context.Time = Time.time;
            //Debug.Log($"Unity delta time: {Time.deltaTime}");
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
                planet.Value.transform.position = float3(_context.GetOrbitPosition(planet.Key) * ZoneSizeScale, .1f);
            }

            foreach (var belt in _zoneBelts)
            {
                belt.Value.Transform.position = float3(_context.GetOrbitPosition(belt.Key) * ZoneSizeScale, .15f);
                belt.Value.Update();
            }

            foreach (var ship in _zoneShips)
            {
                ship.Value.transform.position = float3(ship.Key.Position * ZoneSizeScale, .05f);
                ship.Value.Icon.transform.up = (Vector2) ship.Key.Direction;
                var thrusterScale = ship.Value.Thruster.localScale;
                thrusterScale.x = ship.Key.Axes.First(kvp => kvp.Key is Thruster).Value;
                ship.Value.Thruster.localScale = thrusterScale;
            }
        }
    }

    private void OnApplicationQuit()
    {
        if (TestMode)
        {
            // TODO: Save game state on exit
        }
    }

    void PopulateGalaxy()
    {
        _galaxyPopulated = true;
        
        GalaxyBackground.transform.localScale = Vector3.one * GalaxyScale;
        
        var galaxyMat = GalaxyBackground.material;
        galaxyMat.SetFloat("Arms", _context.GlobalData.Arms);
        galaxyMat.SetFloat("Twist", _context.GlobalData.Twist);
        galaxyMat.SetFloat("TwistPower", _context.GlobalData.TwistPower);
        galaxyMat.SetFloat("SpokeOffset", _context.MapLayers["StarDensity"].SpokeOffset);
        galaxyMat.SetFloat("SpokeScale", _context.MapLayers["StarDensity"].SpokeScale);
        galaxyMat.SetFloat("CoreBoost", _context.MapLayers["StarDensity"].CoreBoost);
        galaxyMat.SetFloat("CoreBoostOffset", _context.MapLayers["StarDensity"].CoreBoostOffset);
        galaxyMat.SetFloat("CoreBoostPower", _context.MapLayers["StarDensity"].CoreBoostPower);
        galaxyMat.SetFloat("EdgeReduction", _context.MapLayers["StarDensity"].EdgeReduction);
        galaxyMat.SetFloat("NoisePosition", _context.MapLayers["StarDensity"].NoisePosition);
        galaxyMat.SetFloat("NoiseAmplitude", _context.MapLayers["StarDensity"].NoiseAmplitude);
        galaxyMat.SetFloat("NoiseOffset", _context.MapLayers["StarDensity"].NoiseOffset);
        galaxyMat.SetFloat("NoiseGain", _context.MapLayers["StarDensity"].NoiseGain);
        galaxyMat.SetFloat("NoiseLacunarity", _context.MapLayers["StarDensity"].NoiseLacunarity);
        galaxyMat.SetFloat("NoiseFrequency", _context.MapLayers["StarDensity"].NoiseFrequency);
        
        // var zones = _galaxyResponse.Zones.ToDictionary(z=>z.ZoneID);
        var linkedZones = new List<Guid>();
        foreach (var zone in _galaxyResponseZones.Values)
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
                if (TestMode)
                {
                    if(!_context.Cache.Get<ZoneData>(zone.ZoneID).Visited)
                    {
                        GenerateZone(zone.ZoneID);
                    }
                    PopulateZoneProperties(zone.ZoneID);
                }
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
                var diff = _galaxyResponseZones[linkedZone].Position - zone.Position;
                link.position = instance.position + Vector3.forward*.1f;
                link.rotation = Quaternion.Euler(0,0,atan2(diff.y, diff.x) * Mathf.Rad2Deg);
                link.localScale = new Vector3(length(diff) * GalaxyScale, 1, 1);
            }
        }
    }

    void GenerateZone(Guid zone)
    {
        var zoneData = _cache.Get<ZoneData>(zone);
        float2 position;
        do
        {
            position = float2(Random.value, Random.value);
        } while (_context.MapLayers["StarDensity"].Evaluate(position, _context.GlobalData) < .1f);
        
        OrbitData[] orbits;
        PlanetData[] planets;
        ZoneGenerator.GenerateZone(
            context: _context,
            zone: zoneData,
            mapLayers: _context.MapLayers.Values,
            resources: _cache.GetAll<SimpleCommodityData>().Where(i=>i.ResourceDensity.Any()),
            orbitData: out orbits,
            planetsData: out planets);
        _cache.AddAll(orbits);
        _cache.AddAll(planets);
        zoneData.Visited = true;
    }

    void PopulateZone()
    {
        // Delete all the existing zone contents
        foreach (var zoneObject in _zoneObjects.Values) Destroy(zoneObject.gameObject);//zoneObject.GetComponent<Prototype>().ReturnToPool();
        _zoneObjects.Clear();
        foreach (var zoneObject in _wormholes) Destroy(zoneObject.gameObject); //zoneObject.GetComponent<Prototype>().ReturnToPool();
        _wormholes.Clear();
        foreach (var belt in _zoneBelts.Values) Destroy(belt.Transform.gameObject);
        _zoneBelts.Clear();
        foreach (var ship in _zoneShips.Values) Destroy(ship.gameObject);
        _zoneShips.Clear();

        if(_populatedZone!=Guid.Empty)
            _context.UnloadZone(_populatedZone);
        
        _populatedZone = _selectedZone;

        var zoneData = _cache.Get<ZoneData>(_populatedZone);

        if (TestMode && !zoneData.Visited)
        {
            GenerateZone(_populatedZone);
        }
        
        _context.InitializeZone(_populatedZone);
        
        float zoneDepth = 0;

        foreach (var planet in _context.ZonePlanets[_populatedZone])
        {
            var planetData = _cache.Get<PlanetData>(planet);
            if (!planetData.Belt)
            {
                var planetObject = Instantiate(ZoneObjectPrefab, ZoneRoot);
                _zoneObjects[planetData.Orbit] = planetObject;
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
                    planetData.Mass > _context.GlobalData.PlanetMass ? PlanetSprite :
                    PlanetoidSprite);
                planetObject.Icon.GetComponent<ClickableCollider>().OnClick += (collider, data) =>
                    {
                        PropertiesPanel.Clear();
                        PropertiesPanel.Title.text = planetData.Name;
                        PropertiesPanel.AddSection(planetData.Mass > _context.GlobalData.SunMass ? "Star" :
                            planetData.Mass > _context.GlobalData.GasGiantMass ? "Gas Giant" :
                            planetData.Mass > _context.GlobalData.PlanetMass ? "Planet" :
                            "Planetoid");
                        PropertiesPanel.AddProperty("Mass", $"{planetData.Mass}");
                        PropertiesPanel.AddList("Resources", planetData.Resources.Select(resource =>
                        {
                            var itemData = _context.Cache.Get<SimpleCommodityData>(resource.Key);
                            return new Tuple<string, string>(itemData.Name, $"{resource.Value:0}");
                        }));
                    };
            }
            else
            {
                var beltObject = Instantiate(BeltPrefab, ZoneRoot);
                var collider = beltObject.GetComponent<MeshCollider>();
                var belt = new AsteroidBelt(_context, beltObject, collider, planetData, AsteroidSpritesheetWidth, AsteroidSpritesheetHeight, ZoneSizeScale);
                _zoneBelts[_cache.Get<OrbitData>(planetData.Orbit).Parent] = belt;
                beltObject.GetComponent<ClickableCollider>().OnClick += (_, data) =>
                {
                    PropertiesPanel.Clear();
                    PropertiesPanel.Title.text = planetData.Name;
                    PropertiesPanel.AddSection("Asteroid Belt");
                    PropertiesPanel.AddProperty("Mass", $"{planetData.Mass}");
                    PropertiesPanel.AddList("Resources", planetData.Resources.Select(resource =>
                    {
                        var itemData = _context.Cache.Get<SimpleCommodityData>(resource.Key);
                        return new Tuple<string, string>(itemData.Name, $"{resource.Value:0}");
                    }));
                };
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
            wormholeObject.transform.position = (Vector2) (_context.WormholePosition(zoneData.ID, wormhole) * ZoneSizeScale);
            wormholeObject.GravityMesh.gameObject.SetActive(false);
            wormholeObject.Icon.material.SetTexture("_MainTex", WormholeSprite);
            wormholeObject.Label.text = otherZone.Name;
            wormholeObject.Icon.GetComponent<ClickableCollider>().OnClick += (_, pointer) =>
            {
                // If the user double clicks on a wormhole, switch to that zone
                if (pointer.clickCount == 2)
                {
                    _selectedGalaxyZone.Background.material.SetColor("_TintColor", UnselectedColor);
                    if(_selectedZone != wormhole && !TestMode)
                        CultClient.Send(new ZoneRequestMessage{ZoneID = wormhole});
                    _selectedZone = wormhole;
                    if(TestMode)
                        PopulateZone();
                    _selectedGalaxyZone = _galaxyZoneObjects[wormhole];
                    _selectedGalaxyZone.Background.material.SetColor("_TintColor", SelectedColor);
                    
                }
            };
        }
    }

    void spawn(string[] args)
    {
        if (!TestMode)
            return;
        
        var zoneData = _cache.Get<ZoneData>(_populatedZone);

        if (zoneData == null)
        {
            Debug.Log("Attempted to spawn ship but no zone is populated!");
            return;
        }

        // Parse first argument as hull name, default to Fighter if argument missing
        HullData hullData;
        if (args.Length > 1)
        {
            hullData = _cache.GetAll<HullData>().FirstOrDefault(h => h.Name == args[1]);
            if (hullData == null)
            {
                Debug.Log($"Hull with name \"{args[1]}\" not found!");
                return;
            }
        }
        else
        {
            hullData = _cache.GetAll<HullData>().FirstOrDefault(h => h.Name == "Fighter");
        }
        var hull = _context.CreateInstance(hullData.ID, .9f) as Gear;

        List<Gear> gear = new List<Gear>();
        if (args.Length > 2)
        {
            foreach (string arg in args.Skip(2))
            {
                var gearData = _cache.GetAll<GearData>().FirstOrDefault(h => h.Name == arg);
                if (gearData == null)
                {
                    Debug.Log($"Gear with name \"{arg}\" not found!");
                    return;
                }
                gear.Add(_context.CreateInstance(gearData.ID, .9f) as Gear);
            }
        }
        else
        {
            // If no gear arguments supplied and we're spawning a ship, get the first thruster and put it in
            if (args[0] == "ship")
            {
                var thrusterData = _cache.GetAll<GearData>().FirstOrDefault(g => g.Behaviors.Any(b => b is ThrusterData));
                var thruster = _context.CreateInstance(thrusterData.ID, .9f) as Gear;
                gear.Add(thruster);
            }
        }

        if (args[0] == "ship")
        {
            var entity = new Ship(_context, hull.ID, gear.Select(i=>i.ID), Enumerable.Empty<Guid>(), _populatedZone);
            _context.Cache.Add(entity);
            _context.ZoneEntities[_populatedZone][entity.ID] = entity;
            //_context.Agents.Add(new AgentController(_context, _populatedZone, entity.ID));
            var zoneShip = Instantiate(ZoneShipPrefab, ZoneRoot);
            zoneShip.Label.text = $"Ship {_zoneShips.Count}";
            _zoneShips[entity] = zoneShip;
        }
        else
        {
            Debug.Log($"Unknown entity type: \"{args[0]}\"");
        }
    }

    void newzone(string[] args)
    {
        var zoneData = _cache.Get<ZoneData>(_selectedZone);

        if (zoneData == null)
        {
            Debug.Log("Attempted to regenerate zone but no zone is selected!");
            return;
        }
        
        if(_populatedZone!=Guid.Empty)
            _context.UnloadZone(_populatedZone);
        _cache.Delete(zoneData);
        _galaxyResponseZones.Remove(zoneData.ID);
        
        var newZone = zoneData.Copy();
        newZone.ID = Guid.NewGuid();
        newZone.Name = newZone.ID.ToString().Substring(0, 8);
        newZone.Visited = false;

        var linkedZones = _cache.GetAll<ZoneData>().Where(z => z.Wormholes.Any(w => w == zoneData.ID)).ToArray();
        foreach (var linkedZone in linkedZones)
        {
            linkedZone.Wormholes.Remove(zoneData.ID);
            linkedZone.Wormholes.Add(newZone.ID);
        }
        newZone.Wormholes = linkedZones.Select(z => z.ID).ToList();
        
        var zone = _galaxyResponseZones[newZone.ID] = new GalaxyResponseZone
        {
            Links = newZone.Wormholes.ToArray(),
            Name = newZone.Name,
            Position = newZone.Position,
            ZoneID = newZone.ID
        };
        
        var galaxyZone = _galaxyZoneObjects[zoneData.ID];
        _galaxyZoneObjects.Remove(zoneData.ID);
        galaxyZone.Label.text = newZone.Name;
        _galaxyZoneObjects[newZone.ID] = galaxyZone;
        
        var collider = galaxyZone.Background.GetComponent<ClickableCollider>();
        collider.Clear();
        collider.OnClick += (_, pointer) =>
        {
            if (_selectedGalaxyZone != null)
                _selectedGalaxyZone.Background.material.SetColor("_TintColor", UnselectedColor);
            if(_selectedZone != zone.ZoneID)
                CultClient.Send(new ZoneRequestMessage{ZoneID = zone.ZoneID});
            _selectedZone = zone.ZoneID;
            _selectedGalaxyZone = galaxyZone;
            _selectedGalaxyZone.Background.material.SetColor("_TintColor", SelectedColor);
            _selectedZone = zone.ZoneID;
                
            // If the user double clicks on a zone, switch to the zone tab
            if(pointer.clickCount == 2) ZoneTabButton.OnPointerClick(pointer);
        };
        
        _cache.Add(newZone);

        _selectedZone = newZone.ID;
        _populatedZone = Guid.Empty;
        
        if(_currentTab == ZoneTabButton)
            PopulateZone();
    }
}

public class AsteroidBelt
{
    public Transform Transform;
    private MeshFilter _filter;
    private MeshCollider _collider;
    private Vector3[] _vertices;
    private Vector3[] _normals;
    private Vector2[] _uvs;
    private int[] _indices;
    private PlanetData _data;
    private GameContext _context;
    private Mesh _mesh;
    private float _zoneScale;

    public AsteroidBelt(GameContext context, MeshFilter meshFilter, MeshCollider collider, PlanetData data, int spritesheetWidth, int spritesheetHeight, float zoneScale)
    {
        Transform = collider.transform;
        _context = context;
        _data = data;
        _filter = meshFilter;
        _collider = collider;
        _zoneScale = zoneScale;
        _vertices = new Vector3[data.Asteroids.Length*4];
        _normals = new Vector3[data.Asteroids.Length*4];
        _uvs = new Vector2[data.Asteroids.Length*4];
        _indices = new int[data.Asteroids.Length*6];

        var maxDist = 0f;
        var spriteSize = float2(1f / spritesheetWidth, 1f / spritesheetHeight);
        // vertex order: bottom left, top left, top right, bottom right
        for (var i = 0; i < _data.Asteroids.Length; i++)
        {
            if (_data.Asteroids[i].x > maxDist)
                maxDist = _data.Asteroids[i].x;
            var spriteX = Random.Range(0, spritesheetWidth);
            var spriteY = Random.Range(0, spritesheetHeight);
            
            _uvs[i * 4] = new Vector2(spriteX * spriteSize.x, spriteY * spriteSize.y);
            _uvs[i * 4 + 1] = new Vector2(spriteX * spriteSize.x, spriteY * spriteSize.y + spriteSize.y);
            _uvs[i * 4 + 2] = new Vector2(spriteX * spriteSize.x + spriteSize.x, spriteY * spriteSize.y + spriteSize.y);
            _uvs[i * 4 + 3] = new Vector2(spriteX * spriteSize.x + spriteSize.x, spriteY * spriteSize.y);
            
            _indices[i * 6] = i * 4;
            _indices[i * 6 + 1] = i * 4 + 1;
            _indices[i * 6 + 2] = i * 4 + 3;
            _indices[i * 6 + 3] = i * 4 + 3;
            _indices[i * 6 + 4] = i * 4 + 1;
            _indices[i * 6 + 5] = i * 4 + 2;
        }
        
        for (var i = 0; i < _normals.Length; i++)
        {
            _normals[i] = -Vector3.forward;
        }
        
        _mesh = new Mesh();
        _mesh.vertices = _vertices;
        _mesh.uv = _uvs;
        _mesh.triangles = _indices;
        _mesh.normals = _normals;
        _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * maxDist);

        _filter.mesh = _mesh;
        //_collider.sharedMesh = _mesh;
    }

    public void Update()
    {
        for (var i = 0; i < _data.Asteroids.Length; i++)
        {
            var rotation = Quaternion.Euler(0, 0, (float) (_context.Time * _data.Asteroids[i].w % 360.0));
            var position = (Vector3) (Vector2)
                OrbitData.Evaluate((float) (_context.Time / -pow(_data.Asteroids[i].x, _context.GlobalData.OrbitPeriodExponent) * _context.GlobalData.OrbitPeriodMultiplier) *
                    _context.GlobalData.OrbitSpeedMultiplier + _data.Asteroids[i].y) * (_data.Asteroids[i].x * _zoneScale);
            _vertices[i * 4] = rotation * new Vector3(-_data.Asteroids[i].z,-_data.Asteroids[i].z,0) + position;
            _vertices[i * 4 + 1] = rotation * new Vector3(-_data.Asteroids[i].z,_data.Asteroids[i].z,0) + position;
            _vertices[i * 4 + 2] = rotation * new Vector3(_data.Asteroids[i].z,_data.Asteroids[i].z,0) + position;
            _vertices[i * 4 + 3] = rotation * new Vector3(_data.Asteroids[i].z,-_data.Asteroids[i].z,0) + position;
        }

        _mesh.vertices = _vertices;
        _collider.sharedMesh = _mesh;
    }
}
