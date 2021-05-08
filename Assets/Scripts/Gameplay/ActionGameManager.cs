/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cinemachine;
using Ink;
using Ink.Runtime;
using MessagePack;
using Newtonsoft.Json;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Unity.Mathematics;
using UnityEngine.EventSystems;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;
using float3 = Unity.Mathematics.float3;
using Path = System.IO.Path;
using quaternion = Unity.Mathematics.quaternion;
using Random = UnityEngine.Random;

public class ActionGameManager : MonoBehaviour
{
    public static ActionGameManager Instance { get; private set; }
    private static DirectoryInfo _gameDataDirectory;
    public static DirectoryInfo GameDataDirectory
    {
        get => _gameDataDirectory ??= new DirectoryInfo(Application.dataPath).Parent.CreateSubdirectory("GameData");
    }

    private static WwiseMetaSoundBanksInfo _soundBanksInfo;
    public static WwiseMetaSoundBanksInfo SoundBanksInfo => _soundBanksInfo ??=
        JsonConvert.DeserializeObject<WwiseMetadataFile>(File.ReadAllText(Path.Combine(GameDataDirectory.FullName, "SoundbanksInfo.json")))
            .SoundBanksInfo;

    private static HashSet<uint> _loadedSoundBanks = new HashSet<uint>();
    public static void LoadSoundbank(uint soundBank)
    {
        if (!_loadedSoundBanks.Contains(soundBank))
        {
            AkSoundEngine.LoadBank(SoundBanksInfo.GetSoundBank(soundBank).ShortName, out _);
            _loadedSoundBanks.Add(soundBank);
        }
    }

    private static GameObject CurrentMusicGameObject;
    private static uint CurrentMusicSoundbank;
    public static void PlayMusic(uint soundBank, GameObject obj)
    {
        if (soundBank == CurrentMusicSoundbank) return;
        if (CurrentMusicSoundbank != 0)
            AkSoundEngine.PostEvent(SoundBanksInfo.GetSoundBank(CurrentMusicSoundbank)
                    .GetEvent(LoopingAudioEvent.Stop).Id, CurrentMusicGameObject);
        LoadSoundbank(soundBank);
        CurrentMusicGameObject = obj;
        CurrentMusicSoundbank = soundBank;
        AkSoundEngine.PostEvent(SoundBanksInfo.GetSoundBank(CurrentMusicSoundbank)
            .GetEvent(LoopingAudioEvent.Play).Id, CurrentMusicGameObject);
    }

    public static void StopMusic()
    {
        if (CurrentMusicSoundbank != 0)
            AkSoundEngine.PostEvent(SoundBanksInfo.GetSoundBank(CurrentMusicSoundbank)
                .GetEvent(LoopingAudioEvent.Stop).Id, CurrentMusicGameObject);
        CurrentMusicGameObject = null;
        CurrentMusicSoundbank = 0;
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
        get
        {
            RegisterResolver.Register();
            return _playerSettings ??= File.Exists(_playerSettingsFilePath)
                ? MessagePackSerializer.Deserialize<PlayerSettings>(File.ReadAllBytes(_playerSettingsFilePath))
                : GetDefaultPlayerSettings();
        }
    }

    private static string _playerSettingsFilePath => Path.Combine(GameDataDirectory.FullName, "PlayerSettings.msgpack");
    public static void SavePlayerSettings()
    {
        File.WriteAllBytes(_playerSettingsFilePath, MessagePackSerializer.Serialize(_playerSettings));
    }

    private static PlayerSettings GetDefaultPlayerSettings()
    {
        var settings = new PlayerSettings();
        settings.Name = Environment.UserName;
        settings.InputSettings.ActionBarInputs.Add("<Keyboard>/leftShift");
        settings.InputSettings.ActionBarInputs.Add("<Mouse>/leftButton");
        settings.InputSettings.ActionBarInputs.Add("<Mouse>/rightButton");
        settings.InputSettings.ActionBarInputs.Add("<Mouse>/middleButton");
        for (int i = 1; i < 6; i++) settings.InputSettings.ActionBarInputs.Add($"<Keyboard>/{i}");
        return settings;
    }

    public static Sector CurrentSector;
    public static bool IsTutorial;

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
    public PostProcessVolume HypothermiaPP;
    public PostProcessVolume SevereHeatstrokePP;
    public PostProcessVolume SevereHypothermiaPP;

    [Header("Scene Links")]
    public GameObject UiRoot;
    public GameObject HelpScreen;
    public InputDisplayLayout InputDisplayLayout;
    public Transform ActionBar;
    public ActionBarSlot ActionBarSlot;
    public Transform EffectManagerParent;
    public ZoneRenderer ZoneRenderer;
    public CinemachineVirtualCamera DockCamera;
    public CinemachineVirtualCamera FollowCamera;
    public CinemachineVirtualCamera WormholeCamera;
    //public SectorRenderer SectorRenderer;
    public SectorMap SectorMap;
    public VolumeSampling VolumeRenderer;
    
    [Header("Menu UI")]
    public MainMenu MainMenu;
    public MenuPanel Menu;
    public MapRenderer MenuMap;
    public TradeMenu TradeMenu;
    public InventoryMenu Inventory;
    public InventoryPanel ShipPanel;
    public InventoryPanel TargetShipPanel;
    public ConfirmationDialog Dialog;
    public ContextMenu Context;
    public DropdownMenu Dropdown;
    
    [Header("Gameplay UI")]
    public CanvasGroup GameplayUI;
    public EventLog EventLog;
    public Prototype HostileTargetIndicator;
    public PlaceUIElementWorldspace ViewDot;
    public Prototype LockIndicator;
    public PlaceUIElementWorldspace[] Crosshairs;
    public GameObject HitMarker;
    public float HitMarkerDuration;
    public SchematicDisplay SchematicDisplay;
    public SchematicDisplay TargetSchematicDisplay;
    
    [Header("Target Indicator")]
    public PlaceUIElementWorldspace TargetIndicator;
    public Image TargetHitpointsFill;
    public Image TargetVisibilityFill;
    public Image VisibilityToTargetFill;
    public Image TargetShieldsBackground;
    public Image TargetShieldsFill;
    public Image TargetShieldsIcon;
    public Color ShieldColor;
    public Color NoShieldColor;
    public Sprite ShieldIcon;
    public Sprite NoShieldIcon;

    public float IntroDuration;
    
    //public PlayerInput Input;
    
    // private CinemachineFramingTransposer _transposer;
    // private CinemachineComposer _composer;
    
    private DirectoryInfo _loadoutPath;
    private bool _paused;
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
    private List<IDisposable> _targetSubscriptions = new List<IDisposable>();
    private float _severeHeatstrokePhase;
    private bool _uiHidden;
    private bool _menuShown;
    private List<ActionBarSlot> _actionBarSlots = new List<ActionBarSlot>();
    private List<InputAction> _actionBarActions = new List<InputAction>();
    private float _hitMarkerTime;
    
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
    public IEnumerable<Story> GetStories => _stories;

    private readonly (float2 direction, string name)[] _directions = {
        (float2(0, 1), "Front"),
        (float2(1, 0), "Right"),
        (float2(-1, 0), "Left"),
        (float2(0, -1), "Rear")
    };

    public DragObject DragObject { get; private set; }
    private Func<DragObject, bool> _endDragCallback;

    private List<Story> _stories = new List<Story>();

    public EntitySettings NewEntitySettings
    {
        get => MessagePackSerializer.Deserialize<EntitySettings>(MessagePackSerializer.Serialize(Settings.GameplaySettings.DefaultEntitySettings));
    }

    public void SaveLoadout(EntityPack pack)
    {
        File.WriteAllBytes(Path.Combine(_loadoutPath.FullName, $"{pack.Name}.loadout"), MessagePackSerializer.Serialize(pack));
    }

    private void OnApplicationQuit() => SaveState();

    public void SaveState()
    {
        PlayerSettings.SavedRun = CurrentSector == null ? null : new SavedGame(CurrentSector, Zone, DockedEntity ?? CurrentEntity);
        if(PlayerSettings.SavedRun != null)
        {
            PlayerSettings.SavedRun.IsTutorial = IsTutorial;
            PlayerSettings.SavedRun.ActionBarBindings = _actionBarSlots.Select(slot => slot.Save()).ToArray();
        }
        SavePlayerSettings();
    }

    private void OnDisable()
    {
        Input.Dispose();
        var ambience = SoundBanksInfo.GetSoundBank(Settings.AmbienceSoundBank);
        foreach (var stopEvent in ambience.IncludedEvents.Where(e => e.Name.EndsWith("_stop")))
        {
            AkSoundEngine.PostEvent(stopEvent.Id, gameObject);
        }
        StopMusic();
        AkSoundEngine.UnregisterGameObj(gameObject);
        ConsoleController.ClearCommands();
        EntityInstance.ClearWeaponManagers();
    }

    void Start()
    {
        Instance = this;
        EntityInstance.EffectManagerParent = EffectManagerParent;
        AkSoundEngine.RegisterGameObj(gameObject);
        ConsoleController.MessageReceiver = this;

        var ambience = SoundBanksInfo.GetSoundBank(Settings.AmbienceSoundBank);
        LoadSoundbank(ambience.Id);
        foreach (var playEvent in ambience.IncludedEvents.Where(e => e.Name.EndsWith("_play")))
        {
            AkSoundEngine.PostEvent(playEvent.Id, gameObject);
        }
        
        ItemManager = new ItemManager(CultCache, Settings.GameplaySettings, Debug.Log);
        ZoneRenderer.ItemManager = ItemManager;
        // TODO: Process Stories

        // _loadoutPath = GameDataDirectory.CreateSubdirectory("Loadouts");
        // Loadouts.AddRange(_loadoutPath.EnumerateFiles("*.loadout")
        //     .Select(fi => MessagePackSerializer.Deserialize<EntityPack>(File.ReadAllBytes(fi.FullName))));

        #region Input Handling

        Input = new AetheriaInput();
        foreach (var x in PlayerSettings.InputSettings.InputActionMap) Input.asset[x.Key.action].ApplyBindingOverride(x.Key.binding, x.Value);

        InputDisplayLayout.Input = Input.asset;
        Input.Global.Enable();

        _zoomLevelIndex = Settings.DefaultMinimapZoom;
        Input.Player.MinimapZoom.performed += context =>
        {
            _zoomLevelIndex = (_zoomLevelIndex + 1) % Settings.MinimapZoomLevels.Length;
            ZoneRenderer.MinimapDistance = Settings.MinimapZoomLevels[_zoomLevelIndex];
        };

        Input.Global.ZoneMap.performed += context =>
        {
            ToggleMenuTab(MenuTab.Map);
            MenuMap.Position = CurrentEntity.Position.xz;
        };

        Input.Global.Inventory.performed += context => ToggleMenuTab(MenuTab.Inventory);

        Input.Global.GalaxyMap.performed += context => ToggleMenuTab(MenuTab.Galaxy);

        Input.Global.Interact.performed += context =>
        {
            if (EventSystem.current.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null) return;
            if (MainMenu.gameObject.activeSelf) return;
            if (CurrentEntity == null)
            {
                AkSoundEngine.PostEvent("UI_Fail", gameObject);
                Dialog.Clear();
                Dialog.Title.text = "Can't undock. You dont have a ship!";
                Dialog.Show();
                Dialog.MoveToCursor();
            }
            else if (CurrentEntity.Parent == null)
            {
                foreach (var wormhole in ZoneRenderer.WormholeInstances.Keys)
                {
                    if (!(length(wormhole.Position - CurrentEntity.Position.xz) < Settings.GameplaySettings.WormholeExitRadius)) continue;
                    EnterWormhole(wormhole);
                }
                Dock();
            }
            else Undock();
        };

        Input.Global.MainMenu.performed += context => ToggleFullscreenMenu(MainMenu.gameObject);
        
        Input.Global.InputScreen.performed += context => ToggleFullscreenMenu(HelpScreen);

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
            if (!CurrentEntity.VisibleHostiles.Any()) return;
            var underReticle = CurrentEntity.VisibleHostiles.Where(x => x != CurrentEntity)
                .MaxBy(x => dot(normalize(x.Position - CurrentEntity.Position), CurrentEntity.LookDirection));
            CurrentEntity.Target.Value = CurrentEntity.Target.Value == underReticle ? null : underReticle;
        };

        Input.Player.TargetNearest.performed += context =>
        {
            if(CurrentEntity.VisibleHostiles.Any())
            {
                CurrentEntity.Target.Value = CurrentEntity.VisibleHostiles.Where(x => x != CurrentEntity)
                    .MaxBy(x => length(x.Position - CurrentEntity.Position));
            }
        };

        Input.Player.TargetNext.performed += context =>
        {
            if (!CurrentEntity.VisibleHostiles.Any()) return;
            var targets = CurrentEntity.VisibleHostiles.Where(x => x != CurrentEntity).OrderBy(x => length(x.Position - CurrentEntity.Position)).ToArray();
            var currentTargetIndex = Array.IndexOf(targets, CurrentEntity.Target.Value);
            CurrentEntity.Target.Value = targets[(currentTargetIndex + 1) % targets.Length];
        };

        Input.Player.TargetPrevious.performed += context =>
        {
            if (!CurrentEntity.VisibleHostiles.Any()) return;
            var targets = CurrentEntity.VisibleHostiles.Where(x => x != CurrentEntity).OrderBy(x => length(x.Position - CurrentEntity.Position)).ToArray();
            var currentTargetIndex = Array.IndexOf(targets, CurrentEntity.Target.Value);
            CurrentEntity.Target.Value = targets[(currentTargetIndex + targets.Length - 1) % targets.Length];
        };
        
        #endregion


        #region Action Bar

        ActionBarSlot createBinding(string controlPath)
        {
            var action = new InputAction(binding: controlPath);
            _actionBarActions.Add(action);
            var slot = Instantiate(ActionBarSlot, ActionBar);
            slot.Binding = null;
            _actionBarSlots.Add(slot);
            action.started += context => slot.Binding?.Activate();
            action.canceled += context => slot.Binding?.Deactivate();

            var shortName = controlPath.Substring(controlPath.LastIndexOf('/') + 1);
            var sprite = Resources.Load<Sprite>($"Sprites/Input/{shortName}");
            if (sprite != null)
            {
                slot.InputIcon.sprite = sprite;
                slot.InputLabel.gameObject.SetActive(false);
            }
            else
            {
                slot.InputLabel.text = shortName;
                slot.InputIcon.gameObject.SetActive(false);
            }

            slot.PointerEnterTrigger.OnPointerEnterAsObservable().Subscribe(_ =>
            {
                //Debug.Log($"Pointer entered action bar slot {controlPath}");
                RegisterDragTarget(dragAction =>
                {
                    //Debug.Log("Registering binding!");
                    switch (dragAction)
                    {
                        case EquippedItemDragObject equippedItemDragAction:
                            var trigger = equippedItemDragAction.EquippedItem.GetBehavior<IActivatedBehavior>();
                            if (trigger == null) return false;
                            slot.Binding = new ActionBarGearBinding(CurrentEntity, slot, equippedItemDragAction.EquippedItem, trigger);
                            return true;
                        case ItemInstanceDragObject itemInstanceDragAction:
                            if (!(itemInstanceDragAction.Item.Data.Value is ConsumableItemData consumable)) return false;
                            slot.Binding = new ActionBarConsumableBinding(CurrentEntity, slot, consumable);
                            return true;
                        case WeaponGroupDragObject weaponGroupDragAction:
                            slot.Binding = new ActionBarWeaponGroupBinding(CurrentEntity, slot, weaponGroupDragAction.Group);
                            return true;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(dragAction));
                    }
                });
            });
            slot.PointerExitTrigger.OnPointerExitAsObservable().Subscribe(_ =>
            {
                //Debug.Log($"Pointer exited action bar slot {controlPath}");
                UnregisterDragTarget();
            });
            return slot;
        }

        foreach (var actionBarInput in PlayerSettings.InputSettings.ActionBarInputs) createBinding(actionBarInput);

        #endregion

        #endregion
        
        StartGame();
        
        ConsoleController.AddCommand("revealzones",
            _ =>
            {
                foreach (var zones in CurrentSector.Zones
                    .Where(z=>!CurrentSector.DiscoveredZones.Contains(z))
                    .GroupBy(z=>z.Distance[CurrentSector.Entrance])
                    .OrderBy(g=>g.Key))
                {
                    SectorMap.QueueZoneReveal(zones);
                }
            });
        
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
        
        ConsoleController.AddCommand("spawnturret",
            _ =>
            {
                var nearestFaction = CurrentSector.Factions.MinBy(f => CurrentSector.HomeZones[f].Distance[Zone.SectorZone]);

                var loadoutGenerator = IsTutorial ? new LoadoutGenerator(
                    ref ItemManager.Random,
                    ItemManager,
                    CurrentSector,
                    Zone.SectorZone,
                    nearestFaction,
                    .5f) : new LoadoutGenerator(
                    ref ItemManager.Random,
                    ItemManager,
                    nearestFaction,
                    .5f);

                var turret = EntitySerializer.Unpack(ItemManager, Zone, loadoutGenerator.GenerateTurretLoadout(), true);
                turret.Position.xz = _currentEntity.Position.xz +
                                     ItemManager.Random.NextFloat2Direction() * ItemManager.Random.NextFloat(50, 500);
                turret.Zone = Zone;
                Zone.Entities.Add(turret);
                turret.Activate();
            });
        
        // ConsoleController.AddCommand("pingscene",
        //     _ =>
        //     {
        //         var startTime = Time.time;
        //         Observable.EveryUpdate().TakeWhile(_ => Time.time - startTime < 5).Subscribe(
        //             _ => Debug.Log($"{(int) (Time.time - startTime)}"),
        //             () =>
        //             {
        //                 var nearestFaction = CurrentSector.Factions.MinBy(f => CurrentSector.HomeZones[f].Distance[Zone.SectorZone]);
        //                 var nearestFactionHomeZone = CurrentSector.HomeZones[nearestFaction];
        //                 var factionPresence = nearestFaction.InfluenceDistance - nearestFactionHomeZone.Distance[Zone.SectorZone] + 1;
        //
        //                 var loadoutGenerator = new LoadoutGenerator(
        //                     ref ItemManager.Random,
        //                     ItemManager,
        //                     CurrentSector,
        //                     Zone.SectorZone,
        //                     nearestFaction,
        //                     .5f);
        //
        //                 for (int i = 0; i < 8; i++)
        //                 {
        //                     var ship = EntitySerializer.Unpack(ItemManager, Zone, loadoutGenerator.GenerateShipLoadout(), true);
        //                     ship.Position.xz = _currentEntity.Position.xz +
        //                                        ItemManager.Random.NextFloat2Direction() * ItemManager.Random.NextFloat(50, 500);
        //                     ship.Zone = Zone;
        //                     Zone.Entities.Add(ship);
        //                     ship.Activate();
        //                 }
        //
        //                 for (int i = 0; i < 8; i++)
        //                 {
        //                     var turret = EntitySerializer.Unpack(ItemManager, Zone, loadoutGenerator.GenerateTurretLoadout(), true);
        //                     turret.Position.xz = _currentEntity.Position.xz +
        //                                          ItemManager.Random.NextFloat2Direction() * ItemManager.Random.NextFloat(50, 500);
        //                     turret.Zone = Zone;
        //                     Zone.Entities.Add(turret);
        //                     turret.Activate();
        //                 }
        //             });
        //     });
    }

    public void BeginDrag(DragObject dragObject)
    {
        this.DragObject = dragObject;
    }

    public void RegisterDragTarget(Func<DragObject, bool> onEndDrag)
    {
        _endDragCallback = onEndDrag;
    }

    public void UnregisterDragTarget()
    {
        _endDragCallback = null;
    }

    public bool EndDrag()
    {
        var success = _endDragCallback?.Invoke(DragObject);
        DragObject = null;
        return success ?? false;
    }

    public void EnablePlayerInput()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Input.Player.Enable();
        foreach (var a in _actionBarActions) a.Enable();
    }

    public void DisablePlayerInput()
    {
        Cursor.lockState = CursorLockMode.None;
        Input.Player.Disable();
        foreach (var a in _actionBarActions) a.Disable();
    }

    private void EnterWormhole(Wormhole wormhole)
    {
        if (!(CurrentEntity is Ship ship)) return;
        // var wormholeCameraFollow = new GameObject("Wormhole Camera Follow").transform;
        // wormholeCameraFollow.position = new Vector3(wormhole.Position.x, -50, wormhole.Position.y);
        // wormholeCameraFollow.rotation = Quaternion.LookRotation(Vector3.down, ship.LookDirection);
        // WormholeCamera.enabled = true;
        // WormholeCamera.Follow = wormholeCameraFollow;
        // FollowCamera.enabled = false;
        ship.EnterWormhole(wormhole.Position);
        ship.OnEnteredWormhole += () =>
        {
            var oldZone = Zone;
            PopulateLevel(wormhole.Target);
            foreach (var zone in wormhole.Target.AdjacentZones)
                CurrentSector.DiscoveredZones.Add(zone);
            SectorMap.QueueZoneReveal(wormhole.Target.AdjacentZones);
            ship.ExitWormhole(ZoneRenderer.WormholeInstances.Keys.First(w => w.Target == oldZone.SectorZone).Position,
                Settings.GameplaySettings.WormholeExitVelocity * ItemManager.Random.NextFloat2Direction());
            CurrentEntity.Zone = Zone;
            SaveState();
        };
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
                sectorZone,
                IsTutorial
            );
            sectorZone.Contents = new Zone(ItemManager, Settings.PlanetSettings, sectorZone.PackedContents, sectorZone);
        }
        Zone = sectorZone.Contents;
        PlayMusic(MusicType.Overworld);
        
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

    private void ToggleFullscreenMenu(GameObject menu)
    {
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null) return;
        if (CurrentEntity == null) return;
        if (menu.activeSelf)
        {
            _paused = false;
            VolumeRenderer.EnableDepth = true;
            menu.SetActive(false);
            UiRoot.SetActive(true);
            if (!_menuShown)
            {
                EnablePlayerInput();
                UpdatePlayerPanel();
                UpdateTargetPanel(CurrentEntity.Target.Value);
            }
        }
        else
        {
            _paused = true;
            VolumeRenderer.EnableDepth = false;
            menu.SetActive(true);
            UiRoot.SetActive(false);
            _menuShown = Menu.gameObject.activeSelf;
            if (!_menuShown) DisablePlayerInput();
        }
    }

    private void ToggleMenuTab(MenuTab tab)
    {
        if (EventSystem.current.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null) return;
        if (MainMenu.gameObject.activeSelf) return;
        if (Menu.gameObject.activeSelf && Menu.CurrentTab == tab)
        {
            Menu.gameObject.SetActive(false);
            if (CurrentEntity != null && CurrentEntity.Parent == null)
            {
                EnablePlayerInput();
                UpdatePlayerPanel();
                UpdateTargetPanel(CurrentEntity.Target.Value);
                GameplayUI.gameObject.SetActive(true);
            }
            return;
        }

        DisablePlayerInput();
        Menu.ShowTab(tab);
        GameplayUI.gameObject.SetActive(false);
    }

    private void StartGame()
    {
        if (CurrentSector != null)
        {
            if (PlayerSettings.SavedRun == null)
            {
                SectorMap.QueueZoneReveal(CurrentSector.Entrance.AdjacentZones.Prepend(CurrentSector.Entrance));
                PopulateLevel(CurrentSector.Entrance);
                var loadoutGenerator = IsTutorial ? new LoadoutGenerator(ref ItemManager.Random, ItemManager, null, 2) :
                    new LoadoutGenerator(ref ItemManager.Random, ItemManager, CurrentSector, Zone.SectorZone, null, 2);
                var ship = EntitySerializer.Unpack(
                    ItemManager, 
                    Zone, 
                    loadoutGenerator.GenerateShipLoadout(data => string.IsNullOrEmpty(Settings.StartingHullName) || data.Name==Settings.StartingHullName ), 
                    true);
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
                foreach(var group in CurrentSector.DiscoveredZones
                    .GroupBy(dz=>dz.Distance[CurrentSector.Entrance]))
                    SectorMap.QueueZoneReveal(group);
                PopulateLevel(CurrentSector.Zones[PlayerSettings.SavedRun.CurrentZone]);
                var targetEntity = Zone.Entities[PlayerSettings.SavedRun.CurrentZoneEntity];
                if (targetEntity is OrbitalEntity orbitalEntity)
                {
                    CurrentEntity = targetEntity.Children.First(c => c is Ship {IsPlayerShip: true});
                    DoDock(orbitalEntity, orbitalEntity.DockingBays.First());
                }
                else
                {
                    //StartCoroutine(IntroCutscene(targetEntity as Ship));
                    BindToEntity(targetEntity);
                }
        
                for (var i = 0; i < _actionBarSlots.Count; i++)
                {
                    _actionBarSlots[i].Restore(PlayerSettings.SavedRun.ActionBarBindings[i], CurrentEntity);
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
                        AkSoundEngine.PostEvent("Dock", gameObject);
                        return;
                    }
                }
            }
        }
    }

    private void DoDock(Entity entity, EquippedDockingBay dockingBay)
    {
        TradeMenu.Inventory = entity.CargoBays.First();
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
            else if (CurrentEntity.GetBehavior<Thruster>() == null && CurrentEntity.GetBehavior<AetherDrive>() == null)
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
        DisablePlayerInput();
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
        EnablePlayerInput();
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
        
        _shipSubscriptions.Add(CurrentEntity.TargetedByCount.Subscribe(count =>
        {
            PlayMusic(count > 0 ? MusicType.Combat : MusicType.Overworld);
        }));
        
        _shipSubscriptions.Add(CurrentEntity.Target.Subscribe(target =>
        {
            // Clear previous subscriptions related to currently targeted enemy
            foreach(var subscription in _targetSubscriptions)
                subscription.Dispose();
            _targetSubscriptions.Clear();

            UpdateTargetPanel(target);
            if (target != null)
            {
                if (target.Shield != null)
                {
                    TargetShieldsBackground.color = new Color(ShieldColor.r, ShieldColor.g, ShieldColor.b, .4f);
                    TargetShieldsIcon.color = ShieldColor;
                    TargetShieldsIcon.sprite = ShieldIcon;
                }
                else
                {
                    TargetShieldsBackground.color = new Color(NoShieldColor.r, NoShieldColor.g, NoShieldColor.b, .4f);
                    TargetShieldsIcon.color = NoShieldColor;
                    TargetShieldsIcon.sprite = NoShieldIcon;
                }
                
                // Subscribe to incoming hits from the player ship to display the hit marker
                _targetSubscriptions.Add(target.IncomingHit.Where(e => e == CurrentEntity).Subscribe(_ =>
                {
                    HitMarker.SetActive(true);
                    _hitMarkerTime = HitMarkerDuration;
                }));
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
        
        _shipSubscriptions.Add(CurrentEntity.Death.Subscribe(Die));
        
        _lockingIndicators = CurrentEntity.GetBehaviors<LockWeapon>()
            .Select(x =>
            {
                var i = LockIndicator.Instantiate<PlaceUIElementWorldspace>();
                return (x, i, i.GetComponent<Rotate>());
            }).ToArray();
    }

    private void UpdatePlayerPanel()
    {
        ShipPanel.Display(CurrentEntity, true);
        SchematicDisplay.ShowShip(CurrentEntity);
    }

    private void UpdateTargetPanel(Entity target)
    {
        TargetIndicator.gameObject.SetActive(target != null);
        TargetShipPanel.gameObject.SetActive(target != null);
        if (target != null)
        {
            TargetShipPanel.Display(target, true);
            TargetSchematicDisplay.ShowShip(target, CurrentEntity);
        }
    }

    private void Die(CauseOfDeath cause)
    {
        var deathTime = Time.time;
        UnbindEntity();
        CurrentEntity = null;
        VolumeRenderer.EnableDepth = false;
        MainMenu.gameObject.SetActive(true);
        Menu.gameObject.SetActive(false);
        CurrentSector = null;
        SavePlayerSettings();
        Observable.EveryUpdate()
            .Where(_ => Time.time - deathTime < DeathPPTransitionTime)
            .Subscribe(_ =>
                {
                    var t = (Time.time - deathTime) / DeathPPTransitionTime;
                    if(cause==CauseOfDeath.Heatstroke)
                    {
                        HeatstrokePP.weight = 1 - t;
                        SevereHeatstrokePP.weight = 1 - t;
                    }
                    else if (cause == CauseOfDeath.Hypothermia)
                    {
                        HypothermiaPP.weight = 1 - t;
                        SevereHypothermiaPP.weight = 1 - t;
                    }
                    DeathPP.weight = t;
                },
                () =>
                {
                    HeatstrokePP.weight = 0;
                    SevereHeatstrokePP.weight = 0;
                    HypothermiaPP.weight = 0;
                    SevereHypothermiaPP.weight = 0;
                    DeathPP.weight = 1;
                });
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

    public void PlayMusic(MusicType type)
    {
        var soundbank = type switch
        {
            MusicType.Overworld => Zone.SectorZone.Owner?.OverworldMusic ?? 0,
            MusicType.Combat => Zone.SectorZone.Owner?.CombatMusic ?? 0,
            MusicType.Boss => Zone.SectorZone.Owner?.BossMusic ?? 0,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
        if (soundbank == 0)
            soundbank = type switch
            {
                MusicType.Overworld => SoundBanksInfo.GetSoundBank(Settings.DefaultOverworldSoundbank).Id,
                MusicType.Combat => SoundBanksInfo.GetSoundBank(Settings.DefaultCombatSoundbank).Id,
                MusicType.Boss => SoundBanksInfo.GetSoundBank(Settings.DefaultBossSoundbank).Id,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        PlayMusic(soundbank, gameObject);
    }

    void Update()
    {
        if(!_paused)
        {
            _time += Time.deltaTime;
            _hitMarkerTime -= Time.deltaTime;
            if(HitMarker.activeSelf && _hitMarkerTime < 0) HitMarker.SetActive(false);
            // ItemManager.Time = _time;
            if(CurrentEntity !=null && CurrentEntity.Parent==null)
            {
                foreach (var indicator in _visibleHostileIndicators)
                {
                    indicator.Value.gameObject.SetActive(indicator.Key!=CurrentEntity.Target.Value);
                    indicator.Value.Place.Target = indicator.Key.Position;
                    if (!indicator.Key.Active)
                        indicator.Value.Fill.enabled = false;
                    else
                    {
                        indicator.Value.Fill.fillAmount =
                            saturate(indicator.Key.EntityInfoGathered[CurrentEntity] / Settings.GameplaySettings.TargetDetectionInfoThreshold);
                        indicator.Value.Fill.enabled =
                            !(indicator.Key.EntityInfoGathered[CurrentEntity] > Settings.GameplaySettings.TargetDetectionInfoThreshold) ||
                            sin(TargetSpottedBlinkFrequency * Time.time) + TargetSpottedBlinkOffset > 0;
                    }
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

                var target = CurrentEntity.Target.Value;
                if (target != null)
                {
                    var threshold = Settings.GameplaySettings.TargetDetectionInfoThreshold;
                    TargetVisibilityFill.fillAmount = lerp(.25f, .75f, (CurrentEntity.EntityInfoGathered[target] - threshold)/(1-threshold));
                    VisibilityToTargetFill.fillAmount = lerp(.25f, .75f, target.EntityInfoGathered[CurrentEntity] / threshold);
                    TargetHitpointsFill.fillAmount = lerp(.25f, .75f, target.Hull.Durability / target.EquippedHull.Data.Durability);
                    TargetShieldsFill.fillAmount = target.Shield == null ? 0 : lerp(.25f, .75f, target.Shield.Progress);
                }

                var tractorPower = Input.Player.TractorBeam.ReadValue<float>();
                CurrentEntity.TractorPower =
                    saturate(CurrentEntity.TractorPower + sign(tractorPower - CurrentEntity.TractorPower) * Time.deltaTime * 2);
            }
            Zone.Update(Time.deltaTime);
        }
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

public abstract class DragObject{}

public class WeaponGroupDragObject : DragObject
{
    public WeaponGroupDragObject(int group)
    {
        Group = group;
    }

    public int Group { get; }
}

public abstract class ItemDragObject : DragObject
{
    protected ItemDragObject(int2 originCellOffset, ItemInstance item)
    {
        OriginCellOffset = originCellOffset;
        Item = item;
    }

    public ItemInstance Item { get; }
    public int2 OriginCellOffset { get; }
}

public class ItemInstanceDragObject : ItemDragObject
{
    public ItemInstanceDragObject(ItemInstance item, EquippedCargoBay originInventory, int2 originCellOffset) : base(originCellOffset, item)
    {
        OriginInventory = originInventory;
    }

    public EquippedCargoBay OriginInventory { get; }
}

public class EquippedItemDragObject : ItemDragObject
{
    public EquippedItemDragObject(EquippedItem item, Entity originEntity, int2 originCellOffset) : base(originCellOffset, item.EquippableItem)
    {
        EquippedItem = item;
        OriginEntity = originEntity;
    }

    public EquippedItem EquippedItem { get; }
    public Entity OriginEntity { get; }
}