using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ZoneObject : MonoBehaviour
{
    public MeshRenderer Icon;

    public TextMeshPro Label;

    public TextMeshPro Message;
    
    [HideInInspector]
    public List<MeshRenderer> Children = new List<MeshRenderer>();
}
