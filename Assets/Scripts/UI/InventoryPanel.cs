using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using static Unity.Mathematics.math;

public class InventoryPanel : MonoBehaviour
{
    public TextMeshProUGUI Title;
    public GridLayoutGroup Grid;
    public Sprite[] NodeTextures;
    public Prototype NodePrototype;

    private Dictionary<int2, GameObject> _nodeInstances;

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
                    if (hardpoint != null)
                    {
                        if (hullData.Shape[v + int2(0, 1)] && 
                            entity.Hardpoints[v.x, v.y + 1] == hardpoint && 
                            entity.GearOccupancy[v.x, v.y + 1] == item)
                            spriteIndex += 1;
                        if (hullData.Shape[v + int2(1, 0)] && 
                            entity.Hardpoints[v.x + 1, v.y] == hardpoint && 
                            entity.GearOccupancy[v.x + 1, v.y] == item)
                            spriteIndex += 2;
                        if (hullData.Shape[v + int2(0, -1)] && 
                            entity.Hardpoints[v.x, v.y - 1] == hardpoint && 
                            entity.GearOccupancy[v.x, v.y - 1] == item)
                            spriteIndex += 4;
                        if (hullData.Shape[v + int2(-1, 0)] && 
                            entity.Hardpoints[v.x - 1, v.y] == hardpoint && 
                            entity.GearOccupancy[v.x - 1, v.y] == item)
                            spriteIndex += 8;
                    }
                    else
                    {
                        if (hullData.Shape[v + int2(0, 1)] && (
                            !interior && !hullData.InteriorCells[v + int2(0, 1)] && entity.Hardpoints[v.x, v.y + 1] == null ||
                            interior && item != null && entity.GearOccupancy[v.x, v.y + 1] == item))
                            spriteIndex += 1;
                        if (hullData.Shape[v + int2(1, 0)] && (
                            !interior && !hullData.InteriorCells[v + int2(1, 0)] && entity.Hardpoints[v.x + 1, v.y] == null ||
                            interior && item != null && entity.GearOccupancy[v.x + 1, v.y] == item))
                            spriteIndex += 2;
                        if (hullData.Shape[v + int2(0, -1)] && (
                            !interior && !hullData.InteriorCells[v + int2(0, -1)] && entity.Hardpoints[v.x, v.y - 1] == null ||
                            interior && item != null && entity.GearOccupancy[v.x, v.y - 1] == item))
                            spriteIndex += 4;
                        if (hullData.Shape[v + int2(-1, 0)] && (
                            !interior && !hullData.InteriorCells[v + int2(-1, 0)] && entity.Hardpoints[v.x - 1, v.y] == null ||
                            interior && item != null && entity.GearOccupancy[v.x - 1, v.y] == item))
                            spriteIndex += 8;
                    }

                    if (spriteIndex == 15)
                    {
                        var empty = new GameObject("Empty Node", typeof(RectTransform));
                        empty.transform.SetParent(Grid.transform);
                        return empty;
                    }
                    
                    var node = NodePrototype.Instantiate<Prototype>().gameObject;
                    var image = node.GetComponentInChildren<Image>();
                    image.sprite = NodeTextures[spriteIndex];
                    image.color = hardpoint != null ? hardpoint.TintColor.ToColor() : interior ? Color.white * .5f : Color.white;
                    return node;
                });
    }
}
