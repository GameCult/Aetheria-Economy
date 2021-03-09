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
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;
using quaternion = Unity.Mathematics.quaternion;
using Random = UnityEngine.Random;

public class ActionGameManager : MonoBehaviour
{
    public string StarterShipTemplate = "Longinus";
    public float TargetSpottedBlinkFrequency = 20;
    public float TargetSpottedBlinkOffset = -.25f;
    public float DeathPPTransitionTime;
    public PostProcessVolume DeathPP;
    public PostProcessVolume HeatstrokePP;
    public PostProcessVolume SevereHeatstrokePP;
    public Prototype HostileTargetIndicator;
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
    public int Credits = 15000000;
    
    //public PlayerInput Input;
    
    // private CinemachineFramingTransposer _transposer;
    // private CinemachineComposer _composer;
    
    public PlayerSettings PlayerSettings { get; private set; }
    private DirectoryInfo _filePath;
    private DirectoryInfo _loadoutPath;
    private bool _editMode;
    private float _time;
    private AetheriaInput _input;
    private int _zoomLevelIndex;
    private Ship _currentShip;

    // private ShipInput _shipInput;
    private float2 _shipYawPitch;
    private float3 _viewDirection;
    private (HardpointData[] hardpoints, Transform[] barrels, PlaceUIElementWorldspace crosshair)[] _articulationGroups;
    private (LockWeapon targetLock, PlaceUIElementWorldspace indicator, Rotate spin)[] _lockingIndicators;
    private Dictionary<Entity, VisibleHostileIndicator> _visibleHostileIndicators = new Dictionary<Entity, VisibleHostileIndicator>();
    private List<IDisposable> _shipSubscriptions = new List<IDisposable>();
    private float _severeHeatstrokePhase;
    
    public List<Entity> PlayerEntities { get; } = new List<Entity>();
    public EquippedDockingBay DockingBay { get; private set; }
    public Entity DockedEntity { get; private set; }

    public Ship CurrentShip
    {
        get => _currentShip;
        set => _currentShip = value;
    }

    public DatabaseCache ItemData { get; private set; }
    public ItemManager ItemManager { get; private set; }
    public Zone Zone { get; private set; }
    public List<EntityPack> Loadouts { get; } = new List<EntityPack>();

    private readonly (float2 direction, string name)[] _directions = {
        (float2(0, 1), "Front"),
        (float2(1, 0), "Right"),
        (float2(-1, 0), "Left"),
        (float2(0, -1), "Rear")
    };


    public void SaveLoadout(EntityPack pack)
    {
        File.WriteAllBytes(Path.Combine(_loadoutPath.FullName, $"{pack.Name}.preset"), MessagePackSerializer.Serialize(pack));
    }

    public void GenerateLevel()
    {
        var zonePack = ZoneGenerator.GenerateZone(
            settings: Settings.ZoneSettings,
            // mapLayers: Context.MapLayers,
            // resources: Context.Resources,
            mass: Settings.DefaultZoneMass,
            radius: Settings.DefaultZoneRadius
        );
        
        Zone = new Zone(Settings.PlanetSettings, zonePack);
        Zone.Log = s => Debug.Log($"Zone: {s}");

        if (CurrentShip != null)
        {
            CurrentShip.Deactivate();
            if (CurrentShip.Zone != null)
            {
                CurrentShip.Zone.Entities.Remove(CurrentShip);
            }
            CurrentShip.Zone = Zone;
            Zone.Entities.Add(CurrentShip);
            CurrentShip.Activate();
        }
        
        SectorRenderer.LoadZone(Zone);
        
        if (CurrentShip != null)
        {
            DisposeEntityBinding();
            BindToEntity();
        }
        
        var testCorp = new MegaCorporation();
        testCorp.Name = "TestCorp";
        testCorp.PlayerHostile = true;
        
        var stationType = ItemData.GetAll<HullData>().First(x=>x.HullType==HullType.Station);
        var stationHull = ItemManager.CreateInstance(stationType) as EquippableItem;
        var stationParent = Zone.PlanetInstances.Values.OrderByDescending(p => p.BodyData.Mass.Value).ElementAt(3);
        var stationParentOrbit = stationParent.Orbit.Data.ID;
        var stationParentPos = Zone.GetOrbitPosition(stationParentOrbit);
        var stationPos = stationParentPos + ItemManager.Random.NextFloat2Direction() * stationParent.GravityWellRadius.Value * .1f;
        var stationOrbit = Zone.CreateOrbit(stationParentOrbit, stationPos);
        var station = new OrbitalEntity(ItemManager, Zone, stationHull, stationOrbit.ID);
        //station.Faction = testCorp;
        Zone.Entities.Add(station);
        var dockingBayData = ItemData.GetAll<DockingBayData>().First();
        var dockingBay = ItemManager.CreateInstance(dockingBayData) as EquippableItem;
        station.TryEquip(dockingBay);
        station.Activate();

        var turretLoadout = Loadouts.First(x => x.Name == "Turret");

        for(int i=0; i<3; i++)
        {
            var planet = Zone.PlanetInstances.Values.OrderByDescending(p => p.BodyData.Mass.Value).ElementAt(4+i);
            var planetOrbit = planet.Orbit.Data.ID;
            var planetPos = Zone.GetOrbitPosition(planetOrbit);
            for (int j = 0; j < 2; j++)
            {
                var pos = planetPos + ItemManager.Random.NextFloat2Direction() * planet.GravityWellRadius.Value * (.1f + .1f * j);
                var orbit = Zone.CreateOrbit(planetOrbit, pos);
                var turret = EntityPack.Unpack(ItemManager, Zone, turretLoadout, orbit.ID, true);
                turret.Activate();
                turret.Name = $"Turret {i}-{(char) ('A' + j)}";
                turret.Faction = testCorp;
                Zone.Entities.Add(turret);
            }
        }

        var enemyShipLoadout = Loadouts.First(x => ((HullData) ItemManager.GetData(x.Hull)).HullType == HullType.Ship);
        var enemyShip = EntityPack.Unpack(ItemManager, Zone, enemyShipLoadout, true);
        enemyShip.Faction = testCorp;
        enemyShip.Activate();
        Zone.Entities.Add(enemyShip);
        Zone.Agents.Add(CreateAgent(enemyShip));
    }

    private Agent CreateAgent(Ship ship)
    {
        var agent = new Minion(ship);
        var task = new PatrolOrbitsTask();
        task.Circuit = Zone.Orbits.OrderBy(_ => Random.value).Take(4).Select(x => x.Key).ToArray();
        agent.Task = task;
        return agent;
    }

    void Start()
    {
        AkSoundEngine.RegisterGameObj(gameObject);
        
        ConsoleController.MessageReceiver = this;
        _filePath = new DirectoryInfo(Application.dataPath).Parent.CreateSubdirectory("GameData");
        _loadoutPath = _filePath.CreateSubdirectory("Loadouts");
        
        ItemData = new DatabaseCache();
        ItemData.Load(Path.Combine(_filePath.FullName, "AetherDB.msgpack"));
        ItemManager = new ItemManager(ItemData, Settings.GameplaySettings, Debug.Log);
        SectorRenderer.ItemManager = ItemManager;

        FileInfo playerSettingsFile = new FileInfo(Path.Combine(_filePath.FullName, "PlayerSettings.msgpack"));
        if (!playerSettingsFile.Exists)
        {
            File.WriteAllBytes(playerSettingsFile.FullName, MessagePackSerializer.Serialize(Settings.DefaultPlayerSettings));
        }
        PlayerSettings = MessagePackSerializer.Deserialize<PlayerSettings>(
            File.ReadAllBytes(playerSettingsFile.FullName));

        Loadouts.AddRange(_loadoutPath.EnumerateFiles("*.preset")
            .Select(fi => MessagePackSerializer.Deserialize<EntityPack>(File.ReadAllBytes(fi.FullName))));

        //var zoneFile = Path.Combine(_filePath.FullName, "Home.zone");

        #region Input Handling

        _input = new AetheriaInput();
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
                Menu.gameObject.SetActive(false);
                if (CurrentShip != null && CurrentShip.Parent == null)
                {
                    _input.Player.Enable();
                    GameplayUI.SetActive(true);
                    
                    SchematicDisplay.ShowShip(CurrentShip);
                    ShipPanel.Display(CurrentShip, true);
                }
                
                return;
            }
            
            Cursor.lockState = CursorLockMode.None;
            _input.Player.Disable();
            GameplayUI.SetActive(false);
            Menu.ShowTab(MenuTab.Map);
            MenuMap.Position = CurrentShip.Position.xz;
        };

        _input.Global.Inventory.performed += context =>
        {
            if (Menu.gameObject.activeSelf && Menu.CurrentTab == MenuTab.Inventory)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Menu.gameObject.SetActive(false);
                if (CurrentShip != null && CurrentShip.Parent == null)
                {
                    _input.Player.Enable();
                    GameplayUI.SetActive(true);
                    
                    SchematicDisplay.ShowShip(CurrentShip);
                    ShipPanel.Display(CurrentShip, true);
                }
                return;
            }

            Cursor.lockState = CursorLockMode.None;
            _input.Player.Disable();
            Menu.ShowTab(MenuTab.Inventory);
            GameplayUI.SetActive(false);
        };

        _input.Global.Dock.performed += context =>
        {
            if (CurrentShip == null)
            {
                AkSoundEngine.PostEvent("UI_Fail", gameObject);
                ConfirmationDialog.Clear();
                ConfirmationDialog.Title.text = "Can't undock. You dont have a ship!";
                ConfirmationDialog.Show();
            }
            else if (CurrentShip.Parent == null) Dock();
            else Undock();
        };

        _input.Player.EnterWormhole.performed += context =>
        {
            foreach (var wormhole in SectorRenderer.WormholeInstances.Keys)
            {
                if (length(wormhole.Position - CurrentShip.Position.xz) < Settings.GameplaySettings.WormholeExitRadius)
                {
                    CurrentShip.EnterWormhole(wormhole.Position);
                    CurrentShip.OnEnteredWormhole += () =>
                    {
                        GenerateLevel();
                        CurrentShip.ExitWormhole(SectorRenderer.WormholeInstances.Keys.First().Position,
                            Settings.GameplaySettings.WormholeExitVelocity * ItemManager.Random.NextFloat2Direction());
                        CurrentShip.Zone = Zone;
                    };
                }
            }
        };

        _input.Player.OverrideShutdown.performed += context =>
        {
            CurrentShip.OverrideShutdown = !CurrentShip.OverrideShutdown;
        };

        _input.Player.ToggleHeatsinks.performed += context =>
        {
            CurrentShip.HeatsinksEnabled = !CurrentShip.HeatsinksEnabled;
            AkSoundEngine.PostEvent(CurrentShip.HeatsinksEnabled ? "UI_Success" : "UI_Fail", gameObject);
        };

        _input.Player.ToggleShield.performed += context =>
        {
            if (CurrentShip.Shield != null)
            {
                CurrentShip.Shield.Item.Enabled.Value = !CurrentShip.Shield.Item.Enabled.Value;
                AkSoundEngine.PostEvent(CurrentShip.Shield.Item.Enabled.Value ? "UI_Success" : "UI_Fail", gameObject);
            }
        };

        #region Targeting

        _input.Player.TargetReticle.performed += context =>
        {
            var underReticle = Zone.Entities.Where(x => x != CurrentShip)
                .MaxBy(x => dot(normalize(x.Position - CurrentShip.Position), CurrentShip.LookDirection));
            CurrentShip.Target.Value = CurrentShip.Target.Value == underReticle ? null : underReticle;
        };

        _input.Player.TargetNearest.performed += context =>
        {
            CurrentShip.Target.Value = Zone.Entities.Where(x=>x!=CurrentShip)
                .MaxBy(x => length(x.Position - CurrentShip.Position));
        };

        _input.Player.TargetNext.performed += context =>
        {
            var targets = Zone.Entities.Where(x => x != CurrentShip).OrderBy(x => length(x.Position - CurrentShip.Position)).ToArray();
            var currentTargetIndex = Array.IndexOf(targets, CurrentShip.Target.Value);
            CurrentShip.Target.Value = targets[(currentTargetIndex + 1) % targets.Length];
        };

        _input.Player.TargetPrevious.performed += context =>
        {
            var targets = Zone.Entities.Where(x => x != CurrentShip).OrderBy(x => length(x.Position - CurrentShip.Position)).ToArray();
            var currentTargetIndex = Array.IndexOf(targets, CurrentShip.Target.Value);
            CurrentShip.Target.Value = targets[(currentTargetIndex + targets.Length - 1) % targets.Length];
        };
        
        #endregion


        #region Trigger Groups

        _input.Player.PreviousWeaponGroup.performed += context =>
        {
            if (CurrentShip.Parent != null) return;
            SchematicDisplay.SelectedGroupIndex--;
        };

        _input.Player.NextWeaponGroup.performed += context =>
        {
            if (CurrentShip.Parent != null) return;
            SchematicDisplay.SelectedGroupIndex++;
        };

        _input.Player.PreviousWeapon.performed += context =>
        {
            if (CurrentShip.Parent != null) return;
            SchematicDisplay.SelectedItemIndex--;
        };

        _input.Player.NextWeapon.performed += context =>
        {
            if (CurrentShip.Parent != null) return;
            SchematicDisplay.SelectedItemIndex++;
        };

        _input.Player.ToggleWeaponGroup.performed += context =>
        {
            if (CurrentShip.Parent != null) return;
            var item = SchematicDisplay.SchematicItems[SchematicDisplay.SelectedItemIndex];
            if (CurrentShip.TriggerGroups[SchematicDisplay.SelectedGroupIndex].items.Contains(item.Item))
                CurrentShip.TriggerGroups[SchematicDisplay.SelectedGroupIndex].items.Remove(item.Item);
            else CurrentShip.TriggerGroups[SchematicDisplay.SelectedGroupIndex].items.Add(item.Item);
            foreach (var group in item.Item.BehaviorGroups.Values)
            {
                var weapon = group.GetBehavior<Weapon>();
                if(weapon != null)
                {
                    if (CurrentShip.TriggerGroups[SchematicDisplay.SelectedGroupIndex].weapons.Contains(weapon))
                        CurrentShip.TriggerGroups[SchematicDisplay.SelectedGroupIndex].weapons.Remove(weapon);
                    else CurrentShip.TriggerGroups[SchematicDisplay.SelectedGroupIndex].weapons.Add(weapon);
                }
            }
            SchematicDisplay.UpdateTriggerGroups();
        };
        
        _input.Player.FireGroup1.started += context =>
        {
            if (CurrentShip.Parent != null) return;
            foreach (var s in CurrentShip.TriggerGroups[0].weapons) s.Activate();
        };

        _input.Player.FireGroup1.canceled += context =>
        {
            if (CurrentShip.Parent != null) return;
            foreach (var s in CurrentShip.TriggerGroups[0].weapons) s.Deactivate();
        };

        _input.Player.FireGroup2.started += context =>
        {
            if (CurrentShip.Parent != null) return;
            foreach (var s in CurrentShip.TriggerGroups[1].weapons) s.Activate();
        };

        _input.Player.FireGroup2.canceled += context =>
        {
            if (CurrentShip.Parent != null) return;
            foreach (var s in CurrentShip.TriggerGroups[1].weapons) s.Deactivate();
        };

        _input.Player.FireGroup3.started += context =>
        {
            if (CurrentShip.Parent != null) return;
            foreach (var s in CurrentShip.TriggerGroups[2].weapons) s.Activate();
        };

        _input.Player.FireGroup3.canceled += context =>
        {
            if (CurrentShip.Parent != null) return;
            foreach (var s in CurrentShip.TriggerGroups[2].weapons) s.Deactivate();
        };

        _input.Player.FireGroup4.started += context =>
        {
            if (CurrentShip.Parent != null) return;
            foreach (var s in CurrentShip.TriggerGroups[3].weapons) s.Activate();
        };

        _input.Player.FireGroup4.canceled += context =>
        {
            if (CurrentShip.Parent != null) return;
            foreach (var s in CurrentShip.TriggerGroups[3].weapons) s.Deactivate();
        };

        _input.Player.FireGroup5.started += context =>
        {
            if (CurrentShip.Parent != null) return;
            foreach (var s in CurrentShip.TriggerGroups[4].weapons) s.Activate();
        };

        _input.Player.FireGroup5.canceled += context =>
        {
            if (CurrentShip.Parent != null) return;
            foreach (var s in CurrentShip.TriggerGroups[4].weapons) s.Deactivate();
        };

        _input.Player.FireGroup6.started += context =>
        {
            if (CurrentShip.Parent != null) return;
            foreach (var s in CurrentShip.TriggerGroups[5].weapons) s.Activate();
        };

        _input.Player.FireGroup6.canceled += context =>
        {
            if (CurrentShip.Parent != null) return;
            foreach (var s in CurrentShip.TriggerGroups[5].weapons) s.Deactivate();
        };
        
        #endregion

        #endregion
        
        StartGame();
        // ConsoleController.AddCommand("editmode", _ => ToggleEditMode());
        ConsoleController.AddCommand("savezone", args =>
        {
            if (args.Length > 0)
                Zone.Data.Name = args[0];
            SaveZone();
        });

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

    private void StartGame()
    {
        GenerateLevel();
        CurrentShip = EntityPack.Unpack(ItemManager, Zone, Loadouts.First(x => x.Name == StarterShipTemplate), true);
        CurrentShip.Zone = Zone;
        Zone.Entities.Add(CurrentShip);
        CurrentShip.Activate();
        BindToEntity();
        CurrentShip.ExitWormhole(
            SectorRenderer.WormholeInstances.Keys.First().Position,
            ItemManager.Random.NextFloat2Direction() * Settings.GameplaySettings.WormholeExitVelocity);
        EnterOverworld();
    }

    public void Dock()
    {
        if (CurrentShip.Parent != null) return;
        foreach (var entity in Zone.Entities.ToArray())
        {
            if (lengthsq(entity.Position.xz - CurrentShip.Position.xz) <
                Settings.GameplaySettings.DockingDistance * Settings.GameplaySettings.DockingDistance)
            {
                var bay = entity.TryDock(CurrentShip);
                if (bay != null)
                {
                    DisposeEntityBinding();
                    DockedEntity = entity;
                    SectorRenderer.PerspectiveEntity = DockedEntity;
                    DockingBay = bay;
                    DockCamera.enabled = true;
                    FollowCamera.enabled = false;
                    var orbital = (OrbitalEntity) entity;
                    DockCamera.Follow = SectorRenderer.EntityInstances[orbital].transform;
                    var parentOrbit = Zone.Orbits[orbital.OrbitData].Data.Parent;
                    var parentPlanet = SectorRenderer.Planets[Zone.Planets.FirstOrDefault(p => p.Value.Orbit == parentOrbit).Key];
                    DockCamera.LookAt = parentPlanet.Body.transform;
                    Menu.ShowTab(MenuTab.Inventory);
                    AkSoundEngine.PostEvent("Dock", gameObject);
                    return;
                }
            }
        }
        AkSoundEngine.PostEvent("Dock_Fail", gameObject);
    }

    public void Undock()
    {
        if (CurrentShip.Parent == null) return;
        if (CurrentShip.GetBehavior<Cockpit>() == null)
        {
            ConfirmationDialog.Clear();
            ConfirmationDialog.Title.text = "Can't undock. Missing cockpit component!";
            ConfirmationDialog.Show();
            AkSoundEngine.PostEvent("UI_Fail", gameObject);
        }
        else if (CurrentShip.GetBehavior<Thruster>() == null)
        {
            ConfirmationDialog.Clear();
            ConfirmationDialog.Title.text = "Can't undock. Missing thruster component!";
            ConfirmationDialog.Show();
            AkSoundEngine.PostEvent("UI_Fail", gameObject);
        }
        else if (CurrentShip.GetBehavior<Reactor>() == null)
        {
            ConfirmationDialog.Clear();
            ConfirmationDialog.Title.text = "Can't undock. Missing reactor component!";
            ConfirmationDialog.Show();
            AkSoundEngine.PostEvent("UI_Fail", gameObject);
        }
        else if (CurrentShip.Parent.TryUndock(CurrentShip))
        {
            BindToEntity();

            EnterOverworld();
            AkSoundEngine.PostEvent("Undock", gameObject);
        }
        else
        {
            ConfirmationDialog.Title.text = "Can't undock. Must empty docking bay!";
            ConfirmationDialog.Show();
            AkSoundEngine.PostEvent("UI_Fail", gameObject);
        }
    }

    private void DisposeEntityBinding()
    {
        foreach (var indicator in _visibleHostileIndicators)
        {
            Destroy(indicator.Value.gameObject);
        }
        
        _visibleHostileIndicators.Clear();
        if(_lockingIndicators!=null) foreach(var (_, indicator, _) in _lockingIndicators)
            indicator.GetComponent<Prototype>().ReturnToPool();
        _input.Player.Disable();
        Cursor.lockState = CursorLockMode.None;
        GameplayUI.SetActive(false);
        
        foreach(var subscription in _shipSubscriptions) subscription.Dispose();
        _shipSubscriptions.Clear();
    }

    private void EnterOverworld()
    {
        Menu.gameObject.SetActive(false);
        DockedEntity = null;
        DockingBay = null;
        DockCamera.enabled = false;
        FollowCamera.enabled = true;
        
        Cursor.lockState = CursorLockMode.Locked;
        GameplayUI.SetActive(true);
        _input.Player.Enable();
        ShipPanel.Display(CurrentShip, true);
        SchematicDisplay.ShowShip(CurrentShip);
    }

    private void BindToEntity()
    {
        SectorRenderer.PerspectiveEntity = CurrentShip;
        
        FollowCamera.LookAt = SectorRenderer.EntityInstances[CurrentShip].LookAtPoint;
        FollowCamera.Follow = SectorRenderer.EntityInstances[CurrentShip].transform;
        _articulationGroups = CurrentShip.Equipment
            .Where(item => item.Behaviors.Any(x => x.Data is WeaponData && !(x.Data is LauncherData)))
            .GroupBy(item => SectorRenderer.EntityInstances[CurrentShip]
                .GetBarrel(CurrentShip.Hardpoints[item.Position.x, item.Position.y])
                .GetComponentInParent<ArticulationPoint>()?.Group ?? -1)
            .Select((group, index) => {
                return (
                    group.Select(item => CurrentShip.Hardpoints[item.Position.x, item.Position.y]).ToArray(),
                    group.Select(item => SectorRenderer.EntityInstances[CurrentShip].GetBarrel(CurrentShip.Hardpoints[item.Position.x, item.Position.y])).ToArray(),
                    Crosshairs[index]
                );
            }).ToArray();
        
        foreach (var crosshair in Crosshairs)
            crosshair.gameObject.SetActive(false);
        foreach (var group in _articulationGroups)
            group.crosshair.gameObject.SetActive(true);
        
        _shipSubscriptions.Add(CurrentShip.Target.Subscribe(target =>
        {
            TargetIndicator.gameObject.SetActive(CurrentShip.Target.Value != null);
            TargetShipPanel.gameObject.SetActive(target != null);
            if (target != null)
            {
                TargetShipPanel.Display(target, true);
                TargetSchematicDisplay.ShowShip(target, CurrentShip);
            }
        }));

        foreach (var entity in CurrentShip.VisibleHostiles)
        {
            var indicator = HostileTargetIndicator.Instantiate<VisibleHostileIndicator>();
            _visibleHostileIndicators.Add(entity, indicator);
        }
        _shipSubscriptions.Add(CurrentShip.VisibleHostiles.ObserveAdd().Subscribe(addEvent =>
        {
            var indicator = HostileTargetIndicator.Instantiate<VisibleHostileIndicator>();
            _visibleHostileIndicators.Add(addEvent.Value, indicator);
        }));
        _shipSubscriptions.Add(CurrentShip.VisibleHostiles.ObserveRemove().Subscribe(removeEvent =>
        {
            _visibleHostileIndicators[removeEvent.Value].GetComponent<Prototype>().ReturnToPool();
            _visibleHostileIndicators.Remove(removeEvent.Value);
        }));
        
        _shipSubscriptions.Add(CurrentShip.Death.Subscribe(_ =>
        {
            var deathTime = Time.time;
            DisposeEntityBinding();
            CurrentShip = null;
            ConfirmationDialog.Clear();
            ConfirmationDialog.Title.text = "You have died!";
            ConfirmationDialog.Show(StartGame, null, "Try again!");
            Observable.EveryUpdate()
                .Select(_ => (Time.time - deathTime) / DeathPPTransitionTime)
                .Where(t => t < 1)
                .Subscribe(t =>
                    {
                        HeatstrokePP.weight = 1 - t;
                        SevereHeatstrokePP.weight = 1 - t;
                        DeathPP.weight = t;
                    }, () =>
                    {
                        HeatstrokePP.weight = 0;
                        SevereHeatstrokePP.weight = 0;
                        DeathPP.weight = 1;
                    });
        }));
        
        _lockingIndicators = CurrentShip.GetBehaviors<LockWeapon>()
            .Select(x =>
            {
                var i = LockIndicator.Instantiate<PlaceUIElementWorldspace>();
                return (x, i, i.GetComponent<Rotate>());
            }).ToArray();
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
        if (CurrentShip.Parent != null)
        {
            foreach (var bay in CurrentShip.Parent.DockingBays)
            {
                if (PlayerEntities.Contains(bay.DockedShip)) yield return bay;
            }
        }
    }

    public IEnumerable<Entity> AvailableEntities()
    {
        if(DockedEntity != null)
            foreach (var entity in PlayerEntities)
            {
                if (DockedEntity.Children.Contains(entity)) yield return entity;
            }
        else if (CurrentShip != null)
            yield return CurrentShip;
    }

    void Update()
    {
        if(!_editMode)
        {
            _time += Time.deltaTime;
            ItemManager.Time = Time.time;
            if(CurrentShip !=null && CurrentShip.Parent==null)
            {
                foreach (var indicator in _visibleHostileIndicators)
                {
                    indicator.Value.gameObject.SetActive(indicator.Key!=CurrentShip.Target.Value);
                    indicator.Value.Place.Target = indicator.Key.Position;
                    indicator.Value.Fill.fillAmount =
                        saturate(indicator.Key.EntityInfoGathered[CurrentShip] / Settings.GameplaySettings.TargetDetectionInfoThreshold);
                    indicator.Value.Fill.enabled =
                        !(indicator.Key.EntityInfoGathered[CurrentShip] > Settings.GameplaySettings.TargetDetectionInfoThreshold) ||
                        sin(TargetSpottedBlinkFrequency * Time.time) + TargetSpottedBlinkOffset > 0;
                }
                var look = _input.Player.Look.ReadValue<Vector2>();
                _shipYawPitch = float2(_shipYawPitch.x + look.x * Sensitivity.x, clamp(_shipYawPitch.y + look.y * Sensitivity.y, -.45f * PI, .45f * PI));
                _viewDirection = mul(float3(0, 0, 1), Unity.Mathematics.float3x3.Euler(float3(_shipYawPitch.yx, 0), RotationOrder.YXZ));
                CurrentShip.LookDirection = _viewDirection;
                CurrentShip.MovementDirection = _input.Player.Move.ReadValue<Vector2>();
                HeatstrokePP.weight = saturate(unlerp(0, Settings.GameplaySettings.SevereHeatstrokeRiskThreshold, CurrentShip.Heatstroke));
                var severeHeatstrokeLerp = saturate(unlerp(Settings.GameplaySettings.SevereHeatstrokeRiskThreshold, 1, CurrentShip.Heatstroke));
                SevereHeatstrokePP.weight =
                    severeHeatstrokeLerp + severeHeatstrokeLerp * (1 - severeHeatstrokeLerp) *
                    max(Settings.HeatstrokePhasingFloor, sin(Time.time * Settings.HeatstrokePhasingFrequency));
            }
        }
        Zone.Update(_time, Time.deltaTime);
        SectorRenderer.GameTime = _time;
    }

    private void LateUpdate()
    {
        UpdateTargetIndicators();
    }

    private void UpdateTargetIndicators()
    {
        if (CurrentShip == null || CurrentShip.Parent != null) return;

        ViewDot.Target = SectorRenderer.EntityInstances[CurrentShip].LookAtPoint.position;
        if (CurrentShip.Target.Value != null)
            TargetIndicator.Target = CurrentShip.Target.Value.Position;
        var distance = length((float3)ViewDot.Target - CurrentShip.Position);
        foreach (var (_, barrels, crosshair) in _articulationGroups)
        {
            var averagePosition = Vector3.zero;
            foreach (var barrel in barrels)
                averagePosition += barrel.position + barrel.forward * distance;
            averagePosition /= barrels.Length;
            crosshair.Target = averagePosition;
        }
        foreach (var (targetLock, indicator, spin) in _lockingIndicators)
        {
            var showLockingIndicator = targetLock.Lock > .01f && CurrentShip.Target.Value != null;
            indicator.gameObject.SetActive(showLockingIndicator);
            if(showLockingIndicator)
            {
                indicator.Target = CurrentShip.Target.Value.Position;
                indicator.NoiseAmplitude = Settings.GameplaySettings.LockIndicatorNoiseAmplitude * (1 - targetLock.Lock);
                indicator.NoiseFrequency = Settings.GameplaySettings.LockIndicatorFrequency.Evaluate(targetLock.Lock);
                spin.Speed = Settings.GameplaySettings.LockSpinSpeed.Evaluate(targetLock.Lock);
            }
        }
    }
}