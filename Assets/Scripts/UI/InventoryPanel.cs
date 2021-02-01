/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.Rendering;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using int2 = Unity.Mathematics.int2;

public class InventoryPanel : MonoBehaviour
{
    public bool Flip;
    public GameSettings Settings;
    public ActionGameManager GameManager;
    public ContextMenu ContextMenu;
    public TextMeshProUGUI Title;
    public TextMeshProUGUI MinTempLabel;
    public TextMeshProUGUI MaxTempLabel;
    public GameObject TemperatureRange;
    public Button Dropdown;
    public Button Current;
    public Button Thermal;
    public GridLayoutGroup Grid;
    public Sprite[] NodeBackgroundTextures;
    public Sprite[] NodeTextures;
    public Sprite[] ThermalTextures;
    public float ThermalToggleRegionSize = .2f;
    public Prototype NodePrototype;
    public RawImage TemperatureDisplay;
    public Texture2D TemperatureColor;
    public ExponentialLerp TemperatureColorCurve;
    public ExponentialLerp TemperatureAlphaCurve;
    public float MaxTemperature;
    public bool FitToContent;
    public float CellHitPulseTime;
    public Color ToggleEnabledColor;
    public Color ToggleDisabledColor;
    public Color CellBackgroundColor = new Color(0, 0, 0, .75f);
    
    Subject<InventoryEventData> _onBeginDrag;
    Subject<InventoryEventData> _onDrag;
    Subject<InventoryEventData> _onEndDrag;
    Subject<InventoryEventData> _onClick;
    Subject<InventoryEventData> _onPointerEnter;
    Subject<InventoryEventData> _onPointerExit;

    private Entity _displayedEntity;
    private EquippedCargoBay _displayedCargo;
    private Texture2D _temperatureTexture;
    private RectTransform _firstRect;

    public EquippedItem FakeEquipment;
    public ItemInstance FakeItem;
    public Shape FakeOccupancy;
    public Shape IgnoreOccupancy;
    public List<GameObject> EmptyCells = new List<GameObject>();
    public Dictionary<int2, InventoryCell> CellInstances = new Dictionary<int2, InventoryCell>();
    
    private List<IDisposable> _subscriptions = new List<IDisposable>();
    private Dictionary<int2, int> _cellAnimationSequence = new Dictionary<int2, int>();
    private int _hitSequence = 0;
    private bool _thermal = false;

    private bool _hud;
    
    private int2[] _offsets = {
        int2(0, 1),
        int2(1, 0),
        int2(0, -1),
        int2(-1, 0),
        int2(1, 1),
        int2(1, -1),
        int2(-1, -1),
        int2(-1, 1)
    };

    private void Start()
    {
        if (Thermal)
        {
            Thermal.onClick.AddListener(() =>
            {
                Display(_displayedEntity, false, !_thermal);
            });
        }

        if (Current)
        {
            Current.onClick.AddListener(() =>
            {
                if (_displayedEntity != GameManager.CurrentShip)
                {
                    GameManager.CurrentShip = (Ship) _displayedEntity;
                    GameManager.DockingBay.DockedShip = (Ship) _displayedEntity;
                    Current.targetGraphic.color = ToggleEnabledColor;
                }
            });
        }
        if(Dropdown)
        {
            Dropdown.onClick.AddListener(() =>
            {
                ContextMenu.Clear();
                foreach (var ship in GameManager.AvailableShips())
                {
                    if (ship.CargoBays.Any())
                    {
                        var options = ship.CargoBays
                            .Where(bay => bay != _displayedCargo)
                            .Select<EquippedCargoBay, (string text, Action action, bool enabled)>((bay, index) =>
                                ($"Bay {index}", () => Display(bay), true));
                        if(ship != _displayedEntity) options = options.Prepend(("Equipment", () => Display(ship), true));
                        ContextMenu.AddDropdown(ship.Name, options);
                    }
                    else if(ship != _displayedEntity) ContextMenu.AddOption(ship.Name, () => Display(ship));
                }

                if(GameManager.DockingBay!=null && _displayedCargo!=GameManager.DockingBay)
                    ContextMenu.AddOption(GameManager.DockingBay.Name, () => Display(GameManager.DockingBay));
                
                ContextMenu.AddOption("Save Loadout",
                    () =>
                    {
                        GameManager.SaveLoadout(EntityPack.Pack(_displayedEntity));
                    });

                if (GameManager.Loadouts.Any())
                {
                    ContextMenu.AddDropdown("Restore Loadout", 
                        GameManager.Loadouts.Select<EntityPack, (string text, Action action, bool enabled)>(pack => 
                            (
                                $"{pack.Name} - {pack.Price(GameManager.ItemManager):n0}", () =>
                                {
                                    var ship = EntityPack.Unpack(GameManager.ItemManager, GameManager.Zone, pack, true);
                                    ship.SetParent(GameManager.DockedEntity);
                                    GameManager.PlayerShips.Add(ship);
                                    GameManager.Credits -= pack.Price(GameManager.ItemManager);
                                    GameManager.CurrentShip = ship;
                                    GameManager.DockingBay.DockedShip = ship;
                                    Display(ship);
                                }, pack.Price(GameManager.ItemManager) < GameManager.Credits
                                )));
                }

                ContextMenu.Show();
            });
        }
    }

    private void Update()
    {
        if (_displayedEntity != null && TemperatureDisplay)
        {
            if(MinTempLabel)
            {
                MinTempLabel.text = $"{((int) (_displayedEntity.MinTemp - 273.15f)).ToString()}°C";
                MaxTempLabel.text = $"{((int) (_displayedEntity.MaxTemp - 273.15f)).ToString()}°C";
            }
            var hullData = GameManager.ItemManager.GetData(_displayedEntity.Hull) as HullData;
            for(var x = 0; x < _temperatureTexture.width; x++)
            {
                for (var y = 0; y < _temperatureTexture.height; y++)
                {
                    if (hullData.Shape[int2(x-1, y-1)])
                    {
                        var temp = (_displayedEntity.Temperature[x - 1, y - 1] - _displayedEntity.MinTemp) / (_displayedEntity.MaxTemp-_displayedEntity.MinTemp);
                        var color = TemperatureColor.GetPixelBilinear(TemperatureColorCurve.Evaluate(temp), 0);
                        color.a = TemperatureAlphaCurve.Evaluate(temp);
                        _temperatureTexture.SetPixel(x,y,color);
                    }
                    else
                        _temperatureTexture.SetPixel(x,y,new Color(0,0,0,0));
                }
            }
            _temperatureTexture.Apply();
            TemperatureDisplay.rectTransform.anchoredPosition = _firstRect.anchoredPosition - Vector2.one * Grid.cellSize * 1.5f;
        }
    }

    private void OnDisable()
    {
        Clear();
    }
    

    public void Clear()
    {
        _displayedCargo = null;
        _displayedEntity = null;
        
        foreach(var empty in EmptyCells)
            Destroy(empty);
        EmptyCells.Clear();
        
        foreach(var node in CellInstances.Values)
            node.GetComponent<Prototype>().ReturnToPool();
        CellInstances.Clear();
        
        foreach(var s in _subscriptions)
            s.Dispose();
        
        _subscriptions.Clear();
        
        if (Current)
            Current.gameObject.SetActive(false);
        if (Thermal)
            Thermal.gameObject.SetActive(false);

        if (TemperatureRange)
            TemperatureRange.SetActive(false);
        
        if(TemperatureDisplay)
            TemperatureDisplay.gameObject.SetActive(false);
        
        if(Title)
            Title.text = "None";
    }

    public void Display(Entity entity, bool hud = false, bool thermal = false)
    {
        Clear();
        _thermal = thermal;
        _hud = hud;

        _displayedEntity = entity;
        _displayedCargo = null;
        _firstRect = null;
        
        if (TemperatureRange)
            TemperatureRange.SetActive(true);

        if(Title)
            Title.text = entity.Name;

        var hullData = GameManager.ItemManager.GetData(entity.Hull) as HullData;
        
        if (FitToContent)
        {
            var gridRect = Grid.GetComponent<RectTransform>();
            var rect = gridRect.rect;
            Grid.cellSize = Vector2.one * (int) min(rect.width / (hullData.Shape.Width + 1), rect.height / (hullData.Shape.Height + 1));
        }
        
        if(TemperatureDisplay)
        {
            _temperatureTexture = new Texture2D(
                hullData.Shape.Width + 2,
                hullData.Shape.Height + 2,
                TextureFormat.RGBA32,
                false,
                false);
            TemperatureDisplay.gameObject.SetActive(true);
            TemperatureDisplay.texture = _temperatureTexture;
            var tempRect = TemperatureDisplay.rectTransform;
            tempRect.sizeDelta = Grid.cellSize * new Vector2(hullData.Shape.Width + 2, hullData.Shape.Height + 2);
        }
        
        if (Current)
        {
            Current.gameObject.SetActive(true);
            Current.targetGraphic.color = entity == GameManager.CurrentShip ? ToggleEnabledColor : ToggleDisabledColor;
        }
        if (Thermal)
        {
            Thermal.gameObject.SetActive(true);
            Thermal.targetGraphic.color = thermal ? ToggleEnabledColor : ToggleDisabledColor;
        }
        
        Grid.constraintCount = hullData.Shape.Width;
        foreach (var v in hullData.Shape.AllCoordinates)
        {
            if (!hullData.Shape[v])
            {
                var empty = new GameObject("Empty Node", typeof(RectTransform));
                empty.transform.SetParent(Grid.transform);
                if (!_firstRect)
                    _firstRect = empty.GetComponent<RectTransform>();
                EmptyCells.Add(empty);
            }
            else
            {
                var cell = NodePrototype.Instantiate<InventoryCell>();
                cell.Background.color = CellBackgroundColor;
                if (!_firstRect)
                    _firstRect = cell.GetComponent<RectTransform>();
                if (!thermal)
                {
                    if(cell.PointerClickTrigger)
                    {
                        cell.PointerClickTrigger.OnPointerClickAsObservable()
                            .Subscribe(data => _onClick?.OnNext(new InventoryEntityEventData(data, v, entity)));
                        cell.BeginDragTrigger.OnBeginDragAsObservable()
                            .Subscribe(data => _onBeginDrag?.OnNext(new InventoryEntityEventData(data, v, entity)));
                        cell.DragTrigger.OnDragAsObservable()
                            .Subscribe(data => _onDrag?.OnNext(new InventoryEntityEventData(data, v, entity)));
                        cell.EndDragTrigger.OnEndDragAsObservable()
                            .Subscribe(data => _onEndDrag?.OnNext(new InventoryEntityEventData(data, v, entity)));
                        cell.PointerEnterTrigger.OnPointerEnterAsObservable()
                            .Subscribe(data => _onPointerEnter?.OnNext(new InventoryEntityEventData(data, v, entity)));
                        cell.PointerExitTrigger.OnPointerExitAsObservable()
                            .Subscribe(data => _onPointerExit?.OnNext(new InventoryEntityEventData(data, v, entity)));
                    }
                }
                else
                {
                    cell.PointerClickTrigger.OnPointerClickAsObservable().Subscribe(data =>
                    {
                        var rect = cell.GetComponent<RectTransform>();
                        var point = Rect.PointToNormalized(rect.rect, rect.InverseTransformPoint(data.position));
                        Debug.Log($"Clicked at pos {data.position}, normalized {point}");
                        if (hullData.Shape[int2(v.x - 1, v.y)] && point.x < ThermalToggleRegionSize)
                        {
                            entity.HullConductivity[v.x - 1, v.y].x = !entity.HullConductivity[v.x - 1, v.y].x;
                            RefreshCells(new []{v,int2(v.x - 1, v.y)});
                        }
                        if (hullData.Shape[int2(v.x + 1, v.y)] && point.x > 1 - ThermalToggleRegionSize)
                        {
                            entity.HullConductivity[v.x, v.y].x = !entity.HullConductivity[v.x, v.y].x;
                            RefreshCells(new []{v,int2(v.x + 1, v.y)});
                        }
                        if (hullData.Shape[int2(v.x, v.y - 1)] && point.y < ThermalToggleRegionSize)
                        {
                            entity.HullConductivity[v.x, v.y - 1].y = !entity.HullConductivity[v.x, v.y - 1].y;
                            RefreshCells(new []{v,int2(v.x, v.y - 1)});
                        }
                        if (hullData.Shape[int2(v.x, v.y + 1)] && point.y > 1 - ThermalToggleRegionSize)
                        {
                            entity.HullConductivity[v.x, v.y].y = !entity.HullConductivity[v.x, v.y].y;
                            RefreshCells(new []{v,int2(v.x, v.y + 1)});
                        }
                    });
                }
                CellInstances.Add(v, cell);
            }
        }
        RefreshCells(CellInstances.Keys);
        
        _subscriptions.Add(entity.ArmorDamage.Subscribe(hit =>
        {
            var hitCells = new[] { hit.pos };
            StartCoroutine(Pulse(hitCells, HitType.Armor, _hitSequence++));
            RefreshCells(hitCells);
        }));
        
        _subscriptions.Add(entity.ItemDamage.Subscribe(hit =>
        {
            var data = entity.ItemManager.GetData(hit.item.EquippableItem);
            var hitCells = data.Shape.Coordinates.Select(v => v + hit.item.Position).ToArray();
            StartCoroutine(Pulse(hitCells, HitType.Armor, _hitSequence++));
            RefreshCells(hitCells);
        }));
    }

    private IEnumerator Pulse(int2[] cells, HitType hitType, int sequence)
    {
        foreach (var cell in cells)
        {
            if(_cellAnimationSequence.ContainsKey(cell))
                _cellAnimationSequence[cell] = max(_cellAnimationSequence[cell], sequence);
            else
                _cellAnimationSequence[cell] = sequence;
        }
        var hitColor = hitType switch
        {
            HitType.Armor => Settings.ArmorHitColor,
            HitType.Hardpoint => Settings.HardpointHitColor,
            HitType.Gear => Settings.GearHitColor,
            _ => Color.white
        };
        float startTime = Time.time;
        while (Time.time - startTime < CellHitPulseTime)
        {
            var lerp = (Time.time - startTime) / CellHitPulseTime;
            foreach(var cell in cells)
                if(_cellAnimationSequence[cell] == sequence)
                    if(CellInstances.ContainsKey(cell))
                        CellInstances[cell].Background.color = Color.Lerp(hitColor, CellBackgroundColor, lerp);
            yield return null;
        }
        foreach (var cell in cells)
            if(CellInstances.ContainsKey(cell))
                CellInstances[cell].Background.color = CellBackgroundColor;
    }

    public void Display(EquippedCargoBay cargo)
    {
        Clear();
        
        _displayedCargo = cargo;
        _displayedEntity = null;

        if(Title)
            Title.text = cargo.Name;
        
        // FakeOccupancy = new Shape(cargo.Data.InteriorShape.Width, cargo.Data.InteriorShape.Height);
        // IgnoreOccupancy = new Shape(cargo.Data.InteriorShape.Width, cargo.Data.InteriorShape.Height);
        Grid.constraintCount = cargo.Data.InteriorShape.Width;
        foreach (var v in cargo.Data.InteriorShape.AllCoordinates)
        {
            if (!cargo.Data.InteriorShape[v])
            {
                var empty = new GameObject("Empty Node", typeof(RectTransform));
                empty.transform.SetParent(Grid.transform);
                EmptyCells.Add(empty);
            }
            else
            {
                var cell = NodePrototype.Instantiate<InventoryCell>();
                cell.PointerClickTrigger.OnPointerClickAsObservable()
                    .Subscribe(data => _onClick?.OnNext(new InventoryCargoEventData(data, v, cargo)));
                cell.BeginDragTrigger.OnBeginDragAsObservable()
                    .Subscribe(data => _onBeginDrag?.OnNext(new InventoryCargoEventData(data, v, cargo)));
                cell.DragTrigger.OnDragAsObservable()
                    .Subscribe(data => _onDrag?.OnNext(new InventoryCargoEventData(data, v, cargo)));
                cell.EndDragTrigger.OnEndDragAsObservable()
                    .Subscribe(data => _onEndDrag?.OnNext(new InventoryCargoEventData(data, v, cargo)));
                cell.PointerEnterTrigger.OnPointerEnterAsObservable()
                    .Subscribe(data => _onPointerEnter?.OnNext(new InventoryCargoEventData(data, v, cargo)));
                cell.PointerExitTrigger.OnPointerExitAsObservable()
                    .Subscribe(data => _onPointerExit?.OnNext(new InventoryCargoEventData(data, v, cargo)));
                CellInstances.Add(v, cell);
            }
        }
        RefreshCells();
    }

    public void RefreshCells()
    {
        RefreshCells(CellInstances.Keys);
    }

    public void RefreshCells(IEnumerable<int2> cells)
    {
        if (_displayedEntity != null)
        {
            foreach (var v in cells)
            {
                if(!CellInstances.ContainsKey(v)) continue;
                
                var hullData = GameManager.ItemManager.GetData(_displayedEntity.Hull) as HullData;
            
                var spriteIndex = 0;
                var interior = hullData.InteriorCells[v];
                var item = FakeOccupancy?[v]??false ? FakeEquipment : IgnoreOccupancy?[v]??false ? null : _displayedEntity.GearOccupancy[v.x, v.y];
                var hardpoint = _displayedEntity.Hardpoints[v.x, v.y];

                bool HardpointMatch(int2 offset)
                {
                    var v2 = v + offset * (Flip ? -1 : 1);
                    return !(
                        hullData.Shape[v2] &&
                        _displayedEntity.Hardpoints[v2.x, v2.y] == hardpoint &&
                        (FakeOccupancy?[v2]??false ? FakeEquipment : IgnoreOccupancy?[v2]??false ? null : _displayedEntity.GearOccupancy[v2.x, v2.y]) == item
                    );
                }

                bool NoHardpointMatch(int2 offset)
                {
                    var v2 = v + offset * (Flip ? -1 : 1);
                    return !(
                        hullData.Shape[v2] && (
                            !interior && !hullData.InteriorCells[v2] && _displayedEntity.Hardpoints[v2.x, v2.y] == null ||
                            interior && item != null && 
                            (FakeOccupancy?[v2]??false ? FakeEquipment : IgnoreOccupancy?[v2]??false ? null : _displayedEntity.GearOccupancy[v2.x, v2.y]) == item
                        )
                    );
                }

                if (hardpoint != null)
                {
                    for(int i = 0; i < 8; i++)
                        if (HardpointMatch(_offsets[i]))
                            spriteIndex += 1 << i;
                }
                else
                {
                    for(int i = 0; i < 8; i++)
                        if (NoHardpointMatch(_offsets[i]))
                            spriteIndex += 1 << i;
                }

                var bgSpriteIndex = spriteIndex;

                if (item != null)
                    spriteIndex += 1 << 8;

                if (_thermal)
                {
                    bool ThermalMatch(int2 offset)
                    {
                        if (!hullData.Shape[v + offset]) return false;
                        var i = (offset.x, offset.y);
                        return i switch
                        {
                            (1, 0) => _displayedEntity.HullConductivity[v.x, v.y].x,
                            (-1, 0) => _displayedEntity.HullConductivity[v.x-1, v.y].x,
                            (0, 1) => _displayedEntity.HullConductivity[v.x, v.y].y,
                            (0, -1) => _displayedEntity.HullConductivity[v.x, v.y-1].y,
                            _ => false
                        };
                    }
                    spriteIndex = 0;
                    for(int i = 0; i < 4; i++)
                        if (ThermalMatch(_offsets[i]))
                            spriteIndex += 1 << i;
                }
            
                CellInstances[v].Background.sprite = NodeBackgroundTextures[bgSpriteIndex];
                CellInstances[v].Icon.sprite = _thermal ? ThermalTextures[spriteIndex] : NodeTextures[spriteIndex];
                CellInstances[v].Icon.color = GetColor(v);
            }
        }
        else if(_displayedCargo != null)
        {
            foreach (var v in cells)
            {
                if(!CellInstances.ContainsKey(v)) continue;
                
                var spriteIndex = 0;
                var item = FakeOccupancy?[v]??false ? FakeItem : IgnoreOccupancy?[v]??false ? null : _displayedCargo.Occupancy[v.x, v.y];

                bool ItemMatch(int2 offset)
                {
                    var v2 = v + offset;
                    return !_displayedCargo.Data.InteriorShape[v2] || item == null ||
                           (FakeOccupancy?[v2]??false ? FakeItem : IgnoreOccupancy?[v2]??false ? null : _displayedCargo.Occupancy[v2.x, v2.y]) != item;
                }

                for(int i = 0; i < 8; i++)
                    if (ItemMatch(_offsets[i]))
                        spriteIndex += 1 << i;
                
                var bgSpriteIndex = spriteIndex;

                if (item != null)
                    spriteIndex += 1 << 8;
            
                CellInstances[v].Background.sprite = NodeBackgroundTextures[bgSpriteIndex];
                CellInstances[v].Icon.sprite = NodeTextures[spriteIndex];
                CellInstances[v].Icon.color = GetColor(v);
            }
        }
    }

    public Color GetColor(int2 position, bool highlight = false)
    {
        if (_displayedEntity != null)
        {
            var hullData = GameManager.ItemManager.GetData(_displayedEntity.Hull) as HullData;
            var interior = hullData.InteriorCells[position];
            var hardpoint = _displayedEntity.Hardpoints[position.x, position.y];
            var item = FakeOccupancy?[position] ?? false ? FakeEquipment : IgnoreOccupancy?[position] ?? false ? null : _displayedEntity.GearOccupancy[position.x, position.y];

            if (_hud)
            {
                if (_displayedEntity.Armor[position.x, position.y] > .01f)
                    return Settings.ArmorGradient.Evaluate(_displayedEntity.Armor[position.x, position.y] / _displayedEntity.MaxArmor[position.x, position.y]);

                if (item != null) return Settings.DurabilityGradient.Evaluate(item.EquippableItem.Durability / _displayedEntity.ItemManager.GetData(item.EquippableItem).Durability);
                return float3(.25f).ToColor();
            }

            if (hardpoint == null)
            {
                if(!interior)
                    return Color.white;
                
                if (item == null)
                {
                    return float3(.25f).ToColor();
                }
                
                if(highlight)
                    return Color.white;
                
                return float3(.5f).ToColor();
            }

            var tint = hardpoint.TintColor;
            
            if (!highlight || item == null)
                tint *= .7071f;

            return tint.ToColor();
        }
        else
        {
            var item = FakeOccupancy?[position] ?? false ? FakeItem : IgnoreOccupancy?[position] ?? false ? null : _displayedCargo.Occupancy[position.x, position.y];
        
            if (item == null)
                return Color.white * .25f;

            var c = float3(1);
            if (GameManager.ItemManager.GetData(item) is EquippableItemData equippable)
                c = HardpointData.GetColor(equippable.HardpointType);
            
            if(!highlight)
                c *= .7071f;

            return c.ToColor();
        }
    }

    public bool DropItem(ItemInstance item, int2 position)
    {
        if (_displayedEntity != null && item is EquippableItem equippableItem)
        {
            return _displayedEntity.TryEquip(equippableItem, position);
        }

        if(_displayedCargo != null)
        {
            return _displayedCargo.TryStore(item, position);
        }

        return false;
    }

    public bool DropItem(ItemInstance item)
    {
        if (_displayedEntity != null && item is EquippableItem equippableItem)
        {
            return _displayedEntity.TryEquip(equippableItem);
        }

        if(_displayedCargo != null)
        {
            return _displayedCargo.TryStore(item);
        }

        return false;
    }
    
    public UniRx.IObservable<InventoryEventData> OnClickAsObservable() => _onClick ?? (_onClick = new Subject<InventoryEventData>());
    public UniRx.IObservable<InventoryEventData> OnBeginDragAsObservable() => _onBeginDrag ?? (_onBeginDrag = new Subject<InventoryEventData>());
    public UniRx.IObservable<InventoryEventData> OnDragAsObservable() => _onDrag ?? (_onDrag = new Subject<InventoryEventData>());
    public UniRx.IObservable<InventoryEventData> OnEndDragAsObservable() => _onEndDrag ?? (_onEndDrag = new Subject<InventoryEventData>());
    public UniRx.IObservable<InventoryEventData> OnPointerEnterAsObservable() => _onPointerEnter ?? (_onPointerEnter = new Subject<InventoryEventData>());
    public UniRx.IObservable<InventoryEventData> OnPointerExitAsObservable() => _onPointerExit ?? (_onPointerExit = new Subject<InventoryEventData>());
}

public abstract class InventoryEventData
{
    protected InventoryEventData(PointerEventData pointerEventData, int2 position)
    {
        PointerEventData = pointerEventData;
        Position = position;
    }

    public PointerEventData PointerEventData { get; }
    public int2 Position { get; }
}

public class InventoryEntityEventData : InventoryEventData
{
    public InventoryEntityEventData(PointerEventData pointerEventData, int2 position, Entity entity) : 
        base(pointerEventData, position)
    {
        Entity = entity;
    }

    public Entity Entity { get; }
}

public class InventoryCargoEventData : InventoryEventData
{
    public InventoryCargoEventData(PointerEventData pointerEventData, int2 position, EquippedCargoBay cargoBay) : 
        base(pointerEventData, position)
    {
        CargoBay = cargoBay;
    }

    public EquippedCargoBay CargoBay { get; }
}
