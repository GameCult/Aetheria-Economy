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
    public PlaceUIElementWorldspace ViewDot;
    public PlaceUIElementWorldspace TargetIndicator;
    public Prototype LockIndicator;
    public PlaceUIElementWorldspace[] Crosshairs;
    public EventLog EventLog;
    public GameSettings Settings;
    public SectorRenderer SectorRenderer;
    // public MapView MapView;
    public CinemachineVirtualCamera DockCamera;
    public CinemachineVirtualCamera FollowCamera;
    public GameObject GameplayUI;
    public MenuPanel Menu;
    public MapRenderer MenuMap;
    public SchematicDisplay SchematicDisplay;
    public SchematicDisplay TargetSchematicDisplay;
    public InventoryMenu Inventory;
    public InventoryPanel ShipPanel;
    public InventoryPanel TargetShipPanel;
    public ConfirmationDialog ConfirmationDialog;
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
    // private ShipInput _shipInput;
    private float2 _shipYawPitch;
    private float3 _viewDirection;
    private (HardpointData[] hardpoints, Transform[] barrels, PlaceUIElementWorldspace crosshair)[] ArticulationGroups;
    private (TargetLock targetLock, PlaceUIElementWorldspace indicator, Rotate spin)[] LockingIndicators;
    public List<Ship> PlayerShips { get; } = new List<Ship>();
    
    public DatabaseCache ItemData { get; private set; }
    public ItemManager ItemManager { get; private set; }
    public Zone Zone { get; private set; }

    private readonly (float2 direction, string name)[] _directions = {
        (float2(0, 1), "Front"),
        (float2(1, 0), "Right"),
        (float2(-1, 0), "Left"),
        (float2(0, -1), "Rear")
    };

    void Start()
    {
        // _transposer = DockCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        // _composer = DockCamera.GetCinemachineComponent<CinemachineComposer>();
        //_lookAt = new GameObject().transform;
        
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
                if (_currentShip != null && _currentShip.Parent == null)
                {
                    GameplayUI.SetActive(true);
                    
                    SchematicDisplay.ShowShip(_currentShip);
                    ShipPanel.Display(_currentShip, true);
                }
                
                return;
            }
            
            Cursor.lockState = CursorLockMode.None;
            _input.Player.Disable();
            GameplayUI.SetActive(false);
            Menu.ShowTab(MenuTab.Map);
            MenuMap.Position = _currentShip.Position.xz;
        };

        _input.Global.Inventory.performed += context =>
        {
            if (Menu.gameObject.activeSelf && Menu.CurrentTab == MenuTab.Inventory)
            {
                Cursor.lockState = CursorLockMode.Locked;
                _input.Player.Enable();
                Menu.gameObject.SetActive(false);
                if (_currentShip != null && _currentShip.Parent == null)
                {
                    GameplayUI.SetActive(true);
                    
                    SchematicDisplay.ShowShip(_currentShip);
                    ShipPanel.Display(_currentShip, true);
                }
                return;
            }

            Cursor.lockState = CursorLockMode.None;
            _input.Player.Disable();
            Menu.ShowTab(MenuTab.Inventory);
            GameplayUI.SetActive(false);
            var cargo = _currentShip.CargoBays.FirstOrDefault();
            if (cargo != null)
            {
                Inventory.GetPanel.Display(cargo);
            }
            else Inventory.GetPanel.Clear();
            Inventory.GetPanel.Display(_currentShip);
        };

        _input.Global.Dock.performed += context =>
        {
            if (_currentShip.Parent == null) Dock();
            else Undock();
        };

        _input.Player.TargetReticle.performed += context =>
        {
            var underReticle = Zone.Entities.Where(x => x != _currentShip).MaxBy(x => dot(normalize(x.Position - _currentShip.Position), _currentShip.LookDirection));
            _currentShip.Target.Value = _currentShip.Target.Value == underReticle ? null : underReticle;
        };

        _input.Player.TargetNearest.performed += context =>
        {
            _currentShip.Target.Value = Zone.Entities.Where(x=>x!=_currentShip).MaxBy(x => length(x.Position - _currentShip.Position));
        };

        _input.Player.TargetNext.performed += context =>
        {
            var targets = Zone.Entities.Where(x => x != _currentShip).OrderBy(x => length(x.Position - _currentShip.Position)).ToArray();
            var currentTargetIndex = Array.IndexOf(targets, _currentShip.Target.Value);
            _currentShip.Target.Value = targets[(currentTargetIndex + 1) % targets.Length];
        };

        _input.Player.TargetPrevious.performed += context =>
        {
            var targets = Zone.Entities.Where(x => x != _currentShip).OrderBy(x => length(x.Position - _currentShip.Position)).ToArray();
            var currentTargetIndex = Array.IndexOf(targets, _currentShip.Target.Value);
            _currentShip.Target.Value = targets[(currentTargetIndex + targets.Length - 1) % targets.Length];
        };

        #region Trigger Groups

        _input.Player.PreviousWeaponGroup.performed += context =>
        {
            if (_currentShip.Parent != null) return;
            SchematicDisplay.SelectedGroupIndex--;
        };

        _input.Player.NextWeaponGroup.performed += context =>
        {
            if (_currentShip.Parent != null) return;
            SchematicDisplay.SelectedGroupIndex++;
        };

        _input.Player.PreviousWeapon.performed += context =>
        {
            if (_currentShip.Parent != null) return;
            SchematicDisplay.SelectedItemIndex--;
        };

        _input.Player.NextWeapon.performed += context =>
        {
            if (_currentShip.Parent != null) return;
            SchematicDisplay.SelectedItemIndex++;
        };

        _input.Player.ToggleWeaponGroup.performed += context =>
        {
            if (_currentShip.Parent != null) return;
            var item = SchematicDisplay.SchematicItems[SchematicDisplay.SelectedItemIndex];
            if (_currentShip.TriggerGroups[SchematicDisplay.SelectedGroupIndex].Contains(item.Item))
                _currentShip.TriggerGroups[SchematicDisplay.SelectedGroupIndex].Remove(item.Item);
            else _currentShip.TriggerGroups[SchematicDisplay.SelectedGroupIndex].Add(item.Item);
            SchematicDisplay.UpdateTriggerGroups();
        };
        
        _input.Player.FireGroup1.started += context =>
        {
            if (_currentShip.Parent != null) return;
            foreach (var item in _currentShip.TriggerGroups[0])
            {
                foreach (var behaviors in item.BehaviorGroups)
                {
                    if (behaviors.Switch != null)
                        behaviors.Switch.Activated = true;
                    behaviors.Trigger?.Pull();
                }
            }
        };

        _input.Player.FireGroup1.canceled += context =>
        {
            if (_currentShip.Parent != null) return;
            foreach (var item in _currentShip.TriggerGroups[0])
            {
                foreach (var behaviors in item.BehaviorGroups)
                {
                    if (behaviors.Switch != null)
                        behaviors.Switch.Activated = false;
                }
            }
        };

        _input.Player.FireGroup2.started += context =>
        {
            if (_currentShip.Parent != null) return;
            foreach (var item in _currentShip.TriggerGroups[1])
            {
                foreach (var behaviors in item.BehaviorGroups)
                {
                    if (behaviors.Switch != null)
                        behaviors.Switch.Activated = true;
                    behaviors.Trigger?.Pull();
                }
            }
        };

        _input.Player.FireGroup2.canceled += context =>
        {
            if (_currentShip.Parent != null) return;
            foreach (var item in _currentShip.TriggerGroups[1])
            {
                foreach (var behaviors in item.BehaviorGroups)
                {
                    if (behaviors.Switch != null)
                        behaviors.Switch.Activated = false;
                }
            }
        };

        _input.Player.FireGroup3.started += context =>
        {
            if (_currentShip.Parent != null) return;
            foreach (var item in _currentShip.TriggerGroups[2])
            {
                foreach (var behaviors in item.BehaviorGroups)
                {
                    if (behaviors.Switch != null)
                        behaviors.Switch.Activated = true;
                    behaviors.Trigger?.Pull();
                }
            }
        };

        _input.Player.FireGroup3.canceled += context =>
        {
            if (_currentShip.Parent != null) return;
            foreach (var item in _currentShip.TriggerGroups[2])
            {
                foreach (var behaviors in item.BehaviorGroups)
                {
                    if (behaviors.Switch != null)
                        behaviors.Switch.Activated = false;
                }
            }
        };

        _input.Player.FireGroup4.started += context =>
        {
            if (_currentShip.Parent != null) return;
            foreach (var item in _currentShip.TriggerGroups[3])
            {
                foreach (var behaviors in item.BehaviorGroups)
                {
                    if (behaviors.Switch != null)
                        behaviors.Switch.Activated = true;
                    behaviors.Trigger?.Pull();
                }
            }
        };

        _input.Player.FireGroup4.canceled += context =>
        {
            if (_currentShip.Parent != null) return;
            foreach (var item in _currentShip.TriggerGroups[3])
            {
                foreach (var behaviors in item.BehaviorGroups)
                {
                    if (behaviors.Switch != null)
                        behaviors.Switch.Activated = false;
                }
            }
        };

        _input.Player.FireGroup5.started += context =>
        {
            if (_currentShip.Parent != null) return;
            foreach (var item in _currentShip.TriggerGroups[4])
            {
                foreach (var behaviors in item.BehaviorGroups)
                {
                    if (behaviors.Switch != null)
                        behaviors.Switch.Activated = true;
                    behaviors.Trigger?.Pull();
                }
            }
        };

        _input.Player.FireGroup5.canceled += context =>
        {
            if (_currentShip.Parent != null) return;
            foreach (var item in _currentShip.TriggerGroups[4])
            {
                foreach (var behaviors in item.BehaviorGroups)
                {
                    if (behaviors.Switch != null)
                        behaviors.Switch.Activated = false;
                }
            }
        };

        _input.Player.FireGroup6.started += context =>
        {
            if (_currentShip.Parent != null) return;
            foreach (var item in _currentShip.TriggerGroups[5])
            {
                foreach (var behaviors in item.BehaviorGroups)
                {
                    if (behaviors.Switch != null)
                        behaviors.Switch.Activated = true;
                    behaviors.Trigger?.Pull();
                }
            }
        };

        _input.Player.FireGroup6.canceled += context =>
        {
            if (_currentShip.Parent != null) return;
            foreach (var item in _currentShip.TriggerGroups[5])
            {
                foreach (var behaviors in item.BehaviorGroups)
                {
                    if (behaviors.Switch != null)
                        behaviors.Switch.Activated = false;
                }
            }
        };
        
        #endregion

        // ConsoleController.AddCommand("editmode", _ => ToggleEditMode());
        ConsoleController.AddCommand("savezone", args =>
        {
            if (args.Length > 0)
                Zone.Data.Name = args[0];
            SaveZone();
        });
        
        var stationType = ItemData.GetAll<HullData>().First(x=>x.HullType==HullType.Station);
        var stationHull = ItemManager.CreateInstance(stationType, 0, 1) as EquippableItem;
        var stationParent = Zone.PlanetInstances.Values.OrderByDescending(p => p.BodyData.Mass.Value).ElementAt(3);
        var stationParentOrbit = stationParent.Orbit.Data.ID;
        var stationParentPos = Zone.GetOrbitPosition(stationParentOrbit);
        var stationPos = stationParentPos + ItemManager.Random.NextFloat2Direction() * stationParent.GravityWellRadius.Value * .1f;
        var stationOrbit = Zone.CreateOrbit(stationParentOrbit, stationPos);
        var station = new OrbitalEntity(ItemManager, Zone, stationHull, stationOrbit.ID);
        Zone.Entities.Add(station);
        var dockingBayData = ItemData.GetAll<DockingBayData>().First();
        var dockingBay = ItemManager.CreateInstance(dockingBayData, 1, 1) as EquippableItem;
        station.TryEquip(dockingBay);
        station.Active = true;
        
        var reactorData = ItemData.GetAll<GearData>().First(x => x.HardpointType == HardpointType.Reactor && x.Shape.Height == 2);
        var autocannonData = ItemData.GetAll<GearData>().First(x => x.Name == "Autocannon");
        var ammoData = ItemData.GetAll<SimpleCommodityData>().First(x => x.Name == "AC2 Ammo");

        var turretType = ItemData.GetAll<HullData>().First(x=>x.HullType==HullType.Turret);
        var controlModuleData = ItemData.GetAll<GearData>().First(x => x.Name == "Murder Module");
        var cargo1Data = ItemData.GetAll<CargoBayData>().First(x => x.Name == "Cargo Bay 1x1");

        for(int i=0; i<5; i++)
        {
            var turretParent = Zone.PlanetInstances.Values.OrderByDescending(p => p.BodyData.Mass.Value).ElementAt(5+i);
            var turretParentOrbit = turretParent.Orbit.Data.ID;
            var turretParentPos = Zone.GetOrbitPosition(turretParentOrbit);
            var turretPos = turretParentPos + ItemManager.Random.NextFloat2Direction() * turretParent.GravityWellRadius.Value * .25f;
            var turretOrbit = Zone.CreateOrbit(turretParentOrbit, turretPos);
            var turret = new OrbitalEntity(ItemManager, Zone, ItemManager.CreateInstance(turretType, 0, 1) as EquippableItem, turretOrbit.ID);
            turret.TryEquip(ItemManager.CreateInstance(reactorData, .5f, 1) as EquippableItem);
            turret.TryEquip(ItemManager.CreateInstance(controlModuleData, .5f, 1) as EquippableItem);
            turret.TryEquip(ItemManager.CreateInstance(autocannonData, .5f, 1) as EquippableItem);
            turret.TryEquip(ItemManager.CreateInstance(autocannonData, .5f, 1) as EquippableItem);
            turret.TryEquip(ItemManager.CreateInstance(cargo1Data, .5f, 1) as EquippableItem);
            turret.CargoBays.First().TryStore(ItemManager.CreateInstance(ammoData, 400));
            turret.Active = true;
            Zone.Entities.Add(turret);
        }
        
        var hullType = ItemData.GetAll<HullData>().First(x=>x.Name == "Djinni");
        var shipHull = ItemManager.CreateInstance(hullType, 0, 1) as EquippableItem;
        var ship = new Ship(ItemManager, Zone, shipHull);
        PlayerShips.Add(ship);
        _currentShip = ship;
        _currentShip.Target.Subscribe(target =>
        {
            TargetIndicator.gameObject.SetActive(_currentShip.Target.Value != null);
            TargetShipPanel.gameObject.SetActive(target != null);
            if (target != null)
            {
                TargetShipPanel.Display(target, true);
                TargetSchematicDisplay.ShowShip(target, _currentShip);
            }
        });
        ship.ArmorDamage.Subscribe(hit =>
        {
            var direction = _directions.MaxBy(d => dot(d.direction, normalize(hit.pos - hullType.Shape.CenterOfMass)));
            EventLog.LogMessage(
                $"{direction.name} armor hit for {hit.damage} damage, currently {((int) (ship.Armor[hit.pos.x, hit.pos.y] / hullType.Armor * 100)).ToString()}%",
                Settings.ArmorHitColor);
        });
        ship.HullDamage.Subscribe(hit => EventLog.LogMessage($"Hull Structure hit for {hit} damage"));
        ship.ItemDamage.Subscribe(hit =>
        {
            var data = ItemManager.GetData(hit.item.EquippableItem);
            EventLog.LogMessage(
                $"{hit.item.EquippableItem.Name} hit for {hit.damage} damage, currently {((int) (hit.item.EquippableItem.Durability / data.Durability * 100)).ToString()}%",
                data.HardpointType == HardpointType.Thermal ? Settings.ThermalHitColor : Settings.GearHitColor);
        });
        ship.HardpointDamage.Subscribe(hit => EventLog.LogMessage(
                $"{Enum.GetName(typeof(HardpointType), hit.hardpoint.Type)} hardpoint hit for {hit.damage} damage, currently {((int) (ship.HardpointArmor[hit.hardpoint] / hit.hardpoint.Armor * 100)).ToString()}%",
                Settings.HardpointHitColor));

        var thrusterData = ItemData.GetAll<GearData>().First(x => x.HardpointType == HardpointType.Thruster && x.Shape.Height == 1);
        for (int i = 0; i < 8; i++)
        {
            ship.TryEquip(ItemManager.CreateInstance(thrusterData, .5f, 1) as EquippableItem);
            //dockingCargo.TryStore(ItemManager.CreateInstance(thrusterData, .5f, 1));
        }
        
        ship.TryEquip(ItemManager.CreateInstance(reactorData, .5f, 1) as EquippableItem);
        
        var cockpitData = ItemData.GetAll<GearData>().First(x => x.Behaviors.Any(b=>b is CockpitData));
        ship.TryEquip(ItemManager.CreateInstance(cockpitData, .5f, 1) as EquippableItem);

        ship.TryEquip(ItemManager.CreateInstance(autocannonData, .5f, 1) as EquippableItem);
        ship.TryEquip(ItemManager.CreateInstance(autocannonData, .5f, 1) as EquippableItem);

        var lrmmData = ItemData.GetAll<GearData>().First(x => x.Name == "LRMM72");
        ship.TryEquip(ItemManager.CreateInstance(lrmmData, .5f, 1) as EquippableItem);
        ship.TryEquip(ItemManager.CreateInstance(lrmmData, .5f, 1) as EquippableItem);

        var cargoData = ItemData.GetAll<CargoBayData>().First(x => x.Name == "Cargo Bay 4x4");
        ship.TryEquip(ItemManager.CreateInstance(cargoData, .5f, 1) as EquippableItem);
        
        ship.CargoBays.First().TryStore(ItemManager.CreateInstance(ammoData, 200));
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
            if (lengthsq(entity.Position.xz - _currentShip.Position.xz) <
                Settings.GameplaySettings.DockingDistance * Settings.GameplaySettings.DockingDistance)
            {
                var bay = entity.TryDock(_currentShip);
                if (bay != null)
                {
                    DockCamera.enabled = true;
                    FollowCamera.enabled = false;
                    _currentShip.Active = false;
                    var orbital = (OrbitalEntity) entity;
                    DockCamera.Follow = SectorRenderer.EntityInstances[orbital].Transform;
                    var parentOrbit = Zone.Orbits[orbital.OrbitData].Data.Parent;
                    var parentPlanet = SectorRenderer.Planets[Zone.Planets.FirstOrDefault(p => p.Value.Orbit == parentOrbit).Key];
                    DockCamera.LookAt = parentPlanet.Body.transform;
                    Menu.ShowTab(MenuTab.Inventory);
                    Inventory.GetPanel.Display(bay);
                    Inventory.GetPanel.Display(_currentShip);
                    Cursor.lockState = CursorLockMode.None;
                    _input.Player.Disable();
                    GameplayUI.SetActive(false);
                    if(LockingIndicators!=null) foreach(var (_, indicator, _) in LockingIndicators)
                        indicator.GetComponent<Prototype>().ReturnToPool();
                }
            }
        }
    }

    public void Undock()
    {
        if (_currentShip.Parent == null) return;
        if (_currentShip.GetBehavior<Cockpit>() == null)
        {
            ConfirmationDialog.Clear();
            ConfirmationDialog.Title.text = "Can't undock. Missing cockpit component!";
            ConfirmationDialog.Show();
        }
        else if (_currentShip.GetBehavior<Thruster>() == null)
        {
            ConfirmationDialog.Clear();
            ConfirmationDialog.Title.text = "Can't undock. Missing thruster component!";
            ConfirmationDialog.Show();
        }
        else if (_currentShip.GetBehavior<Reactor>() == null)
        {
            ConfirmationDialog.Clear();
            ConfirmationDialog.Title.text = "Can't undock. Missing reactor component!";
            ConfirmationDialog.Show();
        }
        else if (_currentShip.Parent.TryUndock(_currentShip))
        {
            Menu.gameObject.SetActive(false);
            _currentShip.Active = true;
            //_shipInput = new ShipInput(_input.Player, _currentShip);
            DockCamera.enabled = false;
            FollowCamera.enabled = true;
            FollowCamera.LookAt = SectorRenderer.EntityInstances[_currentShip].LookAtPoint;
            FollowCamera.Follow = SectorRenderer.EntityInstances[_currentShip].Transform;
            ArticulationGroups = _currentShip.Equipment
                .Where(item => item.Behaviors.Any(x => x.Data is WeaponData && !(x.Data is LauncherData)))
                .GroupBy(item => SectorRenderer.EntityInstances[_currentShip]
                    .GetBarrel(_currentShip.Hardpoints[item.Position.x, item.Position.y])
                    .GetComponentInParent<ArticulationPoint>()?.Group ?? -1)
                .Select((group, index) => {
                    return (
                        group.Select(item => _currentShip.Hardpoints[item.Position.x, item.Position.y]).ToArray(),
                        group.Select(item => SectorRenderer.EntityInstances[_currentShip].GetBarrel(_currentShip.Hardpoints[item.Position.x, item.Position.y])).ToArray(),
                        Crosshairs[index]
                    );
                }).ToArray();

            foreach (var crosshair in Crosshairs)
                crosshair.gameObject.SetActive(false);
            foreach (var group in ArticulationGroups)
                group.crosshair.gameObject.SetActive(true);

            LockingIndicators = _currentShip.GetBehaviors<TargetLock>().Select(x =>
            {
                var i = LockIndicator.Instantiate<PlaceUIElementWorldspace>();
                return (x, i, i.GetComponent<Rotate>());
            }).ToArray();


            Cursor.lockState = CursorLockMode.Locked;
            GameplayUI.SetActive(true);
            _input.Player.Enable();
            ShipPanel.Display(_currentShip, true);
            SchematicDisplay.ShowShip(_currentShip);
        }
        else
        {
            ConfirmationDialog.Title.text = "Can't undock. Must empty docking bay!";
            ConfirmationDialog.Show();
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
            if(_currentShip.Parent==null)
            {
                var look = _input.Player.Look.ReadValue<Vector2>();
                _shipYawPitch = float2(_shipYawPitch.x + look.x * Sensitivity.x, clamp(_shipYawPitch.y + look.y * Sensitivity.y, -.45f * PI, .45f * PI));
                _viewDirection = mul(float3(0, 0, 1), Unity.Mathematics.float3x3.Euler(float3(_shipYawPitch.yx, 0), RotationOrder.YXZ));
                _currentShip.LookDirection = _viewDirection;
                _currentShip.MovementDirection = _input.Player.Move.ReadValue<Vector2>();
            }
        }
        Zone.Update(_time, Time.deltaTime);
        SectorRenderer.Time = _time;
    }

    private void LateUpdate()
    {
        UpdateTargetIndicators();
    }

    private void UpdateTargetIndicators()
    {
        if (_currentShip == null || _currentShip.Parent != null) return;

        ViewDot.Target = SectorRenderer.EntityInstances[_currentShip].LookAtPoint.position;
        if (_currentShip.Target.Value != null)
            TargetIndicator.Target = _currentShip.Target.Value.Position;
        var distance = length((float3)ViewDot.Target - _currentShip.Position);
        foreach (var (_, barrels, crosshair) in ArticulationGroups)
        {
            var averagePosition = Vector3.zero;
            foreach (var barrel in barrels)
                averagePosition += barrel.position + barrel.forward * distance;
            averagePosition /= barrels.Length;
            crosshair.Target = averagePosition;
        }
        foreach (var (targetLock, indicator, spin) in LockingIndicators)
        {
            var showLockingIndicator = targetLock.Lock > .01f && _currentShip.Target.Value != null;
            indicator.gameObject.SetActive(showLockingIndicator);
            if(showLockingIndicator)
            indicator.Target = _currentShip.Target.Value.Position;
            indicator.NoiseAmplitude = Settings.GameplaySettings.LockIndicatorNoiseAmplitude * (1 - targetLock.Lock);
            indicator.NoiseFrequency = Settings.GameplaySettings.LockIndicatorFrequency.Evaluate(targetLock.Lock);
            spin.Speed = Settings.GameplaySettings.LockSpinSpeed.Evaluate(targetLock.Lock);
        }
    }
}