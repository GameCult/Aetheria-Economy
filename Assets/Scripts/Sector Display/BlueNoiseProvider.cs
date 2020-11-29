using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[ExecuteInEditMode]
public class BlueNoiseProvider : MonoBehaviour
{
    public Texture2D NoiseTexture;
    
    private Camera _camera;
    
    private void Start()
    {
        _camera = Camera.main;
        
        Shader.SetGlobalTexture("_DitheringTex", NoiseTexture);
    }

    private void Update()
    {
        Shader.SetGlobalVector("_DitheringCoords", new Vector4(
            (float)_camera.scaledPixelWidth / (float)NoiseTexture.width,
            (float)_camera.scaledPixelWidth / (float)NoiseTexture.height,
            Random.value,
            Random.value
        ));
    }
}
