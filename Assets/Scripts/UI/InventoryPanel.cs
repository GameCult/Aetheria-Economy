using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
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
