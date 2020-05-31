using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NaughtyAttributes;
using RethinkDb.Driver.Net;
using TMPro;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.EventSystems;
using UnityEngine.UI;
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
    public ContextMenu ContextMenu;
    public RectTransform GameMenuRoot;
    public RectTransform CorpMenuRoot;
    public CorporationMenuItem CorpOptionPrefab;
    public TabButton CorpMenuContinueButton;
    public TMP_InputField CorpNameInputField;
    public LineRenderer OrbitLineRenderer;
    
    [Section("Tab Links")]
    public TabGroup PrimaryTabGroup;
    public TabButton GalaxyTabButton;
    public TabButton ZoneTabButton;
    public TabButton TechTabButton;
    
    [Section("Prefabs & Prototypes")]
    public Prototype GalaxyZonePrototype;
    public Prototype GalaxyZoneLinkPrototype;
    public ZoneGravityObject ZoneGravityObjectPrefab;
    public ZoneObject ZoneObjectPrefab;
    public ZoneShip ZoneShipPrefab;
    public MeshFilter BeltPrefab;
    public MeshRenderer ChildEntityPrefab;
    
    [Section("Textures")]
    public Texture2D PlanetoidSprite;
    public Texture2D PlanetSprite;
    public Texture2D GasGiantSprite;
    public Texture2D OrbitalSprite;
    public Texture2D ShipSprite;
    public Texture2D TurretSprite;
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
    public float ChildEntityDistance = .66f;
    public float ChildEntityScale = .25f;
    public float ChildEntityOffsetDegrees = 30f;
    public int OrbitCircleSegments = 128;

    private DatabaseCache _cache;
    private GameContext _context;
    // private GalaxyResponseMessage _galaxyResponse;
    private TabButton _currentTab;
    private bool _galaxyPopulated;
    private Guid _populatedZone;
    private Guid _selectedZone;
    private GalaxyZone _selectedGalaxyZone;
    private Dictionary<Guid, GalaxyZone> _galaxyZoneObjects = new Dictionary<Guid, GalaxyZone>();
    private Dictionary<Guid, ZoneGravityObject> _zoneGravityObjects = new Dictionary<Guid, ZoneGravityObject>(); // Key is Orbit
    private Dictionary<Guid, AsteroidBelt> _zoneBelts = new Dictionary<Guid, AsteroidBelt>();
    private Dictionary<Ship, ZoneShip> _zoneShips = new Dictionary<Ship, ZoneShip>();
    private Dictionary<OrbitalEntity, ZoneObject> _zoneOrbitals = new Dictionary<OrbitalEntity, ZoneObject>();
    private List<ZoneGravityObject> _wormholes = new List<ZoneGravityObject>();
    private Vector3 _galaxyCameraPos = -Vector3.forward;
    private float _galaxyOrthoSize = 50;
    private Material _boundaryMaterial;
    private Material _backgroundMaterial;
    private bool _techLayoutGenerated;
    private Connection _connection;
    // private bool _choosingCorporation;
    private Guid _playerCorporation;
    private Dictionary<CorporationMenuItem, Guid> _corpOptionMap = new Dictionary<CorporationMenuItem, Guid>();
    private CorporationMenuItem _selectedParent;
    private Dictionary<int, float2[]> _childEntityPositions = new Dictionary<int, float2[]>();
    private bool _orbitSelectMode;
    private Guid _selectedOrbit = Guid.Empty;
    private Guid _selectedTowingTarget = Guid.Empty;
    private float _orbitRadius;
    
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
            _context.GalaxyZones = _cache.GetAll<ZoneData>()
                .ToDictionary(z => z.ID, z => new SimplifiedZoneData
                {
                    Links = z.Wormholes.ToArray(),
                    Name = z.Name,
                    Position = z.Position,
                    ZoneID = z.ID
                });
            
            foreach (var zone in _context.GalaxyZones.Keys) GenerateZone(zone);

            var megas = _cache.GetAll<MegaCorporation>();
            if (megas.First().HomeZone == Guid.Empty)
            {
                _context.PlaceMegas();
            }

            var player = _cache.GetAll<Player>().FirstOrDefault();
            if (player == null)
            {
                player = new Player
                {
                    Context = _context,
                    Username = Environment.UserName
                };
                _cache.Add(player);
            }

            if (player.Corporation == Guid.Empty)
            {
                GameMenuRoot.gameObject.SetActive(false);
                CorpMenuRoot.gameObject.SetActive(true);
                
                foreach (var mega in megas)
                {
                    var corpOption = Instantiate(CorpOptionPrefab, CorpMenuRoot);
                    _corpOptionMap[corpOption] = mega.ID;
                    corpOption.Title.text = mega.Name;
                    corpOption.Logo.sprite = Resources.Load<Sprite>(mega.Logo.Substring("Assets/Resources/".Length).Split('.').First());
                    corpOption.Description.text = mega.Description;
                    var corpOptionImage = corpOption.GetComponent<Image>();
                    corpOptionImage.color = UnselectedColor;
                    corpOption.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        if (_selectedParent != null)
                        {
                            _selectedParent.GetComponent<Image>().color = UnselectedColor;
                        }
                        _selectedParent = corpOption;
                        corpOptionImage.color = SelectedColor;
                    });
                    foreach (var attributeValue in mega.Personality.Select(kvp =>
                        new {attribute = _cache.Get<PersonalityAttribute>(kvp.Key), val = kvp.Value}))
                    {
                        var attributeInstance = Instantiate(corpOption.AttributePrefab, corpOption.transform);
                        attributeInstance.Slider.value = attributeValue.val;
                        attributeInstance.Title.text = attributeValue.attribute.Name;
                        attributeInstance.HighLabel.text = attributeValue.attribute.HighName;
                        attributeInstance.LowLabel.text = attributeValue.attribute.LowName;
                    }
                }

                CorpMenuContinueButton.OnClick += () =>
                {
                    if (_selectedParent != null && !string.IsNullOrEmpty(CorpNameInputField.text))
                    {
                        var newCorp = new Corporation
                        {
                            Context = _context,
                            Name = CorpNameInputField.text,
                            Parent = _corpOptionMap[_selectedParent]
                        };
                        _cache.Add(newCorp);
                        _context.CorporationControllers[newCorp.ID] = new List<IController>();
                        player.Corporation = newCorp.ID;
                        _playerCorporation = newCorp.ID;
                        
                        GameMenuRoot.gameObject.SetActive(true);
                        CorpMenuRoot.gameObject.SetActive(false);

                        var parentCorp = _cache.Get<MegaCorporation>(newCorp.Parent);
                        _selectedZone = parentCorp.HomeZone;
                        ZoneTabButton.OnPointerClick(null);
                        
                        var entities = new List<Entity>();
                        foreach(var loadout in parentCorp.InitialFleet)
                            for(int i=0; i<loadout.Value; i++)
                                entities.Add(_context.CreateEntity(_selectedZone, newCorp.ID, loadout.Key));
                        var colony = entities.First(e => e is OrbitalEntity);
                        foreach (var ship in entities.Where(e => e is Ship))
                        {
                            foreach (var controller in ship.GetBehaviors<ControllerBase>())
                                controller.HomeEntity = colony.ID;

                            _context.SetParent(ship, colony);
                        }
                    }
                };
            }
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
                _context.GalaxyZones = galaxy.Zones.ToDictionary(z => z.ZoneID);
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
                    Select(_selectedZone);
                    
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
                Select(_populatedZone);
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

        ClickRaycaster.OnClickMiss += data =>
        {
            if (_currentTab == GalaxyTabButton) PopulateGalaxyProperties();
            else if (_currentTab == ZoneTabButton)
            {
                if (data.button == PointerEventData.InputButton.Left)
                {
                    if (_orbitSelectMode)
                    {
                        var playerCorp = _cache.Get<Corporation>(_playerCorporation);
                        var towingTask = new StationTowing
                        {
                            Context = _context,
                            OrbitDistance = _orbitRadius,
                            OrbitParent = _selectedOrbit,
                            Station = _selectedTowingTarget,
                            Zone = _populatedZone
                        };
                        _cache.Add(towingTask);
                        playerCorp.Tasks.Add(towingTask.ID);
                        _orbitSelectMode = false;
                        OrbitLineRenderer.gameObject.SetActive(false);
                    }
                    else
                        Select(_populatedZone);
                }
                else if (data.button == PointerEventData.InputButton.Right) 
                    ShowContextMenu(_populatedZone);
            }
            else if (_currentTab == TechTabButton) PopulateTechProperties();
        };

        OrbitLineRenderer.positionCount = OrbitCircleSegments;
        OrbitLineRenderer.gameObject.SetActive(false);
    }

    private void PopulateGalaxyProperties()
    {
        PropertiesPanel.Clear();
        PropertiesPanel.Title.text = _context.GlobalData.GalaxyName;
        PropertiesPanel.AddSection("Galaxy");
        PropertiesPanel.AddProperty("Sectors", () => $"{_context.GalaxyZones.Count}");
    }

    private void PopulateTechProperties()
    {
        PropertiesPanel.Clear();
        PropertiesPanel.Title.text = "Technologies";
        var count = _context.Cache.GetAll<BlueprintData>().Count();
        PropertiesPanel.AddProperty("Discovered Techs", () => $"{count}");
        PropertiesPanel.AddProperty("Available Techs", () => $"{count}");
        PropertiesPanel.AddProperty("Researched Techs", () => $"{count}");
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
            foreach (var planet in _zoneGravityObjects)
            {
                planet.Value.transform.position = float3(_context.GetOrbitPosition(planet.Key) * ZoneSizeScale, .1f);
                planet.Value.Message.text = "";
            }

            foreach (var belt in _zoneBelts)
            {
                //belt.Value.Transform.position = float3(_context.GetOrbitPosition(belt.Key) * ZoneSizeScale, .15f);
                belt.Value.Update();
            }

            foreach (var ship in _zoneShips)
            {
                ship.Value.transform.position = float3(ship.Key.Position * ZoneSizeScale, .05f);
                ship.Value.Icon.transform.up = (Vector2) ship.Key.Direction;
                var thrusterScale = ship.Value.Thruster.localScale;
                thrusterScale.x = ship.Key.Axes[ship.Key.GetAxis<Thruster>()].Value;
                ship.Value.Thruster.localScale = thrusterScale;
            }

            var entities = _context.GetEntities(_populatedZone);
            var shipGameObjects = _zoneShips.Keys.ToList();
            var orbitalGameObjects = _zoneOrbitals.Keys.ToList();
            var zoneShips = entities.Where(e => e is Ship && e.Parent == Guid.Empty).Cast<Ship>();
            var zoneOrbitals = entities.Where(e => e is OrbitalEntity && e.Parent == Guid.Empty).Cast<OrbitalEntity>();

            void PopulateChildObjects(ZoneObject zoneObject, Entity entity)
            {
                if (zoneObject.Children.Count != entity.Children.Count)
                {
                    foreach(var child in zoneObject.Children)
                        Destroy(child.gameObject);
                    zoneObject.Children.Clear();
                    var positions = GetChildEntityPositions(entity.Children.Count);
                    foreach (var child in entity.Children)
                    {
                        var childEntity = _context.Cache.Get(child);
                        var childGameObject = Instantiate(ChildEntityPrefab, zoneObject.transform);
                        childGameObject.material.SetTexture("_MainTex", childEntity is Ship ? ShipSprite : OrbitalSprite );
                        childGameObject.GetComponent<ClickableCollider>().OnClick += (collider, data) => 
                        {
                            if (data.button == PointerEventData.InputButton.Left)
                                Select(child);
                            else if (data.button == PointerEventData.InputButton.Right) 
                                ShowContextMenu(child);
                        };
                        zoneObject.Children.Add(childGameObject);
                    }

                    for (int i = 0; i < entity.Children.Count; i++)
                        zoneObject.Children[i].transform.localPosition = (Vector2) positions[i];
                }
            }

            foreach (var ship in zoneShips)
            {
                if (!_zoneShips.ContainsKey(ship))
                {
                    var zoneShip = Instantiate(ZoneShipPrefab, ZoneRoot);
                    zoneShip.Icon.GetComponent<ClickableCollider>().OnClick += (collider, data) => 
                    {
                        if (data.button == PointerEventData.InputButton.Left)
                            Select(ship.ID);
                        else if (data.button == PointerEventData.InputButton.Right) 
                            ShowContextMenu(ship.ID);
                    };
                    zoneShip.Label.text = ship.Name;
                    _zoneShips[ship] = zoneShip;
                }
                
                _zoneShips[ship].transform.position = float3(ship.Position * ZoneSizeScale, .05f);
                _zoneShips[ship].Icon.transform.up = (Vector2) ship.Direction;
                var thrusterScale = _zoneShips[ship].Thruster.localScale;
                thrusterScale.x = ship.Axes[ship.GetAxis<Thruster>()].Value;
                _zoneShips[ship].Thruster.localScale = thrusterScale;
                _zoneShips[ship].Message.text = !ship.Messages.Any() ? "" : 
                    string.Join("\n", ship.Messages.OrderByDescending(x=>x.Value).Take(2).Select(x=>x.Key));

                PopulateChildObjects(_zoneShips[ship], ship);

                shipGameObjects.Remove(ship);
            }
            
            // We have removed all game objects which are associated with active entities
            // Any game objects remaining are orphans and must be executed
            foreach(var ship in shipGameObjects)
            {
                Destroy(_zoneShips[ship].gameObject);
                _zoneShips.Remove(ship);
            }

            foreach (var orbital in zoneOrbitals)
            {
                if (!_zoneOrbitals.ContainsKey(orbital))
                {
                    var zoneOrbital = Instantiate(ZoneObjectPrefab, ZoneRoot);
                    zoneOrbital.Icon.material.SetTexture("_MainTex", OrbitalSprite );
                    zoneOrbital.Icon.GetComponent<ClickableCollider>().OnClick += (collider, data) => 
                    {
                        if (data.button == PointerEventData.InputButton.Left)
                            Select(orbital.ID);
                        else if (data.button == PointerEventData.InputButton.Right) 
                            ShowContextMenu(orbital.ID);
                    };
                    zoneOrbital.Label.text = orbital.Name;
                    _zoneOrbitals[orbital] = zoneOrbital;
                }
                
                _zoneOrbitals[orbital].transform.position = float3(orbital.Position * ZoneSizeScale, .05f);
                _zoneOrbitals[orbital].Icon.transform.up = (Vector2) orbital.Direction;
                _zoneOrbitals[orbital].Message.text = !orbital.Messages.Any() ? "" : 
                    string.Join("\n", orbital.Messages.OrderByDescending(x=>x.Value).Take(2).Select(x=>x.Key));

                PopulateChildObjects(_zoneOrbitals[orbital], orbital);

                orbitalGameObjects.Remove(orbital);
            }
            
            foreach(var orbital in orbitalGameObjects)
            {
                Destroy(_zoneOrbitals[orbital].gameObject);
                _zoneOrbitals.Remove(orbital);
            }

            if (_orbitSelectMode)
            {
                var center = _context.GetOrbitPosition(_selectedOrbit) * ZoneSizeScale;
                var mousePos = Camera.ScreenToWorldPoint(Input.mousePosition);
                var radius = length(float2(mousePos.x, mousePos.y)  - center);
                _orbitRadius = radius / ZoneSizeScale;
                var positions = new Vector3[OrbitCircleSegments];
                for (int i = 0; i < OrbitCircleSegments; i++)
                {
                    var deg = ((float) i) / OrbitCircleSegments * 2 * PI;
                    positions[i] = float3(float2(sin(deg), cos(deg)) * radius + center, 0);
                }
                OrbitLineRenderer.gameObject.SetActive(true);
                OrbitLineRenderer.SetPositions(positions);
            }
        }
    }

    private float2[] GetChildEntityPositions(int count)
    {
        if (!_childEntityPositions.ContainsKey(count))
        {
            var down = float2(0, -ChildEntityDistance);
            var positions = new float2[count];
            if (count % 2 == 1)
            {
                positions[0] = down;
                for (int i = 1; i < count; i++)
                    positions[i] = mul(float3(down, 1), Unity.Mathematics.float3x3.RotateZ(
                        Mathf.Deg2Rad * ChildEntityOffsetDegrees * ((i - 1) / 2 + 1) * (i % 2 == 1 ? 1 : -1))).xy;
            }
            else
            {
                for (int i = 0; i < count; i++)
                    positions[i] = mul(float3(down, 1), Unity.Mathematics.float3x3.RotateZ(
                        Mathf.Deg2Rad * ((ChildEntityOffsetDegrees * (i / 2 + 1) - ChildEntityOffsetDegrees/2) * (i % 2 == 1 ? 1 : -1)))).xy;
            }

            _childEntityPositions[count] = positions;
        }

        return _childEntityPositions[count];
    }

    private void OnApplicationQuit()
    {
        if (TestMode)
        {
            // TODO: Save game state on exit
        }
    }

    void ShowContextMenu(Guid clickTarget)
    {
        var targetObject = _context.Cache.Get(clickTarget);
        ContextMenu.gameObject.SetActive(true);
        ContextMenu.Clear();
        if (targetObject is PlanetData planet)
        {
            if (planet.Belt)
            {
                ContextMenu.AddOption("Create Mining Task", () =>
                {
                    var task = new Mining
                    {
                        Asteroids = clickTarget,
                        Context = _context,
                        Zone = _populatedZone
                    };
                    _cache.Add(task);
                    _cache.Get<Corporation>(_playerCorporation).Tasks.Add(task.ID);
                });
            }
            ContextMenu.AddOption("Create Survey Task", () =>
            {
                var task = new Survey
                {
                    Planets = new List<Guid>(new []{clickTarget}),
                    Context = _context,
                    Zone = _populatedZone
                };
                _cache.Add(task);
                _cache.Get<Corporation>(_playerCorporation).Tasks.Add(task.ID);
            });
            // ContextMenu.AddOption("Test Option 1", () => Debug.Log("Test Option 1 Selected"));
            // ContextMenu.AddOption("Test Option 2", () => Debug.Log("Test Option 2 Selected"));
            // ContextMenu.AddDropdown("Test Dropdown", new []
            // {
            //     ("Dropdown Option 1", (Action) (() => Debug.Log("Dropdown Option 1 Selected"))),
            //     ("Dropdown Option 2", (Action) (() => Debug.Log("Dropdown Option 2 Selected"))),
            //     ("Dropdown Option 3", (Action) (() => Debug.Log("Dropdown Option 3 Selected")))
            // });
            // ContextMenu.AddOption("Test Option 3", () => Debug.Log("Test Option 3 Selected"));
        }
        else if (targetObject is Ship ship)
        {
            var controller = ship.GetBehaviors<ControllerBase>().FirstOrDefault();
            if (controller != null)
                ContextMenu.AddOption("Finish Task", () => controller.FinishTask());

        }
        else if (targetObject is OrbitalEntity orbital)
        {
            ContextMenu.AddOption("Create Towing Task", () =>
            {
                _selectedOrbit = Guid.Empty;
                _selectedTowingTarget = orbital.ID;
                _orbitSelectMode = true;
                OrbitLineRenderer.gameObject.SetActive(true);
            });
        }
        else if (targetObject is ZoneData zone)
        {
            ContextMenu.AddOption("Survey All", () =>
            {
                var planets = zone.Planets.ToList();
                var task = new Survey
                {
                    Planets = planets,
                    Context = _context,
                    Zone = _populatedZone
                };
                _cache.Add(task);
                _cache.Get<Corporation>(_playerCorporation).Tasks.Add(task.ID);
            });
            ContextMenu.AddOption("Survey Planets", () =>
            {
                var planets = zone.Planets.Where(id => !_cache.Get<PlanetData>(id).Belt).ToList();
                var task = new Survey
                {
                    Planets = planets,
                    Context = _context,
                    Zone = _populatedZone
                };
                _cache.Add(task);
                _cache.Get<Corporation>(_playerCorporation).Tasks.Add(task.ID);
            });
            ContextMenu.AddOption("Survey Asteroids", () =>
            {
                var planets = zone.Planets.Where(id => _cache.Get<PlanetData>(id).Belt).ToList();
                var task = new Survey
                {
                    Planets = planets,
                    Context = _context,
                    Zone = _populatedZone
                };
                _cache.Add(task);
                _cache.Get<Corporation>(_playerCorporation).Tasks.Add(task.ID);
            });

        }
        else
        {
            ContextMenu.gameObject.SetActive(false);
            return;
        }
        ContextMenu.Show();
    }

    void Select(Guid clickTarget)
    {
        var targetObject = _context.Cache.Get(clickTarget);

        if (targetObject is PlanetData planet)
        {
            if (_orbitSelectMode)
            {
                _selectedOrbit = planet.Orbit;
            }
            else
            {
                PropertiesPanel.Clear();
                PropertiesPanel.Title.text = planet.Name;
                if(planet.Belt)
                    PropertiesPanel.AddSection("Asteroid Belt");
                else
                    PropertiesPanel.AddSection(
                        planet.Mass > _context.GlobalData.SunMass ? "Star" :
                        planet.Mass > _context.GlobalData.GasGiantMass ? "Gas Giant" :
                        planet.Mass > _context.GlobalData.PlanetMass ? "Planet" :
                        "Planetoid");
                PropertiesPanel.AddProperty("Mass", () => $"{planet.Mass}");
                var playerCorp = _cache.Get<Corporation>(_playerCorporation);
                if (!playerCorp.PlanetSurveyFloor.ContainsKey(clickTarget))
                {
                    PropertiesPanel.AddProperty("Not surveyed for resources", () => "");
                }
                else
                {
                    var detectionFloor = playerCorp.PlanetSurveyFloor[clickTarget];
                    var detectedResources = planet.Resources.Where(x => x.Value > detectionFloor);
                    if(!detectedResources.Any())
                        PropertiesPanel.AddProperty("No resources detected", () => "");
                    else
                    {
                        var list = PropertiesPanel.AddList("Resources");
                        foreach (var resource in detectedResources)
                        {
                            var itemData = _context.Cache.Get<SimpleCommodityData>(resource.Key);
                            list.AddProperty(itemData.Name, () => $"{resource.Value:0}");
                        }
                    }
                }
            }
        }

        if (targetObject is Entity entity)
        {
            PropertiesPanel.Clear();
            PropertiesPanel.Title.text = entity.Name;
            var hull = _context.Cache.Get<Gear>(entity.Hull);
            var hullData = _context.Cache.Get<HullData>(hull.Data);
            PropertiesPanel.AddSection(
                hullData.HullType == HullType.Ship ? "Drone Ship" :
                hullData.HullType == HullType.Station ? "Colony" :
                "Weapons Platform");
            PopulateGearProperties(PropertiesPanel.AddList(hullData.Name), hull, entity);
            //PropertiesPanel.AddProperty("Hull", () => $"{hullData.Name}");
            PropertiesPanel.AddProperty("Capacity", () => $"{entity.OccupiedCapacity}/{entity.Capacity:0}");
            PropertiesPanel.AddProperty("Mass", () => $"{entity.Mass.SignificantDigits(_context.GlobalData.SignificantDigits)}");
            PropertiesPanel.AddProperty("Temperature", () => $"{entity.Temperature:0}°K");
            PropertiesPanel.AddProperty("Energy", () => $"{entity.Energy:0}/{entity.GetBehaviors<Reactor>().First().Capacitance:0}");
            var gearList = PropertiesPanel.AddList("Gear");
            var equippedItems = entity.EquippedItems.Select(g => _context.Cache.Get<Gear>(g));
            foreach(var gear in equippedItems)
                PopulateGearProperties(gearList.AddList(gear.ItemData.Name), gear, entity);
            //     gearList.AddProperty(gear.ItemData.Name);
            // var firstGear = equippedItems.First();
            // PopulateGearProperties(gearList.AddList(firstGear.ItemData.Name), firstGear, entity);
            if(!entity.Cargo.Any())
                PropertiesPanel.AddProperty("No Cargo");
            else
            {
                var cargoList = PropertiesPanel.AddList("Cargo");
                foreach (var itemID in entity.Cargo)
                {
                    var itemInstance = _context.Cache.Get<ItemInstance>(itemID);
                    var data = _context.Cache.Get<ItemData>(itemInstance.Data);
                    if(itemInstance is SimpleCommodity simpleCommodity)
                        cargoList.AddProperty(data.Name, () => simpleCommodity.Quantity.ToString());
                    else
                        cargoList.AddProperty(data.Name);
                }
            }
        }

        if (targetObject is ZoneData zone)
        {
            PropertiesPanel.Clear();
            PropertiesPanel.Title.text = zone.Name;
            PropertiesPanel.AddSection("Sector");
            PropertiesPanel.AddProperty("Planets", () => $"{zone.Planets.Length}");
        }
    }

    void PopulateGearProperties(PropertiesList panel, Gear gear, Entity entity)
    {
        var data = gear.ItemData;
        panel.AddProperty("Durability", () => $"{gear.Durability.SignificantDigits(_context.GlobalData.SignificantDigits)}/{_context.Evaluate(data.Durability, gear).SignificantDigits(_context.GlobalData.SignificantDigits)}");
        foreach (var behavior in data.Behaviors)
        {
            var type = behavior.GetType();
            if (type.GetCustomAttribute(typeof(RuntimeInspectable)) != null)
            {
                foreach (var field in type.GetFields().Where(f => f.GetCustomAttribute<RuntimeInspectable>() != null))
                {
                    var fieldType = field.FieldType;
                    if (fieldType == typeof(float))
                        panel.AddProperty(field.Name, () => $"{((float) field.GetValue(behavior)).SignificantDigits(_context.GlobalData.SignificantDigits)}");
                    else if (fieldType == typeof(int))
                        panel.AddProperty(field.Name, () => $"{(int) field.GetValue(behavior)}");
                    else if (fieldType == typeof(PerformanceStat))
                    {
                        var stat = (PerformanceStat) field.GetValue(behavior);
                        panel.AddProperty(field.Name, () => $"{_context.Evaluate(stat, gear, entity).SignificantDigits(_context.GlobalData.SignificantDigits)}");
                    }
                }
            }
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
        foreach (var zone in _context.GalaxyZones.Values)
        {
            linkedZones.Add(zone.ZoneID);
            var instance = GalaxyZonePrototype.Instantiate<Transform>();
            instance.position = float3((Vector2) zone.Position - Vector2.one * .5f,0) * GalaxyScale;
            var instanceZone = instance.GetComponent<GalaxyZone>();
            _galaxyZoneObjects[zone.ZoneID] = instanceZone;
            instanceZone.Label.text = zone.Name;
            instanceZone.Icon.GetComponent<ClickableCollider>().OnClick += (_, pointer) =>
            {
                if (_selectedGalaxyZone != null)
                    _selectedGalaxyZone.Background.material.SetColor("_TintColor", UnselectedColor);
                if(_selectedZone != zone.ZoneID)
                    CultClient.Send(new ZoneRequestMessage{ZoneID = zone.ZoneID});
                if (TestMode)
                {
                    Select(zone.ZoneID);
                }
                _selectedZone = zone.ZoneID;
                _selectedGalaxyZone = instanceZone;
                _selectedGalaxyZone.Background.material.SetColor("_TintColor", SelectedColor);
                _selectedZone = zone.ZoneID;
                
                // If the user double clicks on a zone, switch to the zone tab
                if(pointer.clickCount == 2) ZoneTabButton.OnPointerClick(pointer);
            };
            
            if(_selectedZone == zone.ZoneID)
            {
                _selectedGalaxyZone = instanceZone;
                _selectedGalaxyZone.Background.material.SetColor("_TintColor", SelectedColor);
            }
            
            foreach (var linkedZone in zone.Links.Where(l=>!linkedZones.Contains(l)))
            {
                var link = GalaxyZoneLinkPrototype.Instantiate<Transform>();
                var diff = _context.GalaxyZones[linkedZone].Position - zone.Position;
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
        foreach (var zoneObject in _zoneGravityObjects.Values) Destroy(zoneObject.gameObject);//zoneObject.GetComponent<Prototype>().ReturnToPool();
        _zoneGravityObjects.Clear();
        foreach (var zoneObject in _wormholes) Destroy(zoneObject.gameObject); //zoneObject.GetComponent<Prototype>().ReturnToPool();
        _wormholes.Clear();
        foreach (var belt in _zoneBelts.Values) Destroy(belt.Transform.gameObject);
        _zoneBelts.Clear();
        foreach (var ship in _zoneShips.Values) Destroy(ship.gameObject);
        _zoneShips.Clear();
        
        _populatedZone = _selectedZone;

        var zoneData = _cache.Get<ZoneData>(_populatedZone);

        if (TestMode && !zoneData.Visited)
        {
            GenerateZone(_populatedZone);
        }

        _context.ForceLoadZone = _populatedZone;
        
        float zoneDepth = 0;

        foreach (var planet in _context.ZonePlanets[_populatedZone])
        {
            var planetData = _cache.Get<PlanetData>(planet);
            if (!planetData.Belt)
            {
                var planetObject = Instantiate(ZoneGravityObjectPrefab, ZoneRoot);
                _zoneGravityObjects[planetData.Orbit] = planetObject;
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
                        if (data.button == PointerEventData.InputButton.Left)
                            Select(planetData.ID);
                        else if (data.button == PointerEventData.InputButton.Right) 
                            ShowContextMenu(planetData.ID);
                    };
            }
            else
            {
                var beltObject = Instantiate(BeltPrefab, ZoneRoot);
                var collider = beltObject.GetComponent<MeshCollider>();
                var belt = new AsteroidBelt(_context, beltObject, collider, planetData.ID, AsteroidSpritesheetWidth, AsteroidSpritesheetHeight, ZoneSizeScale);
                _zoneBelts[_cache.Get<OrbitData>(planetData.Orbit).Parent] = belt;
                beltObject.GetComponent<ClickableCollider>().OnClick += (_, data) =>
                {
                    if (data.button == PointerEventData.InputButton.Left)
                        Select(planetData.ID);
                    else if (data.button == PointerEventData.InputButton.Right) 
                        ShowContextMenu(planetData.ID);
                };
            }
        }

        var radius = (zoneData.Radius * ZoneSizeScale * 2);
        _backgroundMaterial.SetFloat("_ClipDistance", radius);
        _backgroundMaterial.SetFloat("_HeightRange", zoneDepth + _boundaryMaterial.GetFloat("_Depth"));
        ZoneBoundary.transform.localScale = Vector3.one * radius;
        
        foreach (var wormhole in zoneData.Wormholes)
        {
            var otherZone = _context.GalaxyZones[wormhole];
            var wormholeObject = Instantiate(ZoneGravityObjectPrefab, ZoneRoot);
            _wormholes.Add(wormholeObject);
            wormholeObject.transform.position = (Vector2) (_context.WormholePosition(zoneData.ID, wormhole) * ZoneSizeScale);
            wormholeObject.GravityMesh.gameObject.SetActive(false);
            wormholeObject.Icon.material.SetTexture("_MainTex", WormholeSprite);
            wormholeObject.Label.text = otherZone.Name;
            wormholeObject.Message.text = "";
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

        var loadoutData = _context.Cache.GetAll<LoadoutData>().FirstOrDefault(l => l.Name == args[1]);
        if (loadoutData == null)
        {
            Debug.Log($"Loadout with name \"{args[1]}\" not found!");
            return;
        }

        _context.CreateEntity(_populatedZone, _playerCorporation, loadoutData.ID);

        // Parse first argument as hull name, default to Fighter if argument missing
        // HullData hullData;
        // if (args.Length > 1)
        // {
        //     hullData = _cache.GetAll<HullData>().FirstOrDefault(h => h.Name == args[1]);
        //     if (hullData == null)
        //     {
        //         Debug.Log($"Hull with name \"{args[1]}\" not found!");
        //         return;
        //     }
        // }
        // else
        // {
        //     hullData = _cache.GetAll<HullData>().FirstOrDefault(h => h.Name == "Fighter");
        // }
        // var hull = _context.CreateInstance(hullData.ID, .9f) as Gear;
        //
        // List<Gear> gear = new List<Gear>();
        // if (args.Length > 2)
        // {
        //     foreach (string arg in args.Skip(2))
        //     {
        //         var gearData = _cache.GetAll<GearData>().FirstOrDefault(h => h.Name == arg);
        //         if (gearData == null)
        //         {
        //             Debug.Log($"Gear with name \"{arg}\" not found!");
        //             return;
        //         }
        //         gear.Add(_context.CreateInstance(gearData.ID, .9f) as Gear);
        //     }
        // }
        // else
        // {
        //     // If no gear arguments supplied and we're spawning a ship, get the first thruster and put it in
        //     if (args[0] == "ship")
        //     {
        //         var thrusterData = _cache.GetAll<GearData>().FirstOrDefault(g => g.Behaviors.Any(b => b is ThrusterData));
        //         var thruster = _context.CreateInstance(thrusterData.ID, .9f) as Gear;
        //         gear.Add(thruster);
        //     }
        // }
        //
        // if (args[0] == "ship")
        // {
        //     var entity = new Ship(_context, hull.ID, gear.Select(i=>i.ID), Enumerable.Empty<Guid>(), _populatedZone);
        //     _context.Cache.Add(entity);
        //     _context.ZoneEntities[_populatedZone][entity.ID] = entity;
        //     //_context.Agents.Add(new AgentController(_context, _populatedZone, entity.ID));
        //     var zoneShip = Instantiate(ZoneShipPrefab, ZoneRoot);
        //     zoneShip.Label.text = $"Ship {_zoneShips.Count}";
        //     _zoneShips[entity] = zoneShip;
        // }
        // else
        // {
        //     Debug.Log($"Unknown entity type: \"{args[0]}\"");
        // }
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
        _context.GalaxyZones.Remove(zoneData.ID);
        
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
        
        var zone = _context.GalaxyZones[newZone.ID] = new SimplifiedZoneData
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
    private Guid _data;
    private Guid _orbit;
    private GameContext _context;
    private Mesh _mesh;
    private float _zoneScale;
    private float _size;

    public AsteroidBelt(GameContext context, MeshFilter meshFilter, MeshCollider collider, Guid data, int spritesheetWidth, int spritesheetHeight, float zoneScale)
    {
        Transform = collider.transform;
        _context = context;
        _data = data;
        _filter = meshFilter;
        _collider = collider;
        _zoneScale = zoneScale;
        var planetData = context.Cache.Get<PlanetData>(data);
        var orbitData = context.Cache.Get<OrbitData>(planetData.Orbit);
        _orbit = orbitData.Parent;
        _vertices = new Vector3[planetData.Asteroids.Length*4];
        _normals = new Vector3[planetData.Asteroids.Length*4];
        _uvs = new Vector2[planetData.Asteroids.Length*4];
        _indices = new int[planetData.Asteroids.Length*6];

        var maxDist = 0f;
        var spriteSize = float2(1f / spritesheetWidth, 1f / spritesheetHeight);
        // vertex order: bottom left, top left, top right, bottom right
        for (var i = 0; i < planetData.Asteroids.Length; i++)
        {
            if (planetData.Asteroids[i].Distance > maxDist)
                maxDist = planetData.Asteroids[i].Distance;
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
        _size = maxDist;

        _filter.mesh = _mesh;
        //_collider.sharedMesh = _mesh;
    }

    public void Update()
    {
        var planetData = _context.Cache.Get<PlanetData>(_data);
        var asteroidTransforms = _context.GetAsteroidTransforms(_data);
        for (var i = 0; i < planetData.Asteroids.Length; i++)
        {
            var rotation = Quaternion.Euler(0, 0, asteroidTransforms[i].z);
            var position = (Vector3) (Vector2) asteroidTransforms[i].xy * _zoneScale;
            _vertices[i * 4] = rotation * new Vector3(-asteroidTransforms[i].w * _zoneScale,-asteroidTransforms[i].w * _zoneScale,0) + position;
            _vertices[i * 4 + 1] = rotation * new Vector3(-asteroidTransforms[i].w * _zoneScale,asteroidTransforms[i].w * _zoneScale,0) + position;
            _vertices[i * 4 + 2] = rotation * new Vector3(asteroidTransforms[i].w * _zoneScale,asteroidTransforms[i].w * _zoneScale,0) + position;
            _vertices[i * 4 + 3] = rotation * new Vector3(asteroidTransforms[i].w * _zoneScale,-asteroidTransforms[i].w * _zoneScale,0) + position;
        }

        _mesh.bounds = new Bounds((Vector2) _context.GetOrbitPosition(_orbit) * _zoneScale, Vector3.one * (_size * _zoneScale * 2));
        _mesh.vertices = _vertices;
        _collider.sharedMesh = _mesh;
    }
}
