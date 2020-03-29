using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundRenderer : MonoBehaviour
{
    public Transform Background;
    public Transform Background2;
    // public float RotationSpeed1 = 1;
    // public float RotationSpeed2 = 1;
    private Camera _camera;
    private float _ratio;
    private Material _material;
    private Material _material2;
    private float _depth;

    private void Start()
    {
        _camera = GetComponent<Camera>();
        _depth = Background.position.z;
        _material = Background.GetComponent<MeshRenderer>().material;
        _material2 = Background2.GetComponent<MeshRenderer>().material;
    }

    private void OnPreRender()
    {
        _material.SetVector("ParallaxDirection", transform.position);
        _material2.SetVector("ParallaxDirection", transform.position);
        _ratio = (float) Screen.width / Screen.height;
        Background2.position = Background.position = new Vector3(transform.position.x, transform.position.y, _depth);
        var size = _camera.orthographicSize;
        Background2.localScale = Background.localScale = new Vector3(size * 2 * _ratio, size * 2, 1);
    }
}
