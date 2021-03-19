using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LineMaterialOffset : MonoBehaviour
{
    public float ScrollSpeed;
    
    private Material _instancedMaterial;
    private float _offset;
    
    void Start()
    {
        _instancedMaterial = GetComponent<LineRenderer>().material;
    }

    void Update()
    {
        _offset = (_offset + Time.deltaTime * ScrollSpeed) % 1f;
        _instancedMaterial.SetTextureOffset("_MainTex", new Vector2(_offset, 0));
    }
}
