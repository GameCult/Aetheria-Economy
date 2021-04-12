/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

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
            (float)_camera.scaledPixelHeight / (float)NoiseTexture.height,
            Random.value,
            Random.value
        ));
        Shader.SetGlobalInt("_FrameNumber", Time.frameCount);
    }
}
