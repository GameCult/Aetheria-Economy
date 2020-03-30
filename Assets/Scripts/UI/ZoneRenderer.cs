using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoneRenderer : MonoBehaviour
{
    public Camera FollowCamera;
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
        _camera.transform.position = FollowCamera.transform.position;
        if (Screen.width != _resX || Screen.height != _resY)
        {
            _resX = Screen.width;
            _resY = Screen.height;
            _ratio = (float) _resX / _resY;
            if (GravityTexture != null)
            {
                GravityTexture?.Release();
            }
            GravityTexture = new RenderTexture(_resX, _resY, 0, RenderTextureFormat.RFloat);
        }
        
        GravityRenderer.material.mainTexture = GravityTexture;

        var size = _camera.orthographicSize = FollowCamera.orthographicSize;
        
        GravityRenderer.transform.position = new Vector3(transform.position.x, transform.position.y, .25f);
        GravityRenderer.transform.localScale = new Vector3(size * 2 * _ratio, size * 2, 1);
        
        _camera.targetTexture = GravityTexture;
    }
}
