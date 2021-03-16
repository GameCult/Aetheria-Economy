﻿/* This Source Code Form is subject to the terms of the Mozilla Public
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
using UnityEngine.InputSystem;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;
using quaternion = Unity.Mathematics.quaternion;
using Random = UnityEngine.Random;

public class ActionGameManager : MonoBehaviour
{
    private static DirectoryInfo _gameDataDirectory;
    public static DirectoryInfo GameDataDirectory
    {
        get => _gameDataDirectory ??= new DirectoryInfo(Application.dataPath).Parent.CreateSubdirectory("GameData");
    }

    private static DatabaseCache _database;

    public static DatabaseCache Database
    {
        get
        {
            if (_database != null) return _database;

            _database = new DatabaseCache(Path.Combine(GameDataDirectory.FullName, "AetherDB.msgpack"));
            _database.Load();
            
            return _database;
        }
    }

    private static PlayerSettings _playerSettings;
    public static PlayerSettings PlayerSettings
    {
        get => _playerSettings ??= File.Exists(_playerSettingsFilePath)
            ? MessagePackSerializer.Deserialize<PlayerSettings>(File.ReadAllBytes(_playerSettingsFilePath))
            : new PlayerSettings {Name = Environment.UserName};
    }
    private static string _playerSettingsFilePath => Path.Combine(GameDataDirectory.FullName, "PlayerSettings.msgpack");
    public static void SavePlayerSettings()
    {
        File.WriteAllBytes(_playerSettingsFilePath, MessagePackSerializer.Serialize(_playerSettings));
    }

    public static Sector CurrentSector;
    
    public GameSettings Settings;
    public string StarterShipTemplate = "Longinus";
    public float2 Sensitivity;
    public int Credits = 15000000;
    public float TargetSpottedBlinkFrequency = 20;
    public float TargetSpottedBlinkOffset = -.25f;
    
    [Header("Name Generation")]
    public TextAsset NameFile;
    public int NameGeneratorMinLength = 5;
    public int NameGeneratorMaxLength = 10;
    public int NameGeneratorOrder = 4;
    
    [Header("Postprocessing")]
    public float DeathPPTransitionTime;
    public PostProcessVolume DeathPP;
    public PostProcessVolume HeatstrokePP;
    public PostProcessVolume SevereHeatstrokePP;
    
    [Header("Scene Links")]
    public Prototype HostileTargetIndicator;
    public PlaceUIElementWorldspace ViewDot;
    public PlaceUIElementWorldspace TargetIndicator;
    public Prototype LockIndicator;
    public PlaceUIElementWorldspace[] Crosshairs;
    public EventLog EventLog;
    [FormerlySerializedAs("SectorRenderer")] public ZoneRenderer ZoneRenderer;
    public CinemachineVirtualCamera DockCamera;
    public CinemachineVirtualCamera FollowCamera;
    public GameObject GameplayUI;
    public MenuPanel Menu;
    public MapRenderer MenuMap;
    //public SectorRenderer SectorRenderer;
    public SectorMap SectorMap;
    public SchematicDisplay SchematicDisplay;
    public SchematicDisplay TargetSchematicDisplay;
    public InventoryMenu Inventory;
    public InventoryPanel ShipPanel;
    public InventoryPanel TargetShipPanel;
    [FormerlySerializedAs("ConfirmationDialog")] public ConfirmationDialog Dialog;
    
    //public PlayerInput Input;
    
    // private CinemachineFramingTransposer _transposer;
    // private CinemachineComposer _composer;
    
    private DirectoryInfo _loadoutPath;
    private bool _editMode;
    private float _time;
    private AetheriaInput _input;
    private int _zoomLevelIndex;
    private Entity _currentEntity;

    // private ShipInput _shipInput;
    private float2 _entityYawPitch;
    private float3 _viewDirection;
    private (HardpointData[] hardpoints, Transform[] barrels, PlaceUIElementWorldspace crosshair)[] _articulationGroups;
    private (LockWeapon targetLock, PlaceUIElementWorldspace indicator, Rotate spin)[] _lockingIndicators;
    private Dictionary<Entity, VisibleHostileIndicator> _visibleHostileIndicators = new Dictionary<Entity, VisibleHostileIndicator>();
    private List<IDisposable> _shipSubscriptions = new List<IDisposable>();
    private float _severeHeatstrokePhase;
    
    public EquippedDockingBay DockingBay { get; private set; }
    public Entity DockedEntity { get; private set; }

    public Entity CurrentEntity
    {
        get => _currentEntity;
        set => _currentEntity = value;
    }
    
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
        File.WriteAllBytes(Path.Combine(_loadoutPath.FullName, $"{pack.Name}.loadout"), MessagePackSerializer.Serialize(pack));
    }

    private void OnApplicationQuit()
    {
        if(CurrentSector!=null)
        {
            SaveState();
        }
    }

    public void SaveState()
    {
        PlayerSettings.CurrentRun = new SavedGame(CurrentSector, Zone, CurrentEntity);
        SavePlayerSettings();
    }

    void Start()
    {
        AkSoundEngine.RegisterGameObj(gameObject);
        ConsoleController.MessageReceiver = this;
        
        ItemManager = new ItemManager(Database, Settings.GameplaySettings, Debug.Log);
        ZoneRenderer.ItemManager = ItemManager;

        _loadoutPath = GameDataDirectory.CreateSubdirectory("Loadouts");
        Loadouts.AddRange(_loadoutPath.EnumerateFiles("*.loadout")
            .Select(fi => MessagePackSerializer.Deserialize<EntityPack>(File.ReadAllBytes(fi.FullName))));

        #region Input Handling

        _input = new AetheriaInput();
        _input.Global.Enable();
        _input.UI.Enable();

        _zoomLevelIndex = Settings.DefaultMinimapZoom;
        _input.Player.MinimapZoom.performed += context =>
        {
            _zoomLevelIndex = (_zoomLevelIndex + 1) % Settings.MinimapZoomLevels.Length;
            ZoneRenderer.MinimapDistance = Settings.MinimapZoomLevels[_zoomLevelIndex];
        };

        _input.Global.MapToggle.performed += context =>
        {
            if (Menu.gameObject.activeSelf && Menu.CurrentTab == MenuTab.Map)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Menu.gameObject.SetActive(false);
                if (CurrentEntity != null && CurrentEntity.Parent == null)
                {
                    _input.Player.Enable();
                    GameplayUI.SetActive(true);
                    
                    SchematicDisplay.ShowShip(CurrentEntity);
                    ShipPanel.Display(CurrentEntity, true);
                }
                
                return;
            }
            
            Cursor.lockState = CursorLockMode.None;
            _input.Player.Disable();
            GameplayUI.SetActive(false);
            Menu.ShowTab(MenuTab.Map);
            MenuMap.Position = CurrentEntity.Position.xz;
        };

        _input.Global.Inventory.performed += context =>
        {
            if (Menu.gameObject.activeSelf && Menu.CurrentTab == MenuTab.Inventory)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Menu.gameObject.SetActive(false);
                if (CurrentEntity != null && CurrentEntity.Parent == null)
                {
                    _input.Player.Enable();
                    GameplayUI.SetActive(true);
                    
                    SchematicDisplay.ShowShip(CurrentEntity);
                    ShipPanel.Display(CurrentEntity, true);
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
            if (CurrentEntity == null)
            {
                AkSoundEngine.PostEvent("UI_Fail", gameObject);
                Dialog.Clear();
                Dialog.Title.text = "Can't undock. You dont have a ship!";
                Dialog.Show();
                Dialog.MoveToCursor();
            }
            else if (CurrentEntity.Parent == null) Dock();
            else Undock();
        };

        _input.Player.EnterWormhole.performed += context =>
        {
            if(CurrentEntity is Ship ship)
            {
                foreach (var wormhole in ZoneRenderer.WormholeInstances.Keys)
                {
                    if (length(wormhole.Position - CurrentEntity.Position.xz) < Settings.GameplaySettings.WormholeExitRadius)
                    {
                        ship.EnterWormhole(wormhole.Position);
                        ship.OnEnteredWormhole += () =>
                        {
                            GenerateLevel(wormhole.Target);
                            ship.ExitWormhole(ZoneRenderer.WormholeInstances.Keys.First().Position,
                                Settings.GameplaySettings.WormholeExitVelocity * ItemManager.Random.NextFloat2Direction());
                            CurrentEntity.Zone = Zone;
                        };
                    }
                }
            }
        };

        _input.Player.OverrideShutdown.performed += context =>
        {
            CurrentEntity.OverrideShutdown = !CurrentEntity.OverrideShutdown;
        };

        _input.Player.ToggleHeatsinks.performed += context =>
        {
            CurrentEntity.HeatsinksEnabled = !CurrentEntity.HeatsinksEnabled;
            AkSoundEngine.PostEvent(CurrentEntity.HeatsinksEnabled ? "UI_Success" : "UI_Fail", gameObject);
        };

        _input.Player.ToggleShield.performed += context =>
        {
            if (CurrentEntity.Shield != null)
            {
                CurrentEntity.Shield.Item.Enabled.Value = !CurrentEntity.Shield.Item.Enabled.Value;
                AkSoundEngine.PostEvent(CurrentEntity.Shield.Item.Enabled.Value ? "UI_Success" : "UI_Fail", gameObject);
            }
        };

        #region Targeting

        _input.Player.TargetReticle.performed += context =>
        {
            var underReticle = Zone.Entities.Where(x => x != CurrentEntity)
                .MaxBy(x => dot(normalize(x.Position - CurrentEntity.Position), CurrentEntity.LookDirection));
            CurrentEntity.Target.Value = CurrentEntity.Target.Value == underReticle ? null : underReticle;
        };

        _input.Player.TargetNearest.performed += context =>
        {
            CurrentEntity.Target.Value = Zone.Entities.Where(x=>x!=CurrentEntity)
                .MaxBy(x => length(x.Position - CurrentEntity.Position));
        };

        _input.Player.TargetNext.performed += context =>
        {
            var targets = Zone.Entities.Where(x => x != CurrentEntity).OrderBy(x => length(x.Position - CurrentEntity.Position)).ToArray();
            var currentTargetIndex = Array.IndexOf(targets, CurrentEntity.Target.Value);
            CurrentEntity.Target.Value = targets[(currentTargetIndex + 1) % targets.Length];
        };

        _input.Player.TargetPrevious.performed += context =>
        {
            var targets = Zone.Entities.Where(x => x != CurrentEntity).OrderBy(x => length(x.Position - CurrentEntity.Position)).ToArray();
            var currentTargetIndex = Array.IndexOf(targets, CurrentEntity.Target.Value);
            CurrentEntity.Target.Value = targets[(currentTargetIndex + targets.Length - 1) % targets.Length];
        };
        
        #endregion


        #region Trigger Groups

        _input.Player.PreviousWeaponGroup.performed += context =>
        {
            if (CurrentEntity.Parent != null) return;
            SchematicDisplay.SelectedGroupIndex--;
        };

        _input.Player.NextWeaponGroup.performed += context =>
        {
            if (CurrentEntity.Parent != null) return;
            SchematicDisplay.SelectedGroupIndex++;
        };

        _input.Player.PreviousWeapon.performed += context =>
        {
            if (CurrentEntity.Parent != null) return;
            SchematicDisplay.SelectedItemIndex--;
        };

        _input.Player.NextWeapon.performed += context =>
        {
            if (CurrentEntity.Parent != null) return;
            SchematicDisplay.SelectedItemIndex++;
        };

        _input.Player.ToggleWeaponGroup.performed += context =>
        {
            if (CurrentEntity.Parent != null) return;
            var item = SchematicDisplay.SchematicItems[SchematicDisplay.SelectedItemIndex];
            if (CurrentEntity.TriggerGroups[SchematicDisplay.SelectedGroupIndex].items.Contains(item.Item))
                CurrentEntity.TriggerGroups[SchematicDisplay.SelectedGroupIndex].items.Remove(item.Item);
            else CurrentEntity.TriggerGroups[SchematicDisplay.SelectedGroupIndex].items.Add(item.Item);
            foreach (var group in item.Item.BehaviorGroups.Values)
            {
                var weapon = group.GetBehavior<Weapon>();
                if(weapon != null)
                {
                    if (CurrentEntity.TriggerGroups[SchematicDisplay.SelectedGroupIndex].weapons.Contains(weapon))
                        CurrentEntity.TriggerGroups[SchematicDisplay.SelectedGroupIndex].weapons.Remove(weapon);
                    else CurrentEntity.TriggerGroups[SchematicDisplay.SelectedGroupIndex].weapons.Add(weapon);
                }
            }
            SchematicDisplay.UpdateTriggerGroups();
        };
        
        _input.Player.FireGroup1.started += context =>
        {
            if (CurrentEntity.Parent != null) return;
            foreach (var s in CurrentEntity.TriggerGroups[0].weapons) s.Activate();
        };

        _input.Player.FireGroup1.canceled += context =>
        {
            if (CurrentEntity.Parent != null) return;
            foreach (var s in CurrentEntity.TriggerGroups[0].weapons) s.Deactivate();
        };

        _input.Player.FireGroup2.started += context =>
        {
            if (CurrentEntity.Parent != null) return;
            foreach (var s in CurrentEntity.TriggerGroups[1].weapons) s.Activate();
        };

        _input.Player.FireGroup2.canceled += context =>
        {
            if (CurrentEntity.Parent != null) return;
            foreach (var s in CurrentEntity.TriggerGroups[1].weapons) s.Deactivate();
        };

        _input.Player.FireGroup3.started += context =>
        {
            if (CurrentEntity.Parent != null) return;
            foreach (var s in CurrentEntity.TriggerGroups[2].weapons) s.Activate();
        };

        _input.Player.FireGroup3.canceled += context =>
        {
            if (CurrentEntity.Parent != null) return;
            foreach (var s in CurrentEntity.TriggerGroups[2].weapons) s.Deactivate();
        };

        _input.Player.FireGroup4.started += context =>
        {
            if (CurrentEntity.Parent != null) return;
            foreach (var s in CurrentEntity.TriggerGroups[3].weapons) s.Activate();
        };

        _input.Player.FireGroup4.canceled += context =>
        {
            if (CurrentEntity.Parent != null) return;
            foreach (var s in CurrentEntity.TriggerGroups[3].weapons) s.Deactivate();
        };

        _input.Player.FireGroup5.started += context =>
        {
            if (CurrentEntity.Parent != null) return;
            foreach (var s in CurrentEntity.TriggerGroups[4].weapons) s.Activate();
        };

        _input.Player.FireGroup5.canceled += context =>
        {
            if (CurrentEntity.Parent != null) return;
            foreach (var s in CurrentEntity.TriggerGroups[4].weapons) s.Deactivate();
        };

        _input.Player.FireGroup6.started += context =>
        {
            if (CurrentEntity.Parent != null) return;
            foreach (var s in CurrentEntity.TriggerGroups[5].weapons) s.Activate();
        };

        _input.Player.FireGroup6.canceled += context =>
        {
            if (CurrentEntity.Parent != null) return;
            foreach (var s in CurrentEntity.TriggerGroups[5].weapons) s.Deactivate();
        };
        
        #endregion

        #endregion
        
        StartGame();
    }

    public void GenerateLevel(SectorZone sectorZone = null)
    {
        if (sectorZone != null)
        {
            if (sectorZone.Contents == null)
            {
                sectorZone.PackedContents ??= ZoneGenerator.GenerateZone(
                    settings: Settings.ZoneSettings,
                    mass: Settings.DefaultZoneMass,
                    radius: Settings.DefaultZoneRadius
                );
                sectorZone.Contents = new Zone(ItemManager, Settings.PlanetSettings, sectorZone.PackedContents, sectorZone);
            }
            Zone = sectorZone.Contents;
        }
        else
        {
            var zonePack = ZoneGenerator.GenerateZone(
                settings: Settings.ZoneSettings,
                mass: Settings.DefaultZoneMass,
                radius: Settings.DefaultZoneRadius
            );
            Zone = new Zone(ItemManager, Settings.PlanetSettings, zonePack, sectorZone);
        }
        
        Zone.Log = s => Debug.Log($"Zone: {s}");

        if (CurrentEntity != null)
        {
            CurrentEntity.Deactivate();
            if (CurrentEntity.Zone != null)
            {
                CurrentEntity.Zone.Entities.Remove(CurrentEntity);
            }
            CurrentEntity.Zone = Zone;
            Zone.Entities.Add(CurrentEntity);
            CurrentEntity.Activate();
        }
        
        ZoneRenderer.LoadZone(Zone);
        
        if (CurrentEntity != null)
        {
            UnbindEntity();
            BindToEntity(CurrentEntity);
        }

        GenerateZoneEntities(Zone);
    }

    private void GenerateZoneEntities(Zone zone)
    {
        //
        // var turretLoadout = Loadouts.First(x => x.Name == "Turret");
        //
        // for(int i=0; i<3; i++)
        // {
        //     var planet = zone.PlanetInstances.Values.OrderByDescending(p => p.BodyData.Mass.Value).ElementAt(4+i);
        //     var planetOrbit = planet.Orbit.Data.ID;
        //     var planetPos = zone.GetOrbitPosition(planetOrbit);
        //     for (int j = 0; j < 2; j++)
        //     {
        //         var pos = planetPos + ItemManager.Random.NextFloat2Direction() * planet.GravityWellRadius.Value * (.1f + .1f * j);
        //         var orbit = zone.CreateOrbit(planetOrbit, pos);
        //         var turret = EntityPack.Unpack(ItemManager, Zone, turretLoadout, orbit.ID, true);
        //         turret.Activate();
        //         turret.Name = $"Turret {i}-{(char) ('A' + j)}";
        //         turret.Faction = testCorp;
        //         zone.Entities.Add(turret);
        //     }
        // }
        //
        // var enemyShipLoadout = Loadouts.First(x => ((HullData) ItemManager.GetData(x.Hull)).HullType == HullType.Ship);
        // var enemyShip = EntityPack.Unpack(ItemManager, Zone, enemyShipLoadout, true);
        // enemyShip.Faction = testCorp;
        // enemyShip.Activate();
        // zone.Entities.Add(enemyShip);
        // zone.Agents.Add(CreateAgent(enemyShip));
    }

    private Agent CreateAgent(Ship ship)
    {
        var agent = new Minion(ship);
        var task = new PatrolOrbitsTask();
        task.Circuit = Zone.Orbits.OrderBy(_ => Random.value).Take(4).Select(x => x.Key).ToArray();
        agent.Task = task;
        return agent;
    }

    private void StartGame()
    {
        if (CurrentSector != null)
        {
            if (PlayerSettings.CurrentRun == null)
            {
                GenerateLevel(CurrentSector.Entrance);
                var ship = EntitySerializer.Unpack(ItemManager, Zone, Loadouts.First(x => x.Name == StarterShipTemplate), true);
                ship.Zone = Zone;
                Zone.Entities.Add(ship);
                ship.Activate();
                BindToEntity(ship);
            }
            else
            {
                GenerateLevel(CurrentSector.Zones[PlayerSettings.CurrentRun.CurrentZone]);
                var targetEntity = Zone.Entities[PlayerSettings.CurrentRun.CurrentZoneEntity];
                if (targetEntity is OrbitalEntity orbitalEntity)
                    DoDock(orbitalEntity, orbitalEntity.DockingBays.First());
                else
                    BindToEntity(targetEntity);
            }
        }
        else GenerateLevel();
        
        // var stationType = Database.GetAll<HullData>().First(x=>x.HullType==HullType.Station);
        // var stationHull = ItemManager.CreateInstance(stationType) as EquippableItem;
        // var stationParent = Zone.PlanetInstances.Values.OrderByDescending(p => p.BodyData.Mass.Value).ElementAt(3);
        // var stationParentOrbit = stationParent.Orbit.Data.ID;
        // var stationParentPos = Zone.GetOrbitPosition(stationParentOrbit);
        // var stationPos = stationParentPos + ItemManager.Random.NextFloat2Direction() * stationParent.GravityWellRadius.Value * .1f;
        // var stationOrbit = Zone.CreateOrbit(stationParentOrbit, stationPos);
        // var station = new OrbitalEntity(ItemManager, Zone, stationHull, stationOrbit.ID, Settings.DefaultEntitySettings);
        // Zone.Entities.Add(station);
        // var dockingBayData = Database.GetAll<DockingBayData>().First();
        // var dockingBay = ItemManager.CreateInstance(dockingBayData) as EquippableItem;
        // station.TryEquip(dockingBay);
        // station.Activate();
        //
        // DoDock(station, station.DockingBays.First());
        
        // ship.ExitWormhole(
        //     SectorRenderer.WormholeInstances.Keys.First().Position,
        //     ItemManager.Random.NextFloat2Direction() * Settings.GameplaySettings.WormholeExitVelocity);
    }

    public void Dock()
    {
        if (CurrentEntity.Parent != null) return;
        if (CurrentEntity is Ship ship)
        {
            foreach (var entity in Zone.Entities.ToArray())
            {
                if (lengthsq(entity.Position.xz - CurrentEntity.Position.xz) <
                    Settings.GameplaySettings.DockingDistance * Settings.GameplaySettings.DockingDistance)
                {
                    var bay = entity.TryDock(ship);
                    if (bay != null)
                    {
                        UnbindEntity();
                        DoDock(entity, bay);
                        AkSoundEngine.PostEvent("Dock", gameObject);
                        return;
                    }
                }
            }
        }
        AkSoundEngine.PostEvent("Dock_Fail", gameObject);
    }

    private void DoDock(Entity entity, EquippedDockingBay dockingBay)
    {
        DockedEntity = entity;
        ZoneRenderer.PerspectiveEntity = DockedEntity;
        DockingBay = dockingBay;
        DockCamera.enabled = true;
        FollowCamera.enabled = false;
        var orbital = (OrbitalEntity) entity;
        DockCamera.Follow = ZoneRenderer.EntityInstances[orbital].transform;
        var parentOrbit = Zone.Orbits[orbital.OrbitData].Data.Parent;
        var parentPlanet = ZoneRenderer.Planets[Zone.Planets.FirstOrDefault(p => p.Value.Orbit == parentOrbit).Key];
        DockCamera.LookAt = parentPlanet.Body.transform;
        Menu.ShowTab(MenuTab.Inventory);
    }

    public void Undock()
    {
        if (CurrentEntity.Parent == null) return;
        if (CurrentEntity is Ship ship)
        {
            if (CurrentEntity.GetBehavior<Cockpit>() == null)
            {
                Dialog.Clear();
                Dialog.Title.text = "Can't undock. Missing cockpit component!";
                Dialog.Show();
                Dialog.MoveToCursor();
                AkSoundEngine.PostEvent("UI_Fail", gameObject);
            }
            else if (CurrentEntity.GetBehavior<Thruster>() == null)
            {
                Dialog.Clear();
                Dialog.Title.text = "Can't undock. Missing thruster component!";
                Dialog.Show();
                Dialog.MoveToCursor();
                AkSoundEngine.PostEvent("UI_Fail", gameObject);
            }
            else if (CurrentEntity.GetBehavior<Reactor>() == null)
            {
                Dialog.Clear();
                Dialog.Title.text = "Can't undock. Missing reactor component!";
                Dialog.Show();
                Dialog.MoveToCursor();
                AkSoundEngine.PostEvent("UI_Fail", gameObject);
            }
            else if (CurrentEntity.Parent.TryUndock(ship))
            {
                BindToEntity(ship);
                AkSoundEngine.PostEvent("Undock", gameObject);
            }
            else
            {
                Dialog.Title.text = "Can't undock. Must empty docking bay!";
                Dialog.Show();
                Dialog.MoveToCursor();
                AkSoundEngine.PostEvent("UI_Fail", gameObject);
            }
        }
    }

    private void UnbindEntity()
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

    private void BindToEntity(Entity entity)
    {
        if (!ZoneRenderer.EntityInstances.ContainsKey(entity))
        {
            Debug.LogError($"Attempted to bind to entity {entity.Name}, but SectorRenderer has no such instance!");
            return;
        }
        
        CurrentEntity = entity;
        DeathPP.weight = 0;
        ZoneRenderer.PerspectiveEntity = CurrentEntity;
        
        Menu.gameObject.SetActive(false);
        DockedEntity = null;
        DockingBay = null;
        DockCamera.enabled = false;
        FollowCamera.enabled = true;
        
        Cursor.lockState = CursorLockMode.Locked;
        GameplayUI.SetActive(true);
        _input.Player.Enable();
        ShipPanel.Display(CurrentEntity, true);
        SchematicDisplay.ShowShip(CurrentEntity);
        
        FollowCamera.LookAt = ZoneRenderer.EntityInstances[CurrentEntity].LookAtPoint;
        FollowCamera.Follow = ZoneRenderer.EntityInstances[CurrentEntity].transform;
        _articulationGroups = CurrentEntity.Equipment
            .Where(item => item.Behaviors.Any(x => x.Data is WeaponData && !(x.Data is LauncherData)))
            .GroupBy(item => ZoneRenderer.EntityInstances[CurrentEntity]
                .GetBarrel(CurrentEntity.Hardpoints[item.Position.x, item.Position.y])
                .GetComponentInParent<ArticulationPoint>()?.Group ?? -1)
            .Select((group, index) => {
                return (
                    group.Select(item => CurrentEntity.Hardpoints[item.Position.x, item.Position.y]).ToArray(),
                    group.Select(item => ZoneRenderer.EntityInstances[CurrentEntity].GetBarrel(CurrentEntity.Hardpoints[item.Position.x, item.Position.y])).ToArray(),
                    Crosshairs[index]
                );
            }).ToArray();
        
        foreach (var crosshair in Crosshairs)
            crosshair.gameObject.SetActive(false);
        foreach (var group in _articulationGroups)
            group.crosshair.gameObject.SetActive(true);
        
        _shipSubscriptions.Add(CurrentEntity.Target.Subscribe(target =>
        {
            TargetIndicator.gameObject.SetActive(CurrentEntity.Target.Value != null);
            TargetShipPanel.gameObject.SetActive(target != null);
            if (target != null)
            {
                TargetShipPanel.Display(target, true);
                TargetSchematicDisplay.ShowShip(target, CurrentEntity);
            }
        }));

        foreach (var hostile in CurrentEntity.VisibleHostiles)
        {
            var indicator = HostileTargetIndicator.Instantiate<VisibleHostileIndicator>();
            _visibleHostileIndicators.Add(hostile, indicator);
        }
        _shipSubscriptions.Add(CurrentEntity.VisibleHostiles.ObserveAdd().Subscribe(addEvent =>
        {
            var indicator = HostileTargetIndicator.Instantiate<VisibleHostileIndicator>();
            _visibleHostileIndicators.Add(addEvent.Value, indicator);
        }));
        _shipSubscriptions.Add(CurrentEntity.VisibleHostiles.ObserveRemove().Subscribe(removeEvent =>
        {
            _visibleHostileIndicators[removeEvent.Value].GetComponent<Prototype>().ReturnToPool();
            _visibleHostileIndicators.Remove(removeEvent.Value);
        }));
        
        _shipSubscriptions.Add(CurrentEntity.Death.Subscribe(_ =>
        {
            var deathTime = Time.time;
            UnbindEntity();
            CurrentEntity = null;
            Dialog.Clear();
            Dialog.Title.text = "You have died!";
            Dialog.Show(StartGame, null, "Try again!");
            Dialog.MoveToCursor();
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
        
        _lockingIndicators = CurrentEntity.GetBehaviors<LockWeapon>()
            .Select(x =>
            {
                var i = LockIndicator.Instantiate<PlaceUIElementWorldspace>();
                return (x, i, i.GetComponent<Rotate>());
            }).ToArray();
    }

    public void SaveZone(string name) => File.WriteAllBytes(
        Path.Combine(_gameDataDirectory.FullName, $"{name}.zone"), MessagePackSerializer.Serialize(Zone.PackZone()));

    // public void ToggleEditMode()
    // {
    //     _editMode = !_editMode;
    //     FollowCamera.gameObject.SetActive(!_editMode);
    //     TopDownCamera.gameObject.SetActive(_editMode);
    // }
    
    public IEnumerable<EquippedCargoBay> AvailableCargoBays()
    {
        if (CurrentEntity.Parent != null)
        {
            foreach (var bay in CurrentEntity.Parent.DockingBays)
            {
                if (bay.DockedShip.IsPlayerShip) yield return bay;
            }
        }
    }

    public IEnumerable<Entity> AvailableEntities()
    {
        if(DockedEntity != null)
            foreach (var entity in DockedEntity.Children)
            {
                if (entity is Ship { IsPlayerShip: true }) yield return entity;
            }
        else if (CurrentEntity != null)
            yield return CurrentEntity;
    }

    void Update()
    {
        if(!_editMode)
        {
            _time += Time.deltaTime;
            ItemManager.Time = Time.time;
            if(CurrentEntity !=null && CurrentEntity.Parent==null)
            {
                foreach (var indicator in _visibleHostileIndicators)
                {
                    indicator.Value.gameObject.SetActive(indicator.Key!=CurrentEntity.Target.Value);
                    indicator.Value.Place.Target = indicator.Key.Position;
                    indicator.Value.Fill.fillAmount =
                        saturate(indicator.Key.EntityInfoGathered[CurrentEntity] / Settings.GameplaySettings.TargetDetectionInfoThreshold);
                    indicator.Value.Fill.enabled =
                        !(indicator.Key.EntityInfoGathered[CurrentEntity] > Settings.GameplaySettings.TargetDetectionInfoThreshold) ||
                        sin(TargetSpottedBlinkFrequency * Time.time) + TargetSpottedBlinkOffset > 0;
                }
                var look = _input.Player.Look.ReadValue<Vector2>();
                _entityYawPitch = float2(_entityYawPitch.x + look.x * Sensitivity.x, clamp(_entityYawPitch.y + look.y * Sensitivity.y, -.45f * PI, .45f * PI));
                _viewDirection = mul(float3(0, 0, 1), Unity.Mathematics.float3x3.Euler(float3(_entityYawPitch.yx, 0), RotationOrder.YXZ));
                CurrentEntity.LookDirection = _viewDirection;
                HeatstrokePP.weight = saturate(unlerp(0, Settings.GameplaySettings.SevereHeatstrokeRiskThreshold, CurrentEntity.Heatstroke));
                var severeHeatstrokeLerp = saturate(unlerp(Settings.GameplaySettings.SevereHeatstrokeRiskThreshold, 1, CurrentEntity.Heatstroke));
                SevereHeatstrokePP.weight =
                    severeHeatstrokeLerp + severeHeatstrokeLerp * (1 - severeHeatstrokeLerp) *
                    max(Settings.HeatstrokePhasingFloor, sin(Time.time * Settings.HeatstrokePhasingFrequency));
                
                if(CurrentEntity is Ship ship)
                {
                    ship.MovementDirection = _input.Player.Move.ReadValue<Vector2>();
                }
            }
        }
        Zone.Update(_time, Time.deltaTime);
        ZoneRenderer.GameTime = _time;
    }

    private void LateUpdate()
    {
        UpdateTargetIndicators();
    }

    private void UpdateTargetIndicators()
    {
        if (CurrentEntity == null || CurrentEntity.Parent != null) return;

        ViewDot.Target = ZoneRenderer.EntityInstances[CurrentEntity].LookAtPoint.position;
        if (CurrentEntity.Target.Value != null)
            TargetIndicator.Target = CurrentEntity.Target.Value.Position;
        var distance = length((float3)ViewDot.Target - CurrentEntity.Position);
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
            var showLockingIndicator = targetLock.Lock > .01f && CurrentEntity.Target.Value != null && CurrentEntity.Target.Value.IsHostileTo(CurrentEntity);
            indicator.gameObject.SetActive(showLockingIndicator);
            if(showLockingIndicator)
            {
                indicator.Target = CurrentEntity.Target.Value.Position;
                indicator.NoiseAmplitude = Settings.GameplaySettings.LockIndicatorNoiseAmplitude * (1 - targetLock.Lock);
                indicator.NoiseFrequency = Settings.GameplaySettings.LockIndicatorFrequency.Evaluate(targetLock.Lock);
                spin.Speed = Settings.GameplaySettings.LockSpinSpeed.Evaluate(targetLock.Lock);
            }
        }
    }
}