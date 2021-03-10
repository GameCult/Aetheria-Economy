/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using Unity.Mathematics;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryMenu : MonoBehaviour
{
    public InventoryPanel[] InventoryPanels;
    public PropertiesPanel PropertiesPanel;
    public ActionGameManager GameManager;
    public RectTransform DragParent;
    public ConfirmationDialog Dialog;
    public ObservablePointerEnterTrigger BackgroundEntered;
    public ObservablePointerExitTrigger BackgroundExited;

    private int _currentPanel = -1;
    
    private int2 _selectedPosition;
    private InventoryPanel _selectedPanel;
    private ItemInstance _selectedItem;
    private ItemData _selectedItemData;

    private ItemInstance _dragItem;
    private Transform[] _dragCells;
    private Vector2[] _dragOffsets;
    private int2 _dragCellOffset;
    private ItemRotation _originalRotation;
    //private Shape _previousFakeOccupancy;
    private Shape _originalOccupancy;
    private EquippedItem _originalEquippedItem;
    private InventoryPanel _originalPanel;

    private InventoryPanel _dragTargetPanel;
    private int2 _dragTargetPosition;
    private int2 _lastDragPosition;
    private bool _dragTargetValid;
    private bool _destroyItem;

    private void OnEnable()
    {
        AkSoundEngine.RegisterGameObj(gameObject);
        BackgroundEntered.gameObject.SetActive(true);

        BackgroundEntered.OnPointerEnterAsObservable().Subscribe(enter =>
        {
            _destroyItem = true;
        });
        BackgroundExited.OnPointerExitAsObservable().Subscribe(enter =>
        {
            _destroyItem = false;
        });
        var cargo = GameManager.DockingBay ?? GameManager.CurrentEntity.CargoBays.FirstOrDefault();
        if (cargo!=null)
            InventoryPanels[0].Display(cargo);
        if(GameManager.CurrentEntity != null)
            InventoryPanels[1].Display(GameManager.CurrentEntity);
        else InventoryPanels[1].Clear();
    }

    private void OnDisable()
    {
        AkSoundEngine.UnregisterGameObj(gameObject);
        BackgroundEntered.gameObject.SetActive(false);
    }

    void Start()
    {
        PropertiesPanel.Context = GameManager.ItemManager;
        foreach (var panel in InventoryPanels)
        {
            panel.OnBeginDragAsObservable().Subscribe(data =>
            {
                //Debug.Log($"Begin drag on panel {panel.name}");
                if (data is InventoryCargoEventData cargoEvent)
                {
                    var item = cargoEvent.CargoBay.Occupancy[cargoEvent.Position.x, cargoEvent.Position.y];
                    if (item != null)
                    {
                        var itemData = GameManager.ItemManager.GetData(item);

                        var itemPosition = cargoEvent.CargoBay.Cargo[item];
                        _originalOccupancy = cargoEvent.CargoBay.Data.InteriorShape.Inset(itemData.Shape, itemPosition, item.Rotation);
                        _originalRotation = item.Rotation;
                        _originalPanel = panel;
                        _dragItem = item;
                        _dragCellOffset = itemPosition - cargoEvent.Position;
                        _dragCells = _originalOccupancy.Coordinates
                            .Select(v => Instantiate(panel.CellInstances[v], DragParent, true).transform).ToArray();
                        foreach(var cell in _dragCells)
                        {
                            foreach (var component in cell.GetComponentsInChildren<ObservableTriggerBase>())
                                component.enabled = false;
                            cell.GetComponentInChildren<Image>().raycastTarget = false;
                        }
                        _dragOffsets = _dragCells.Select(x => (Vector2) x.position - data.PointerEventData.position).ToArray();
                        panel.IgnoreOccupancy = _originalOccupancy;
                        panel.RefreshCells(_originalOccupancy.Expand().Coordinates);
                        AkSoundEngine.PostEvent("Pickup", gameObject);
                    }
                }
                else if (data is InventoryEntityEventData entityEvent)
                {
                    if(!entityEvent.Entity.Active)
                    {
                        var item = entityEvent.Entity.GearOccupancy[entityEvent.Position.x, entityEvent.Position.y];
                        if (item != null)
                        {
                            var itemData = GameManager.ItemManager.GetData(item.EquippableItem);
                            var hullData = GameManager.ItemManager.GetData(entityEvent.Entity.Hull);

                            _originalRotation = item.EquippableItem.Rotation;
                            _originalEquippedItem = entityEvent.Entity.GearOccupancy[data.Position.x, data.Position.y];
                            _originalOccupancy = hullData.Shape.Inset(itemData.Shape, item.Position, item.EquippableItem.Rotation);
                            _originalPanel = panel;
                            _dragItem = item.EquippableItem;
                            _dragCellOffset = item.Position - entityEvent.Position;
                            _dragCells = _originalOccupancy.Coordinates
                                .Select(v => Instantiate(panel.CellInstances[v], DragParent, true).transform).ToArray();
                            foreach (var cell in _dragCells)
                            {
                                foreach (var component in cell.GetComponentsInChildren<ObservableTriggerBase>())
                                    component.enabled = false;
                                cell.GetComponentInChildren<Image>().raycastTarget = false;
                            }

                            _dragOffsets = _dragCells.Select(x => (Vector2) x.position - data.PointerEventData.position).ToArray();
                            panel.IgnoreOccupancy = _originalOccupancy;
                            panel.RefreshCells(_originalOccupancy.Expand().Coordinates);
                            AkSoundEngine.PostEvent("Pickup", gameObject);
                        }
                    }
                }
            });
            
            
            panel.OnDragAsObservable().Subscribe(data =>
            {
                if (_dragItem == null) return;
                for (var i = 0; i < _dragCells.Length; i++)
                    _dragCells[i].position = new Vector3(
                        data.PointerEventData.position.x + _dragOffsets[i].x,
                        data.PointerEventData.position.y + _dragOffsets[i].y,
                        _dragCells[i].position.z);
            });
            
            
            panel.OnEndDragAsObservable().Subscribe(data =>
            {
                if (_dragItem == null) return;
                if (_dragTargetValid)
                {
                    if (data is InventoryCargoEventData cargoEvent)
                    {
                        cargoEvent.CargoBay.Remove(_dragItem);
                        _dragTargetPanel.DropItem(_dragItem, _dragTargetPosition);
                        if(_dragTargetPanel.Target == InventoryPanelTarget.Cargo)
                            AkSoundEngine.PostEvent("Drop", gameObject);
                        else
                            AkSoundEngine.PostEvent("Equip", gameObject);
                    }
                    else if (data is InventoryEntityEventData entityEvent)
                    {
                        if (entityEvent.Entity.TryUnequip(_originalEquippedItem) != null)
                        {
                            _dragTargetPanel.DropItem(_dragItem, _dragTargetPosition);
                            AkSoundEngine.PostEvent("Unequip", gameObject);
                        }
                        else
                        {
                            AkSoundEngine.PostEvent("UI_Fail", gameObject);
                            Dialog.Clear();
                            Dialog.Title.text = "Unable to move item!";
                            Dialog.AddProperty("Verify that cargo bays are empty before un-equipping them.");
                            Dialog.Show();
                        }
                    }

                    _dragTargetPanel.FakeOccupancy = null;
                }
                else
                {
                    _dragItem.Rotation = _originalRotation;
                }

                if (_destroyItem)
                {
                    if (data is InventoryCargoEventData cargoEvent)
                    {
                        cargoEvent.CargoBay.Remove(_dragItem);
                    }
                    else if (data is InventoryEntityEventData entityEvent)
                    {
                        if (entityEvent.Entity.TryUnequip(_originalEquippedItem) == null)
                        {
                            Dialog.Clear();
                            Dialog.Title.text = "Unable to destroy item!";
                            Dialog.AddProperty("Verify that cargo bays are empty before un-equipping them.");
                            Dialog.Show();
                        }
                    }
                }
                
                panel.IgnoreOccupancy = null;
                panel.RefreshCells();
                if(_dragTargetPanel!=panel && _dragTargetPanel != null)
                    _dragTargetPanel.RefreshCells();
                
                _dragItem = null;
                foreach (var cell in _dragCells) Destroy(cell.gameObject);
            });

            Action floatCells = () =>
            {
                foreach (var cell in _dragCells) cell.gameObject.SetActive(true);
                _dragTargetValid = false;
            };
            
            panel.OnPointerEnterAsObservable().Subscribe(data =>
            {
                if (_dragItem == null) return;
                
                // Debug.Log($"Pointer entered {data.Position}");
                // if(panel != _originalPanel)
                //     _originalPanel.RefreshCells();
                
                _dragTargetPanel = panel;
                _lastDragPosition = data.Position;
                _dragTargetPosition = data.Position + _dragCellOffset;
                if (data is InventoryCargoEventData cargoEvent)
                {
                    if (cargoEvent.CargoBay.ItemFits(_dragItem, _dragTargetPosition))
                    {
                        foreach (var cell in _dragCells) cell.gameObject.SetActive(false);
                        var itemData = GameManager.ItemManager.GetData(_dragItem);
                        panel.FakeItem = _dragItem;
                        panel.FakeOccupancy = cargoEvent.CargoBay.Data.InteriorShape.Inset(itemData.Shape, _dragTargetPosition, _dragItem.Rotation);
                        _dragTargetValid = true;
                        panel.RefreshCells();
                    }
                }
                else if (data is InventoryEntityEventData entityEvent)
                {
                    var eqItem = _dragItem as EquippableItem;
                    var hullData = GameManager.ItemManager.GetData(entityEvent.Entity.Hull);
                    if (eqItem != null && entityEvent.Entity.ItemFits(eqItem, _dragTargetPosition))
                    {
                        foreach (var cell in _dragCells) cell.gameObject.SetActive(false);
                        var itemData = GameManager.ItemManager.GetData(_dragItem);
                        panel.FakeEquipment = _originalEquippedItem;
                        panel.FakeOccupancy = hullData.Shape.Inset(itemData.Shape, _dragTargetPosition, _dragItem.Rotation);
                        _dragTargetValid = true;
                        panel.RefreshCells();
                    }
                }
            });
            
            panel.OnPointerExitAsObservable().Subscribe(data =>
            {
                if (_dragItem == null) return;

                if (data.Position.Equals(_lastDragPosition) || !_dragTargetValid)
                {
                    panel.FakeItem = null;
                    panel.FakeOccupancy = null;
                    panel.RefreshCells();
                    // if (_originalPanel != panel)
                    // {
                    //     _originalPanel.FakeItem = null;
                    //     _originalPanel.FakeOccupancy = null;
                    //     _originalPanel.RefreshCells();
                    // }
                    floatCells();
                }
            });

            panel.OnClickAsObservable().Subscribe(data =>
            {
                if (data is InventoryCargoEventData cargoEvent)
                {
                    var item = cargoEvent.CargoBay.Occupancy[cargoEvent.Position.x, cargoEvent.Position.y];
                    if(item!=null)
                    {
                        if (Mouse.current.clickCount.ReadValue() == 2)
                        {
                            var otherPanel = panel == InventoryPanels[0] ? InventoryPanels[1] : InventoryPanels[0];
                            if (otherPanel.CanDropItem(item))
                            {
                                otherPanel.DropItem(item);
                                AkSoundEngine.PostEvent("Equip", gameObject);
                                cargoEvent.CargoBay.Remove(item);
                                panel.RefreshCells();
                                otherPanel.RefreshCells();
                            }
                            else AkSoundEngine.PostEvent("UI_Fail", gameObject);
                        }
                        else
                        {
                            if (_selectedPanel != null)
                            {
                                if (_selectedPanel.CellInstances.ContainsKey(_selectedPosition))
                                {
                                    foreach (var v in _selectedItemData.Shape.Coordinates)
                                    {
                                        var v2 = _selectedItemData.Shape.Rotate(v, _selectedItem.Rotation) + _selectedPosition;
                                        _selectedPanel.CellInstances[v2].Icon.color = _selectedPanel.GetColor(v2);
                                    }
                                }
                            }
                            _selectedPanel = panel;
                            _selectedPosition = cargoEvent.Position;
                            PropertiesPanel.Clear();
                            PropertiesPanel.AddItemProperties(item);
                            _selectedPanel = panel;
                            _selectedPosition = cargoEvent.CargoBay.Cargo[item];
                            _selectedItem = item;
                            _selectedItemData = GameManager.ItemManager.GetData(item);
                            foreach (var v in _selectedItemData.Shape.Coordinates)
                            {
                                var v2 = _selectedItemData.Shape.Rotate(v, _selectedItem.Rotation) + _selectedPosition;
                                _selectedPanel.CellInstances[v2].Icon.color = _selectedPanel.GetColor(v2, true);
                            }
                            AkSoundEngine.PostEvent("UI_Success", gameObject);
                        }
                    }
                }
                else if (data is InventoryEntityEventData entityEvent)
                {
                    var item = entityEvent.Entity.GearOccupancy[entityEvent.Position.x, entityEvent.Position.y];
                    if (item != null)
                    {
                        if (Mouse.current.clickCount.ReadValue() == 2)
                        {
                            var otherPanel = panel == InventoryPanels[0] ? InventoryPanels[1] : InventoryPanels[0];
                            if (otherPanel.CanDropItem(item.EquippableItem))
                            {
                                if (!entityEvent.Entity.Active)
                                {
                                    if (entityEvent.Entity.TryUnequip(item) != null)
                                    {
                                        otherPanel.DropItem(item.EquippableItem);
                                        AkSoundEngine.PostEvent("Unequip", gameObject);
                                        panel.RefreshCells();
                                        otherPanel.RefreshCells();
                                    }
                                    else AkSoundEngine.PostEvent("UI_Fail", gameObject);
                                }
                                else AkSoundEngine.PostEvent("UI_Fail", gameObject);
                            }
                        }
                        else
                        {
                            if (_selectedPanel != null)
                            {
                                if (_selectedPanel.CellInstances.ContainsKey(_selectedPosition))
                                {
                                    foreach (var v in _selectedItemData.Shape.Coordinates)
                                    {
                                        var v2 = _selectedItemData.Shape.Rotate(v, _selectedItem.Rotation) + _selectedPosition;
                                        _selectedPanel.CellInstances[v2].Icon.color = _selectedPanel.GetColor(v2);
                                    }
                                }
                            }

                            PropertiesPanel.Inspect(entityEvent.Entity, item);
                            _selectedPanel = panel;
                            _selectedPosition = item.Position;
                            _selectedItem = item.EquippableItem;
                            _selectedItemData = GameManager.ItemManager.GetData(item.EquippableItem);
                            foreach (var v in _selectedItemData.Shape.Coordinates)
                            {
                                var v2 = _selectedItemData.Shape.Rotate(v, _selectedItem.Rotation) + _selectedPosition;
                                _selectedPanel.CellInstances[v2].Icon.color = _selectedPanel.GetColor(v2, true);
                            }
                            AkSoundEngine.PostEvent("UI_Success", gameObject);
                        }
                    }
                }
            });

            panel.OnBackgroundClick.Subscribe(data =>
            {
                PropertiesPanel.Clear();
                PropertiesPanel.AddField("Shutdown Threshold",
                    () => GameManager.PlayerSettings.ShutdownPerformance,
                    f => GameManager.PlayerSettings.ShutdownPerformance = f,
                    0,
                    1);
            });
        }
    }

    void Update()
    {
        if (Keyboard.current.qKey.wasPressedThisFrame && _dragItem != null)
        {
            _dragItem.Rotation = (ItemRotation) (((int) _dragItem.Rotation + 1) % 4);
        }
        if (Keyboard.current.eKey.wasPressedThisFrame && _dragItem != null)
        {
            _dragItem.Rotation = (ItemRotation) (((int) _dragItem.Rotation + 3) % 4);
        }
    }
}
