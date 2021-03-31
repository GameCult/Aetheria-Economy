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
using UnityEngine.InputSystem;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;
using float3 = Unity.Mathematics.float3;
using quaternion = Unity.Mathematics.quaternion;
using Random = UnityEngine.Random;

public class ActionGameManager : MonoBehaviour
{
    private static DirectoryInfo _gameDataDirectory;
    public static DirectoryInfo GameDataDirectory
    {
        get => _gameDataDirectory ??= new DirectoryInfo(Application.dataPath).Parent.CreateSubdirectory("GameData");
    }

    private static CultCache _cultCache;

    public static CultCache CultCache
    {
        get
        {
            if (_cultCache != null) return _cultCache;

            _cultCache = new CultCache(Path.Combine(GameDataDirectory.FullName, "AetherDB.msgpack"));
            _cultCache.Load();
            
            return _cultCache;
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
    //public string StarterShipTemplate = "Longinus";
    public float2 Sensitivity;
    public int Credits = 15000000;
    public float TargetSpottedBlinkFrequency = 20;
    public float TargetSpottedBlinkOffset = -.25f;
    
    [Header("Postprocessing")]
    public float DeathPPTransitionTime;
    public PostProcessVolume DeathPP;
    public PostProcessVolume HeatstrokePP;
    public PostProcessVolume SevereHeatstrokePP;

    [Header("Scene Links")]
    public Transform EffectManagerParent;
    public TradeMenu TradeMenu;
    public Prototype HostileTargetIndicator;
    public PlaceUIElementWorldspace ViewDot;
    public PlaceUIElementWorldspace TargetIndicator;
    public Prototype LockIndicator;
    public PlaceUIElementWorldspace[] Crosshairs;
    public EventLog EventLog;
    [FormerlySerializedAs("SectorRenderer")] public ZoneRenderer ZoneRenderer;
    public CinemachineVirtualCamera DockCamera;
    public CinemachineVirtualCamera FollowCamera;
    public CinemachineVirtualCamera WormholeCamera;
    public CanvasGroup GameplayUI;
    public MenuPanel Menu;
    public MapRenderer MenuMap;
    //public SectorRenderer SectorRenderer;
    public SectorMap SectorMap;
    public SchematicDisplay SchematicDisplay;
    public SchematicDisplay TargetSchematicDisplay;
    public InventoryMenu Inventory;
    public InventoryPanel ShipPanel;
    public InventoryPanel TargetShipPanel;
    public ConfirmationDialog Dialog;

    public float IntroDuration;
    
    //public PlayerInput Input;
    
    // private CinemachineFramingTransposer _transposer;
    // private CinemachineComposer _composer;
    
    private DirectoryInfo _loadoutPath;
    private bool _editMode;
    private float _time;
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
    private bool _uiHidden;
    
    public AetheriaInput Input { get; private set; }
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

    public EntitySettings NewEntitySettings
    {
        get => MessagePackSerializer.Deserialize<EntitySettings>(MessagePackSerializer.Serialize(Settings.GameplaySettings.DefaultEntitySettings));
    }

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
        PlayerSettings.CurrentRun = new SavedGame(CurrentSector, Zone, DockedEntity??CurrentEntity);
        SavePlayerSettings();
    }

    void Start()
    {
        EntityInstance.EffectManagerParent = EffectManagerParent;
        AkSoundEngine.RegisterGameObj(gameObject);
        ConsoleController.MessageReceiver = this;
        
        ItemManager = new ItemManager(CultCache, Settings.GameplaySettings, Debug.Log);
        ZoneRenderer.ItemManager = ItemManager;

        // _loadoutPath = GameDataDirectory.CreateSubdirectory("Loadouts");
        // Loadouts.AddRange(_loadoutPath.EnumerateFiles("*.loadout")
        //     .Select(fi => MessagePackSerializer.Deserialize<EntityPack>(File.ReadAllBytes(fi.FullName))));

        #region Input Handling

        Input = new AetheriaInput();
        Input.Global.Enable();
        Input.UI.Enable();

        _zoomLevelIndex = Settings.DefaultMinimapZoom;
        Input.Player.MinimapZoom.performed += context =>
        {
            _zoomLevelIndex = (_zoomLevelIndex + 1) % Settings.MinimapZoomLevels.Length;
            ZoneRenderer.MinimapDistance = Settings.MinimapZoomLevels[_zoomLevelIndex];
        };

        Input.Global.MapToggle.performed += context =>
        {
            if (Menu.gameObject.activeSelf && Menu.CurrentTab == MenuTab.Map)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Menu.gameObject.SetActive(false);
                if (CurrentEntity != null && CurrentEntity.Parent == null)
                {
                    Input.Player.Enable();
                    GameplayUI.gameObject.SetActive(true);
                    
                    SchematicDisplay.ShowShip(CurrentEntity);
                    ShipPanel.Display(CurrentEntity, true);
                }
                
                return;
            }
            
            Cursor.lockState = CursorLockMode.None;
            Input.Player.Disable();
            GameplayUI.gameObject.SetActive(false);
            Menu.ShowTab(MenuTab.Map);
            MenuMap.Position = CurrentEntity.Position.xz;
        };

        Input.Global.Inventory.performed += context =>
        {
            if (Menu.gameObject.activeSelf && Menu.CurrentTab == MenuTab.Inventory)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Menu.gameObject.SetActive(false);
                if (CurrentEntity != null && CurrentEntity.Parent == null)
                {
                    Input.Player.Enable();
                    GameplayUI.gameObject.SetActive(true);
                    
                    SchematicDisplay.ShowShip(CurrentEntity);
                    ShipPanel.Display(CurrentEntity, true);
                }
                return;
            }

            Cursor.lockState = CursorLockMode.None;
            Input.Player.Disable();
            Menu.ShowTab(MenuTab.Inventory);
            GameplayUI.gameObject.SetActive(false);
        };

        Input.Global.Dock.performed += context =>
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

        Input.Player.EnterWormhole.performed += context =>
        {
            if(CurrentEntity is Ship ship)
            {
                foreach (var wormhole in ZoneRenderer.WormholeInstances.Keys)
                {
                    if (length(wormhole.Position - CurrentEntity.Position.xz) < Settings.GameplaySettings.WormholeExitRadius)
                    {
                        var oldZone = Zone;
                        // var wormholeCameraFollow = new GameObject("Wormhole Camera Follow").transform;
                        // wormholeCameraFollow.position = new Vector3(wormhole.Position.x, -50, wormhole.Position.y);
                        // wormholeCameraFollow.rotation = Quaternion.LookRotation(Vector3.down, ship.LookDirection);
                        // WormholeCamera.enabled = true;
                        // WormholeCamera.Follow = wormholeCameraFollow;
                        // FollowCamera.enabled = false;
                        ship.EnterWormhole(wormhole.Position);
                        ship.OnEnteredWormhole += () =>
                        {
                            PopulateLevel(wormhole.Target);
                            SectorMap.QueueZoneReveal(wormhole.Target.AdjacentZones);
                            ship.ExitWormhole(ZoneRenderer.WormholeInstances.Keys.First(w=>w.Target==oldZone.SectorZone).Position,
                                Settings.GameplaySettings.WormholeExitVelocity * ItemManager.Random.NextFloat2Direction());
                            CurrentEntity.Zone = Zone;
                            SaveState();
                        };
                    }
                }
            }
        };

        Input.Player.HideUI.performed += context =>
        {
            _uiHidden = !_uiHidden;
            GameplayUI.alpha = _uiHidden ? 0 : 1;
        };

        Input.Player.OverrideShutdown.performed += context =>
        {
            CurrentEntity.OverrideShutdown = !CurrentEntity.OverrideShutdown;
        };

        Input.Player.Ping.performed += context =>
        {
            CurrentEntity.Sensor?.Ping();
        };

        Input.Player.ToggleHeatsinks.performed += context =>
        {
            CurrentEntity.HeatsinksEnabled = !CurrentEntity.HeatsinksEnabled;
            AkSoundEngine.PostEvent(CurrentEntity.HeatsinksEnabled ? "UI_Success" : "UI_Fail", gameObject);
        };

        Input.Player.ToggleShield.performed += context =>
        {
            if (CurrentEntity.Shield != null)
            {
                CurrentEntity.Shield.Item.Enabled.Value = !CurrentEntity.Shield.Item.Enabled.Value;
                AkSoundEngine.PostEvent(CurrentEntity.Shield.Item.Enabled.Value ? "UI_Success" : "UI_Fail", gameObject);
            }
        };

        #region Targeting

        Input.Player.TargetReticle.performed += context =>
        {
            var underReticle = Zone.Entities.Where(x => x != CurrentEntity)
                .MaxBy(x => dot(normalize(x.Position - CurrentEntity.Position), CurrentEntity.LookDirection));
            CurrentEntity.Target.Value = CurrentEntity.Target.Value == underReticle ? null : underReticle;
        };

        Input.Player.TargetNearest.performed += context =>
        {
            CurrentEntity.Target.Value = Zone.Entities.Where(x=>x!=CurrentEntity)
                .MaxBy(x => length(x.Position - CurrentEntity.Position));
        };

        Input.Player.TargetNext.performed += context =>
        {
            var targets = Zone.Entities.Where(x => x != CurrentEntity).OrderBy(x => length(x.Position - CurrentEntity.Position)).ToArray();
            var currentTargetIndex = Array.IndexOf(targets, CurrentEntity.Target.Value);
            CurrentEntity.Target.Value = targets[(currentTargetIndex + 1) % targets.Length];
        };

        Input.Player.TargetPrevious.performed += context =>
        {
            var targets = Zone.Entities.Where(x => x != CurrentEntity).OrderBy(x => length(x.Position - CurrentEntity.Position)).ToArray();
            var currentTargetIndex = Array.IndexOf(targets, CurrentEntity.Target.Value);
            CurrentEntity.Target.Value = targets[(currentTargetIndex + targets.Length - 1) % targets.Length];
        };
        
        #endregion


        #region Trigger Groups

        Input.Player.PreviousWeaponGroup.performed += context =>
        {
            if (CurrentEntity.Parent != null) return;
            SchematicDisplay.SelectedGroupIndex--;
        };

        Input.Player.NextWeaponGroup.performed += context =>
        {
            if (CurrentEntity.Parent != null) return;
            SchematicDisplay.SelectedGroupIndex++;
        };

        Input.Player.PreviousWeapon.performed += context =>
        {
            if (CurrentEntity.Parent != null) return;
            SchematicDisplay.SelectedItemIndex--;
        };

        Input.Player.NextWeapon.performed += context =>
        {
            if (CurrentEntity.Parent != null) return;
            SchematicDisplay.SelectedItemIndex++;
        };

        Input.Player.ToggleWeaponGroup.performed += context =>
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
        
        Input.Player.FireGroup1.started += context =>
        {
            if (CurrentEntity.Parent != null) return;
            foreach (var s in CurrentEntity.TriggerGroups[0].weapons) s.Activate();
        };

        Input.Player.FireGroup1.canceled += context =>
        {
            if (CurrentEntity.Parent != null) return;
            foreach (var s in CurrentEntity.TriggerGroups[0].weapons) s.Deactivate();
        };

        Input.Player.FireGroup2.started += context =>
        {
            if (CurrentEntity.Parent != null) return;
            foreach (var s in CurrentEntity.TriggerGroups[1].weapons) s.Activate();
        };

        Input.Player.FireGroup2.canceled += context =>
        {
            if (CurrentEntity.Parent != null) return;
            foreach (var s in CurrentEntity.TriggerGroups[1].weapons) s.Deactivate();
        };

        Input.Player.FireGroup3.started += context =>
        {
            if (CurrentEntity.Parent != null) return;
            foreach (var s in CurrentEntity.TriggerGroups[2].weapons) s.Activate();
        };

        Input.Player.FireGroup3.canceled += context =>
        {
            if (CurrentEntity.Parent != null) return;
            foreach (var s in CurrentEntity.TriggerGroups[2].weapons) s.Deactivate();
        };

        Input.Player.FireGroup4.started += context =>
        {
            if (CurrentEntity.Parent != null) return;
            foreach (var s in CurrentEntity.TriggerGroups[3].weapons) s.Activate();
        };

        Input.Player.FireGroup4.canceled += context =>
        {
            if (CurrentEntity.Parent != null) return;
            foreach (var s in CurrentEntity.TriggerGroups[3].weapons) s.Deactivate();
        };

        Input.Player.FireGroup5.started += context =>
        {
            if (CurrentEntity.Parent != null) return;
            foreach (var s in CurrentEntity.TriggerGroups[4].weapons) s.Activate();
        };

        Input.Player.FireGroup5.canceled += context =>
        {
            if (CurrentEntity.Parent != null) return;
            foreach (var s in CurrentEntity.TriggerGroups[4].weapons) s.Deactivate();
        };

        Input.Player.FireGroup6.started += context =>
        {
            if (CurrentEntity.Parent != null) return;
            foreach (var s in CurrentEntity.TriggerGroups[5].weapons) s.Activate();
        };

        Input.Player.FireGroup6.canceled += context =>
        {
            if (CurrentEntity.Parent != null) return;
            foreach (var s in CurrentEntity.TriggerGroups[5].weapons) s.Deactivate();
        };
        
        #endregion

        #endregion
        
        StartGame();
        
        ConsoleController.AddCommand("give",
            args =>
            {
                var itemName = string.Join(" ", args);
                var item = ItemManager.ItemData.GetAll<EquippableItemData>()
                    .FirstOrDefault(itemData => string.Equals(itemData.Name, itemName, StringComparison.InvariantCultureIgnoreCase));
                if (item != null)
                {
                    _currentEntity.CargoBays.First().TryStore(ItemManager.CreateInstance(item, .95f));
                }
            });
        
        ConsoleController.AddCommand("trackmissile",
            _ =>
            {
                foreach (var missileManager in FindObjectsOfType<GuidedProjectileManager>())
                {
                    missileManager.OnFireGuided.Where(x => x.source == _currentEntity).Take(1).Subscribe(x =>
                    {
                        FollowCamera.Follow = x.missile.transform;
                        FollowCamera.LookAt = x.target;
                        x.missile.OnKill += () =>
                        {
                            FollowCamera.LookAt = ZoneRenderer.EntityInstances[CurrentEntity].LookAtPoint;
                            FollowCamera.Follow = ZoneRenderer.EntityInstances[CurrentEntity].transform;
                        };
                    });
                }
            });
        
        ConsoleController.AddCommand("pingscene",
            _ =>
            {
                var startTime = Time.time;
                Observable.EveryUpdate().TakeWhile(_ => Time.time - startTime < 5).Subscribe(
                    _ => Debug.Log($"{(int) (Time.time - startTime)}"),
                    () =>
                    {
                        var nearestFaction = CurrentSector.Factions.MinBy(f => CurrentSector.HomeZones[f].Distance[Zone.SectorZone]);
                        var nearestFactionHomeZone = CurrentSector.HomeZones[nearestFaction];
                        var factionPresence = nearestFaction.InfluenceDistance - nearestFactionHomeZone.Distance[Zone.SectorZone] + 1;

                        var loadoutGenerator = new LoadoutGenerator(
                            ref ItemManager.Random,
                            ItemManager,
                            CurrentSector,
                            Zone.SectorZone,
                            nearestFaction,
                            .5f);

                        for (int i = 0; i < 8; i++)
                        {
                            var ship = EntitySerializer.Unpack(ItemManager, Zone, loadoutGenerator.GenerateShipLoadout(), true);
                            ship.Position.xz = _currentEntity.Position.xz +
                                               ItemManager.Random.NextFloat2Direction() * ItemManager.Random.NextFloat(50, 500);
                            ship.Zone = Zone;
                            Zone.Entities.Add(ship);
                            ship.Activate();
                        }

                        for (int i = 0; i < 8; i++)
                        {
                            var turret = EntitySerializer.Unpack(ItemManager, Zone, loadoutGenerator.GenerateTurretLoadout(), true);
                            turret.Position.xz = _currentEntity.Position.xz +
                                                 ItemManager.Random.NextFloat2Direction() * ItemManager.Random.NextFloat(50, 500);
                            turret.Zone = Zone;
                            Zone.Entities.Add(turret);
                            turret.Activate();
                        }
                    });
            });
    }

    public void PopulateLevel(SectorZone sectorZone)
    {
        if (sectorZone == null) throw new ArgumentNullException(nameof(sectorZone));
        
        if (sectorZone.Contents == null)
        {
            sectorZone.PackedContents ??= ZoneGenerator.GenerateZone(
                ItemManager,
                Settings.ZoneSettings,
                CurrentSector,
                sectorZone
            );
            sectorZone.Contents = new Zone(ItemManager, Settings.PlanetSettings, sectorZone.PackedContents, sectorZone);
        }
        Zone = sectorZone.Contents;
        
        Zone.Log = s => Debug.Log($"Zone: {s}");

        if (CurrentEntity != null)
        {
            CurrentEntity.Deactivate();
            CurrentEntity.Zone.Entities.Remove(CurrentEntity);
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
    }

    private void StartGame()
    {
        if (CurrentSector != null)
        {
            if (PlayerSettings.CurrentRun == null)
            {
                SectorMap.QueueZoneReveal(CurrentSector.Entrance.AdjacentZones.Prepend(CurrentSector.Entrance));
                PopulateLevel(CurrentSector.Entrance);
                var loadoutGenerator = new LoadoutGenerator(ref ItemManager.Random, ItemManager, CurrentSector, Zone.SectorZone, null, 2);
                var ship = EntitySerializer.Unpack(ItemManager, Zone, loadoutGenerator.GenerateShipLoadout(), true);
                // EntitySerializer.Unpack(ItemManager, Zone, Loadouts.First(x => x.Name == StarterShipTemplate), true);
                ((Ship) ship).IsPlayerShip = true;
                ship.Position = float3.zero;
                ship.Zone = Zone;
                Zone.Entities.Add(ship);
                ship.Activate();
                BindToEntity(ship);
            }
            else
            {
                PopulateLevel(CurrentSector.Zones[PlayerSettings.CurrentRun.CurrentZone]);
                var targetEntity = Zone.Entities[PlayerSettings.CurrentRun.CurrentZoneEntity];
                if (targetEntity is OrbitalEntity orbitalEntity)
                    DoDock(orbitalEntity, orbitalEntity.DockingBays.First());
                else
                {
                    //StartCoroutine(IntroCutscene(targetEntity as Ship));
                    BindToEntity(targetEntity);
                }
            }
        }
    }

    private IEnumerator IntroCutscene(Ship ship)
    {
        ZoneRenderer.PerspectiveEntity = ship;
        var entityPosition = ship.Position.xz;
        var followOrbit = Zone.Orbits.Keys.MinBy(o => lengthsq(Zone.GetOrbitPosition(o) - entityPosition));
        var followPlanet = ZoneRenderer.Planets[Zone.Planets.FirstOrDefault(p => p.Value.Orbit == followOrbit).Key];
        DockCamera.Follow = followPlanet.Body.transform;
        var rootOrbit = followOrbit;
        while (Zone.Orbits[rootOrbit].Data.Parent != Guid.Empty)
            rootOrbit = Zone.Orbits[rootOrbit].Data.Parent;
        var rootPlanet = ZoneRenderer.Planets[Zone.Planets.FirstOrDefault(p => p.Value.Orbit == rootOrbit).Key];
        DockCamera.LookAt = rootPlanet.Body.transform;

        var shipVelocity = ship.GetBehavior<VelocityLimit>().Limit;
        var followOrbitPosition = Zone.GetOrbitPosition(followOrbit);
        var shipDirection = normalize(Zone.GetOrbitPosition(rootOrbit) - followOrbitPosition);
        ship.Position.xz = followOrbitPosition - shipDirection * shipVelocity * IntroDuration;

        var startTime = Time.time;
        while (Time.time - startTime < IntroDuration)
        {
            ship.Direction = shipDirection;
            ship.Velocity = shipDirection * shipVelocity;
            yield return null;
        }
        
        BindToEntity(ship);
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
                        TradeMenu.Inventory = entity.CargoBays.First();
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
        var parentOrbitPlanet = Zone.Planets.FirstOrDefault(p => p.Value.Orbit == parentOrbit).Key;
        if (ZoneRenderer.Planets.ContainsKey(parentOrbitPlanet))
            DockCamera.LookAt = ZoneRenderer.Planets[parentOrbitPlanet].Body.transform;
        else DockCamera.LookAt = ZoneRenderer.ZoneRoot;
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
        Input.Player.Disable();
        Cursor.lockState = CursorLockMode.None;
        GameplayUI.gameObject.SetActive(false);
        
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

        if (length(CurrentEntity.Direction) > .1f)
            _viewDirection = float3(CurrentEntity.Direction.x,0,CurrentEntity.Direction.y);
        
        Cursor.lockState = CursorLockMode.Locked;
        Input.Player.Enable();
        GameplayUI.gameObject.SetActive(true);
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
            // Dialog.Clear();
            // Dialog.Title.text = "You have died!";
            // Dialog.Show(() =>
            // {
            //     PlayerSettings.CurrentRun = null;
            //     StartGame();
            // }, null, "Try again!");
            // Dialog.transform.position = new Vector3(Screen.width / 2, Screen.height / 2);
            //Dialog.MoveToCursor();
            Observable.EveryUpdate()
                .Where(_ => Time.time - deathTime < DeathPPTransitionTime)
                .Subscribe(_ =>
                    {
                        var t = (Time.time - deathTime) / DeathPPTransitionTime;
                        HeatstrokePP.weight = 1 - t;
                        SevereHeatstrokePP.weight = 1 - t;
                        DeathPP.weight = t;
                    },
                    () =>
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
                var look = Input.Player.Look.ReadValue<Vector2>();
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
                    ship.MovementDirection = Input.Player.Move.ReadValue<Vector2>();
                }
            }
        }
        Zone.Update(Time.deltaTime);
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