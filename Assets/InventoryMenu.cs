using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using Unity.Mathematics;
using UnityEngine.UI;

public class InventoryMenu : MonoBehaviour
{
    public InventoryPanel[] InventoryPanels;
    public PropertiesPanel PropertiesPanel;
    public ActionGameManager GameManager;
    public RectTransform DragParent;

    private int _currentPanel;
    
    private int2 _selectedPosition;
    private InventoryPanel _selectedPanel;
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

    public InventoryPanel GetPanel => InventoryPanels[_currentPanel = (_currentPanel + 1) % InventoryPanels.Length];
    
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
                    }
                }
                else if (data is InventoryEntityEventData entityEvent)
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
                        foreach(var cell in _dragCells)
                        {
                            foreach (var component in cell.GetComponentsInChildren<ObservableTriggerBase>())
                                component.enabled = false;
                            cell.GetComponentInChildren<Image>().raycastTarget = false;
                        }
                        _dragOffsets = _dragCells.Select(x => (Vector2) x.position - data.PointerEventData.position).ToArray();
                        panel.IgnoreOccupancy = _originalOccupancy;
                        panel.RefreshCells(_originalOccupancy.Expand().Coordinates);
                    }
                }
            });
            
            
            panel.OnDragAsObservable().Subscribe(data =>
            {
                if (_dragItem == null) return;
                var scroll = data.PointerEventData.scrollDelta.y;
                if(scroll != 0)
                    Debug.Log($"Scrolling {scroll}!");
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
                    }
                    else if (data is InventoryEntityEventData entityEvent)
                    {
                        entityEvent.Entity.Unequip(_originalEquippedItem);
                    }
                    _dragTargetPanel.DropItem(_dragItem, _dragTargetPosition);
                    _dragTargetPanel.FakeOccupancy = null;
                }
                else
                {
                    _dragItem.Rotation = _originalRotation;
                }
                panel.IgnoreOccupancy = null;
                panel.RefreshCells();
                if(_dragTargetPanel!=panel)
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
                
                Debug.Log($"Pointer entered {data.Position}");
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
                        if (_selectedPanel != null)
                        {
                            foreach (var v in _selectedItemData.Shape.Coordinates)
                            {
                                var v2 = v + _selectedPosition;
                                _selectedPanel.CellInstances[v2].Icon.color = _selectedPanel.GetColor(v2);
                            }
                        }
                        _selectedPanel = panel;
                        _selectedPosition = cargoEvent.Position;
                        PropertiesPanel.Clear();
                        PropertiesPanel.AddItemProperties(item);
                        _selectedPanel = panel;
                        _selectedPosition = cargoEvent.CargoBay.Cargo[item];
                        _selectedItemData = GameManager.ItemManager.GetData(item);
                        foreach (var v in _selectedItemData.Shape.Coordinates)
                        {
                            var v2 = _selectedItemData.Shape.Rotate(v, item.Rotation) + _selectedPosition;
                            _selectedPanel.CellInstances[v2].Icon.color = _selectedPanel.GetColor(v2, true);
                        }
                    }
                }
                else if (data is InventoryEntityEventData entityEvent)
                {
                    var item = entityEvent.Entity.GearOccupancy[entityEvent.Position.x, entityEvent.Position.y];
                    if (item != null)
                    {
                        if (_selectedPanel != null)
                        {
                            foreach (var v in _selectedItemData.Shape.Coordinates)
                            {
                                var v2 = v + _selectedItemData.Shape.Rotate(_selectedPosition, item.EquippableItem.Rotation);
                                _selectedPanel.CellInstances[v2].Icon.color = _selectedPanel.GetColor(v2);
                            }
                        }
                        PropertiesPanel.Inspect(entityEvent.Entity, item);
                        _selectedPanel = panel;
                        _selectedPosition = item.Position;
                        _selectedItemData = GameManager.ItemManager.GetData(item.EquippableItem);
                        foreach (var v in _selectedItemData.Shape.Coordinates)
                        {
                            var v2 = v + _selectedPosition;
                            _selectedPanel.CellInstances[v2].Icon.color = _selectedPanel.GetColor(v2, true);
                        }
                    }
                }
            });
        }
    }

    void Update()
    {
        
    }
}
