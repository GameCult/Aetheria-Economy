using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundRenderer : MonoBehaviour
{
    public Transform Background;
    // public float RotationSpeed1 = 1;
    // public float RotationSpeed2 = 1;
    private Camera _camera;
    private float _ratio;
    private Material _material;
    private float _depth;

    private void Start()
    {
        _camera = GetComponent<Camera>();
        _depth = Background.position.z;
        _material = Background.GetComponent<MeshRenderer>().material;
    }

    private void OnPreRender()
    {
        _material.SetVector("ParallaxDirection", transform.position);
        _ratio = (float) Screen.height / Screen.width;
        Background.position = new Vector3(transform.position.x, transform.position.y, _depth);
        var size = _camera.orthographicSize;
        Background.localScale = new Vector3(size * 4, size * 4 * _ratio, 1);
    }
}
