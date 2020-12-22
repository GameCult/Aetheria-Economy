/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cinemachine;
using MessagePack;
using TMPro;
using UniRx;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;

public class ActionGameManager : MonoBehaviour
{
    public GameSettings Settings;
    public SectorRenderer SectorRenderer;
    // public MapView MapView;
    public CinemachineVirtualCamera DockCamera;
    public CinemachineVirtualCamera FollowCamera;
    public GameObject GameplayUI;
    public MenuPanel Menu;
    public InventoryMenu Inventory;
    public float2 Sensitivity;
    
    //public PlayerInput Input;
    
    // private CinemachineFramingTransposer _transposer;
    // private CinemachineComposer _composer;
    private DirectoryInfo _filePath;
    private bool _editMode;
    private float _time;
    private AetheriaInput _input;
    private int _zoomLevelIndex;
    private Ship _currentShip;
    private ShipInput _shipInput;
    private float2 _shipYawPitch;
    private float3 _viewDirection;
    private Transform _lookAt;
    public List<Ship> PlayerShips { get; } = new List<Ship>();
    
    public DatabaseCache ItemData { get; private set; }
    public ItemManager ItemManager { get; private set; }
    public Zone Zone { get; private set; }


    void Start()
    {
        // _transposer = DockCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        // _composer = DockCamera.GetCinemachineComponent<CinemachineComposer>();
        _lookAt = new GameObject().transform;
        
        ConsoleController.MessageReceiver = this;
        _filePath = new DirectoryInfo(Application.dataPath).Parent.CreateSubdirectory("GameData");
        ItemData = new DatabaseCache();
        ItemData.Load(_filePath.FullName);
        ItemManager = new ItemManager(ItemData, Settings.GameplaySettings, Debug.Log);

        var zoneFile = Path.Combine(_filePath.FullName, "Home.zone");
        
        // If the game has already been run, there will be a Home file containing a ZonePack; if not, generate one
        var zonePack = File.Exists(zoneFile) ? 
            MessagePackSerializer.Deserialize<ZonePack>(File.ReadAllBytes(zoneFile)): 
            ZoneGenerator.GenerateZone(
                settings: Settings.ZoneSettings,
                // mapLayers: Context.MapLayers,
                // resources: Context.Resources,
                mass: Settings.DefaultZoneMass,
                radius: Settings.DefaultZoneRadius
            );
        
        Zone = new Zone(Settings.PlanetSettings, zonePack);
        SectorRenderer.ItemManager = ItemManager;
        SectorRenderer.LoadZone(Zone);

        _input = new AetheriaInput();
        _input.Player.Enable();
        _input.Global.Enable();
        _input.UI.Enable();

        _zoomLevelIndex = Settings.DefaultMinimapZoom;
        _input.Player.MinimapZoom.performed += context =>
        {
            _zoomLevelIndex = (_zoomLevelIndex + 1) % Settings.MinimapZoomLevels.Length;
            SectorRenderer.MinimapDistance = Settings.MinimapZoomLevels[_zoomLevelIndex];
        };

        _input.Global.MapToggle.performed += context =>
        {
            if (Menu.gameObject.activeSelf && Menu.CurrentTab == MenuTab.Map)
            {
                Cursor.lockState = CursorLockMode.Locked;
                _input.Player.Enable();
                Menu.gameObject.SetActive(false);
                return;
            }
            
            Cursor.lockState = CursorLockMode.None;
            _input.Player.Disable();
            Menu.ShowTab(MenuTab.Map);
        };

        _input.Global.Inventory.performed += context =>
        {
            if (Menu.gameObject.activeSelf && Menu.CurrentTab == MenuTab.Inventory)
            {
                Cursor.lockState = CursorLockMode.Locked;
                _input.Player.Enable();
                Menu.gameObject.SetActive(false);
                return;
            }

            Cursor.lockState = CursorLockMode.None;
            _input.Player.Disable();
            Menu.ShowTab(MenuTab.Inventory);
        };

        _input.Global.Dock.performed += context =>
        {
            if (_currentShip.Parent == null) Dock();
            else Undock();
        };

        // ConsoleController.AddCommand("editmode", _ => ToggleEditMode());
        ConsoleController.AddCommand("savezone", args =>
        {
            if (args.Length > 0)
                Zone.Data.Name = args[0];
            SaveZone();
        });
        
        var stationType = ItemData.GetAll<HullData>().First(x=>x.HullType==HullType.Station);
        var stationHull = ItemManager.CreateInstance(stationType, 0, 1) as EquippableItem;
        var stationParent = Zone.PlanetInstances.Values.OrderByDescending(p => p.BodyData.Mass.Value).ElementAt(2);
        var parentOrbit = stationParent.Orbit.Data.ID;
        var stationPos = Zone.GetOrbitPosition(parentOrbit) + 
            normalize(ItemManager.Random.NextFloat2() - float2(.5f, .5f)) * ItemManager.Random.NextFloat() * stationParent.GravityWellRadius.Value * .75f;
        var stationOrbit = Zone.CreateOrbit(parentOrbit, stationPos);
        var station = new OrbitalEntity(ItemManager, Zone, stationHull, stationOrbit.ID);
        Zone.Entities.Add(station);
        var dockingBayData = ItemData.GetAll<DockingBayData>().First();
        var dockingBay = ItemManager.CreateInstance(dockingBayData, 1, 1) as EquippableItem;
        station.TryEquip(dockingBay);
        station.Active = true;
        
        var hullType = ItemData.GetAll<HullData>().First(x=>x.HullType==HullType.Ship);
        var shipHull = ItemManager.CreateInstance(hullType, 0, 1) as EquippableItem;
        var ship = new Ship(ItemManager, Zone, shipHull);
        PlayerShips.Add(ship);
        _currentShip = ship;

        var dockingCargo = station.DockingBays.First();

        var thrusterData = ItemData.GetAll<GearData>().First(x => x.HardpointType == HardpointType.Thruster && x.Shape.Height == 1);
        for (int i = 0; i < 8; i++)
        {
            ship.TryEquip(ItemManager.CreateInstance(thrusterData, .5f, 1) as EquippableItem);
            //dockingCargo.TryStore(ItemManager.CreateInstance(thrusterData, .5f, 1));
        }
        
        var reactorData = ItemData.GetAll<GearData>().First(x => x.HardpointType == HardpointType.Reactor && x.Shape.Height == 2);
        ship.TryEquip(ItemManager.CreateInstance(reactorData, .5f, 1) as EquippableItem);
        //dockingCargo.TryStore(ItemManager.CreateInstance(reactorData, .5f, 1));
        
        Dock();

        // MapButton.onClick.AddListener(() =>
        // {
        //     if (_currentMenuTabButton == MapPanel) return;
        //     
        //     _currentMenuTabButton.GetComponent<TextMeshProUGUI>().color = Color.white;
        //     _currentMenuTabButton = MapButton;
        // });
        //
        //InventoryPanel.Display(ItemManager, entity);
    }

    public void Dock()
    {
        if (_currentShip.Parent != null) return;
        foreach (var entity in Zone.Entities.ToArray())
        {
            if (lengthsq(entity.Position - _currentShip.Position) <
                Settings.GameplaySettings.DockingDistance * Settings.GameplaySettings.DockingDistance)
            {
                var bay = entity.TryDock(_currentShip);
                if (bay != null)
                {
                    DockCamera.enabled = true;
                    FollowCamera.enabled = false;
                    _currentShip.Active = false;
                    var orbital = (OrbitalEntity) entity;
                    DockCamera.Follow = SectorRenderer.Orbitals[orbital];
                    var parentOrbit = Zone.Orbits[orbital.OrbitData].Data.Parent;
                    var parentPlanet = SectorRenderer.Planets[Zone.Planets.FirstOrDefault(p => p.Value.Orbit == parentOrbit).Key];
                    DockCamera.LookAt = parentPlanet.Body.transform;
                    Menu.ShowTab(MenuTab.Inventory);
                    Inventory.GetPanel.Display(bay);
                    Inventory.GetPanel.Display(_currentShip);
                    _shipInput = null;
                    Cursor.lockState = CursorLockMode.None;
                    _input.Player.Disable();
                }
            }
        }
    }

    public void Undock()
    {
        if (_currentShip.Parent == null) return;
        if (_currentShip.Parent.TryUndock(_currentShip))
        {
            Menu.gameObject.SetActive(false);
            _currentShip.Active = true;
            _shipInput = new ShipInput(_input.Player, _currentShip);
            DockCamera.enabled = false;
            FollowCamera.enabled = true;
            FollowCamera.LookAt = _lookAt;
            FollowCamera.Follow = SectorRenderer.Ships[_currentShip].Transform;
            Cursor.lockState = CursorLockMode.Locked;
            _input.Player.Enable();
        }
    }

    public void SaveZone() => File.WriteAllBytes(
        Path.Combine(_filePath.FullName, $"{Zone.Data.Name}.zone"), MessagePackSerializer.Serialize(Zone.Pack()));

    // public void ToggleEditMode()
    // {
    //     _editMode = !_editMode;
    //     FollowCamera.gameObject.SetActive(!_editMode);
    //     TopDownCamera.gameObject.SetActive(_editMode);
    // }
    
    public IEnumerable<EquippedCargoBay> AvailableCargoBays()
    {
        if (_currentShip.Parent != null)
        {
            foreach (var bay in _currentShip.Parent.DockingBays)
            {
                if (PlayerShips.Contains(bay.DockedShip)) yield return bay;
            }
        }
    }

    public IEnumerable<Ship> AvailableShips()
    {
        if (_currentShip.Parent != null)
        {
            foreach (var ship in PlayerShips)
            {
                if (_currentShip.Parent.Children.Contains(ship)) yield return ship;
            }
        }
        else 
            yield return _currentShip;
    }

    void Update()
    {
        if(!_editMode)
        {
            _time += Time.deltaTime;
            ItemManager.Time = Time.time;
            if(_shipInput!=null)
            {
                var look = _input.Player.Look.ReadValue<Vector2>();
                _shipYawPitch = float2(_shipYawPitch.x + look.x * Sensitivity.x, clamp(_shipYawPitch.y + look.y * Sensitivity.y, -.45f * PI, .45f * PI));
                _viewDirection = mul(float3(0, 0, 1), Unity.Mathematics.float3x3.Euler(float3(_shipYawPitch.yx, 0), RotationOrder.YXZ));
                _lookAt.position = SectorRenderer.Ships[_currentShip].Transform.position + (Vector3) _viewDirection * 100;
                _shipInput.DesiredOrientation = _viewDirection.xz;
                _shipInput.Update();
            }
        }
        Zone.Update(_time, Time.deltaTime);
        SectorRenderer.Time = _time;
    }
}

public class ShipInput
{
    private AetheriaInput.PlayerActions _input;
    private Ship _ship;
    private Thruster[] _allThrusters;
    private Thruster[] _forwardThrusters;
    private Thruster[] _reverseThrusters;
    private Thruster[] _rightThrusters;
    private Thruster[] _leftThrusters;
    private Thruster[] _clockwiseThrusters;
    private Thruster[] _counterClockwiseThrusters;
    
    public float2 DesiredOrientation { get; set; }
    public float DesiredPitch { get; set; }

    public ShipInput(AetheriaInput.PlayerActions input, Ship ship)
    {
        _input = input;
        _ship = ship;
        _allThrusters = ship.GetBehaviors<Thruster>().ToArray();
        _forwardThrusters = _allThrusters.Where(x => x.Item.EquippableItem.Rotation == ItemRotation.Reversed).ToArray();
        _reverseThrusters = _allThrusters.Where(x => x.Item.EquippableItem.Rotation == ItemRotation.None).ToArray();
        _rightThrusters = _allThrusters.Where(x => x.Item.EquippableItem.Rotation == ItemRotation.CounterClockwise).ToArray();
        _leftThrusters = _allThrusters.Where(x => x.Item.EquippableItem.Rotation == ItemRotation.Clockwise).ToArray();
        _counterClockwiseThrusters = _allThrusters.Where(x => x.Torque < -.05f).ToArray();
        _clockwiseThrusters = _allThrusters.Where(x => x.Torque > .05f).ToArray();
    }

    public void Update()
    {
        var move = _input.Move.ReadValue<Vector2>();
        
        foreach (var thruster in _allThrusters) thruster.Axis = 0;
        foreach (var thruster in _rightThrusters) thruster.Axis += move.x;
        foreach (var thruster in _leftThrusters) thruster.Axis += -move.x;
        foreach (var thruster in _forwardThrusters) thruster.Axis += move.y;
        foreach (var thruster in _reverseThrusters) thruster.Axis += -move.y;
        
        var deltaRot = dot(normalize(DesiredOrientation), normalize(_ship.Direction).Rotate(ItemRotation.Clockwise));
        if (abs(deltaRot) < .01) deltaRot = 0;
        deltaRot = pow(abs(deltaRot), .5f) * sign(deltaRot);
        
        foreach (var thruster in _clockwiseThrusters) thruster.Axis += deltaRot;
        foreach (var thruster in _counterClockwiseThrusters) thruster.Axis += -deltaRot;
    }
}