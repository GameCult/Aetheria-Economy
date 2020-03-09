using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoneRenderer : MonoBehaviour
{
    public Material BackgroundMaterial;
    public RenderTexture GravityTexture;
    public MeshRenderer GravityRenderer;
    
    private int _resX, _resY;
    private float _ratio;
    private Camera _camera;

    private void Start()
    {
        _camera = GetComponent<Camera>();
    }

    private void OnPreRender()
    {
        if (Screen.width != _resX || Screen.height != _resY)
        {
            _resX = Screen.width;
            _resY = Screen.height;
            _ratio = (float) _resY / _resX;
            if (GravityTexture != null)
            {
                GravityTexture?.Release();
            }
            GravityTexture = new RenderTexture(_resX, _resY, 0, RenderTextureFormat.RFloat);
        }
        
        BackgroundMaterial.mainTexture = GravityTexture;

        GravityRenderer.transform.position = new Vector3(transform.position.x, transform.position.y, 0);
        var size = _camera.orthographicSize;
        GravityRenderer.transform.localScale = new Vector3(size * 4, size * 4 * _ratio, 1);
        
        _camera.targetTexture = GravityTexture;
    }
}
