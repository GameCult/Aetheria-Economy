/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using static Unity.Mathematics.math;
using static Unity.Mathematics.noise;

/// <summary>
/// Drives the volume render.
/// </summary>
//[ExecuteInEditMode]
[RequireComponent( typeof( Camera ) ), ImageEffectAllowedInSceneView]
public class VolumeSampling : MonoBehaviour
{
    // public int Octaves = 4;
    // public float Frequency = 1;
    // public float Lacunarity = 2;
    // public float Gain = .5f;
    // public int NoiseTextureCount = 16;
    // public int NoiseTextureSize = 32;
    public Material _volMaterial;
    public Transform GridTransform;
    private Camera _camera;
    // private Texture3D[] _noiseTextures;
    // private int _currentNoiseTexture;

    private void Start()
    {
        _camera = GetComponent<Camera>();
        // _noiseTextures = new Texture3D[NoiseTextureCount];
        // var samples = new Color[NoiseTextureSize * NoiseTextureSize * NoiseTextureSize];
        // for (int i = 0; i < NoiseTextureCount; i++)
        // {
        //     var iv = (float) i / NoiseTextureCount;
        //     var tex = new Texture3D(NoiseTextureSize,NoiseTextureSize,NoiseTextureSize, GraphicsFormat.R8_UNorm, TextureCreationFlags.None);
        //     tex.filterMode = FilterMode.Bilinear;
        //     for (int z = 0; z < NoiseTextureSize; z++)
        //     {
        //         var zf = (float) z / NoiseTextureSize;
        //         int zOffset = z * NoiseTextureSize * NoiseTextureSize;
        //         for (int y = 0; y < NoiseTextureSize; y++)
        //         {
        //             var yf = (float) y / NoiseTextureSize;
        //             int yOffset = y * NoiseTextureSize;
        //             for (int x = 0; x < NoiseTextureSize; x++)
        //             {
        //                 var xf = (float) x / NoiseTextureSize;
        //                 var index = x + yOffset + zOffset;
        //                 float freq = Frequency, amp = 1;
        //                 float sum = 0;
        //                 var p = float4(xf, yf, zf, iv);
        //                 for(int o = 0; o < Octaves; o++) 
        //                 {
        //                     sum += (pnoise(p * freq, 1 / freq) + 1) / 2 * amp;
        //                     freq *= Lacunarity;
        //                     amp *= Gain;
        //                 }
        //
        //                 samples[index] = Color.white * sum;
        //             }
        //         }
        //     }
        //     tex.SetPixels(samples);
        //     tex.Apply();
        //     _noiseTextures[i] = tex;
        // }
    }

    [ImageEffectOpaque]
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // check for no shader / shader compile error
        if( _volMaterial == null )
        {
            Graphics.Blit(source, destination);
            return;
        }

        // _currentNoiseTexture = (_currentNoiseTexture + 1) % NoiseTextureCount;
        //
        // _volMaterial.SetTexture("_NoiseTex", _noiseTextures[_currentNoiseTexture]);
        
        // Shader needs this matrix to generate world space rays
        _volMaterial.SetMatrix("_CamProj", (_camera.projectionMatrix * _camera.worldToCameraMatrix).inverse);
        _volMaterial.SetMatrix("_CamInvProj", (_camera.projectionMatrix * _camera.worldToCameraMatrix).inverse);
        
        // Shader needs to know the position and scale of cameras used to render input textures
        if(GridTransform != null)
            _volMaterial.SetVector("_GridTransform", new Vector4(GridTransform.position.x,GridTransform.position.z,GridTransform.localScale.x*4));
        
        Graphics.Blit( source, destination, _volMaterial, 0 );
    }
}
