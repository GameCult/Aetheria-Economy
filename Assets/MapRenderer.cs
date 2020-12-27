/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.UI;
using static Unity.Mathematics.math;
using int2 = Unity.Mathematics.int2;

public class MapRenderer : MonoBehaviour
{
    public Canvas Canvas;
    public Camera MapOverlayCamera;
    public Camera GravityCamera;
    public Camera TintCamera;
    public Image OverlayDisplay;
    public Image GravityDisplay;
    public Image TintDisplay;
    public float Scale;
    public float2 Position;

    private RectTransform _rect;
    private RenderTexture _mapTexture;
    private RenderTexture _gravityTexture;
    private RenderTexture _tintTexture;
    private int2 _size;
    private bool _init;
    
    void Start()
    {
        _rect = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        _init = true;
        MapOverlayCamera.gameObject.SetActive(true);
        GravityCamera.gameObject.SetActive(true);
        TintCamera.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        if (_mapTexture != null)
        {
            _mapTexture.Release();
            _mapTexture = null;
            _gravityTexture.Release();
            _gravityTexture = null;
            _tintTexture.Release();
            _tintTexture = null;
        }
        MapOverlayCamera.gameObject.SetActive(false);
        GravityCamera.gameObject.SetActive(false);
        TintCamera.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        var size = int2(Screen.width, Screen.height);
        if (_init || size.x != _size.x || size.y != _size.y)
        {
            _init = false;
            _size = size;
            if (_mapTexture != null)
            {
                _mapTexture.Release();
                _mapTexture = null;
                _gravityTexture.Release();
                _gravityTexture = null;
                _tintTexture.Release();
                _tintTexture = null;
            }
            var canvasCorners = new Vector3[4];
            Canvas.GetComponent<RectTransform>().GetWorldCorners(canvasCorners);
            _mapTexture = new RenderTexture(_size.x, _size.y, 0, RenderTextureFormat.Default);
            MapOverlayCamera.targetTexture = _mapTexture;
            OverlayDisplay.material.SetTexture("_DetailTex", _mapTexture);
            _gravityTexture = new RenderTexture(_size.x, _size.y, 0, RenderTextureFormat.RFloat);
            GravityCamera.targetTexture = _gravityTexture;
            GravityDisplay.material.SetTexture("_DetailTex", _gravityTexture);
            _tintTexture = new RenderTexture(_size.x / 2, _size.y / 2, 0, RenderTextureFormat.RGB111110Float);
            TintCamera.targetTexture = _tintTexture;
            TintDisplay.material.SetTexture("_DetailTex", _tintTexture);
        }

        var pos = ((Vector2) Position).Flatland(1);
        
        MapOverlayCamera.transform.position = pos;
        MapOverlayCamera.orthographicSize = _size.y * Scale * .5f;
        
        GravityCamera.transform.position = pos;
        GravityCamera.orthographicSize = _size.y * Scale * .5f;
        GravityDisplay.material.SetFloat("_Scale", Scale / 2);
        
        TintCamera.transform.position = pos;
        TintCamera.orthographicSize = _size.y * Scale * .5f;
    }
}
