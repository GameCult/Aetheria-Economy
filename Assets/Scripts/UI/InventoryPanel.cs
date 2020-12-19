using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using int2 = Unity.Mathematics.int2;

public class InventoryPanel : MonoBehaviour
{
    public ActionGameManager GameManager;
    public ContextMenu ContextMenu;
    public TextMeshProUGUI Title;
    public Button Dropdown;
    public GridLayoutGroup Grid;
    public Sprite[] NodeTextures;
    public Prototype NodePrototype;

    private Dictionary<int2, GameObject> _nodeInstances = new Dictionary<int2, GameObject>();
    private int2[] _offsets = new[]
    {
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
        Dropdown.onClick.AddListener(() =>
        {
            ContextMenu.Clear();
            foreach (var ship in GameManager.AvailableShips())
            {
                if (ship.CargoBays.Any())
                {
                    ContextMenu.AddDropdown(ship.Name,
                        ship.CargoBays
                            .Select<EquippedCargoBay, (string text, Action action, bool enabled)>((bay, index) =>
                                ($"Bay {index}", () => Display(GameManager.ItemManager, bay), true))
                            .Prepend(("Equipment", () => Display(GameManager.ItemManager, ship), true)));
                }
                else ContextMenu.AddOption(ship.Name, () => Display(GameManager.ItemManager, ship));
            }

            foreach (var bay in GameManager.AvailableCargoBays())
            {
                ContextMenu.AddOption(bay.Name, () => Display(GameManager.ItemManager, bay));
            }
            ContextMenu.Show();
        });
    }

    public void Display(ItemManager manager, Entity entity)
    {
        foreach(var node in _nodeInstances.Values)
            Destroy(node);
        _nodeInstances.Clear();

        Title.text = entity.Name;
        
        var hullData = manager.GetData(entity.Hull) as HullData;
        Grid.constraintCount = hullData.Shape.Width;
        _nodeInstances = hullData.Shape.AllCoordinates
            .ToDictionary(v => v,
                v =>
                {
                    if (!hullData.Shape[v])
                    {
                        var empty = new GameObject("Empty Node", typeof(RectTransform));
                        empty.transform.SetParent(Grid.transform);
                        return empty;
                    }

                    var spriteIndex = 0;
                    var interior = hullData.InteriorCells[v];
                    var item = entity.GearOccupancy[v.x, v.y];
                    var hardpoint = entity.Hardpoints[v.x, v.y];


                    bool HardpointMatch(int2 offset) => !(hullData.Shape[v + offset] &&
                                                     entity.Hardpoints[v.x + offset.x, v.y + offset.y] == hardpoint &&
                                                     entity.GearOccupancy[v.x + offset.x, v.y + offset.y] == item);

                    bool NoHardpointMatch(int2 offset) => !(hullData.Shape[v + offset] && (
                        !interior && !hullData.InteriorCells[v + offset] && entity.Hardpoints[v.x + offset.x, v.y + offset.y] == null ||
                        interior && item != null && entity.GearOccupancy[v.x + offset.x, v.y + offset.y] == item));
                    
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

                    if (item != null)
                        spriteIndex += 1 << 8;

                    if (spriteIndex == 0)
                    {
                        var empty = new GameObject("Empty Node", typeof(RectTransform));
                        empty.transform.SetParent(Grid.transform);
                        return empty;
                    }
                    
                    var node = NodePrototype.Instantiate<Prototype>().gameObject;
                    var image = node.GetComponentInChildren<Image>();
                    image.sprite = NodeTextures[spriteIndex];
                    image.color = hardpoint?.TintColor.ToColor() ?? (interior ? Color.white * .5f : Color.white);
                    return node;
                });
    }

    public void Display(ItemManager manager, EquippedCargoBay cargo)
    {
        foreach(var node in _nodeInstances.Values)
            Destroy(node);
        _nodeInstances.Clear();

        Title.text = cargo.Name;
        
        Grid.constraintCount = cargo.Data.InteriorShape.Width;
        _nodeInstances = cargo.Data.InteriorShape.AllCoordinates
            .ToDictionary(v => v,
                v =>
                {
                    if (!cargo.Data.InteriorShape[v])
                    {
                        var empty = new GameObject("Empty Node", typeof(RectTransform));
                        empty.transform.SetParent(Grid.transform);
                        return empty;
                    }

                    var spriteIndex = 0;
                    var item = cargo.Occupancy[v.x, v.y];

                    bool ItemMatch(int2 offset) => 
                        !cargo.Data.InteriorShape[v + offset] || 
                        item == null || 
                        cargo.Occupancy[v.x + offset.x, v.y + offset.y] != item;
                    
                    for(int i = 0; i < 8; i++)
                        if (ItemMatch(_offsets[i]))
                            spriteIndex += 1 << i;

                    if (item != null)
                        spriteIndex += 1 << 8;

                    if (spriteIndex == 0)
                    {
                        var empty = new GameObject("Empty Node", typeof(RectTransform));
                        empty.transform.SetParent(Grid.transform);
                        return empty;
                    }
                    
                    var node = NodePrototype.Instantiate<Prototype>().gameObject;
                    var image = node.GetComponentInChildren<Image>();
                    image.sprite = NodeTextures[spriteIndex];
                    if (item == null)
                        image.color = Color.white * .5f;
                    else if (manager.GetData(item) is EquippableItemData equippable)
                        image.color = HardpointData.GetColor(equippable.HardpointType).ToColor();
                    else
                        image.color = Color.white;
                    return node;
                });
    }
}
