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
    public TextMeshProUGUI Title;
    public GridLayoutGroup Grid;
    public Sprite[] NodeTextures;
    public Prototype NodePrototype;

    private Dictionary<int2, GameObject> _nodeInstances;
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

    public void Display(ItemManager manager, Entity entity)
    {
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
        Grid.constraintCount = cargo.Data.Shape.Width;
        _nodeInstances = cargo.Data.Shape.AllCoordinates
            .ToDictionary(v => v,
                v =>
                {
                    if (!cargo.Data.Shape[v])
                    {
                        var empty = new GameObject("Empty Node", typeof(RectTransform));
                        empty.transform.SetParent(Grid.transform);
                        return empty;
                    }

                    var spriteIndex = 0;
                    var item = cargo.Occupancy[v.x, v.y];

                    bool ItemMatch(int2 offset) => 
                        !cargo.Data.Shape[v + offset] || 
                        item == null || 
                        cargo.Occupancy[v.x + offset.x, v.y + offset.y] != item;
                    
                    for(int i = 0; i < 8; i++)
                        if (ItemMatch(_offsets[i]))
                            spriteIndex += 1 << i;

                    if (spriteIndex == 0)
                    {
                        var empty = new GameObject("Empty Node", typeof(RectTransform));
                        empty.transform.SetParent(Grid.transform);
                        return empty;
                    }
                    
                    var node = NodePrototype.Instantiate<Prototype>().gameObject;
                    var image = node.GetComponentInChildren<Image>();
                    image.sprite = NodeTextures[spriteIndex];
                    var itemData = manager.GetData(item);
                    if (item == null)
                        image.color = Color.white * .5f;
                    else if (itemData is EquippableItemData equippable)
                        image.color = HardpointData.GetColor(equippable.HardpointType).ToColor();
                    else
                        image.color = Color.white;
                    return node;
                });
    }
}
