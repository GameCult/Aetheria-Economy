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
    public Material _volMaterial;
    public Transform GridTransform;
    //public int DownsampleBlurMask = 2;
    private Camera _camera;
    private RenderBuffer[] _mrt;
    private RenderTexture _blurMask;
    private Texture2D _disabledBlurMask;

    public bool EnableDepth { get; set; } = true;

    private void Start()
    {
        //Debug.Log($"Supported MRT count: {SystemInfo.supportedRenderTargetCount}");
        _camera = GetComponent<Camera>();
        _mrt = new RenderBuffer[2];
        _blurMask = new RenderTexture(Screen.width, Screen.height, 0, GraphicsFormat.R8_UNorm);
        _disabledBlurMask = Color.black.ToTexture();
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
        
        // Shader needs this matrix to generate world space rays
        _volMaterial.SetMatrix("_CamProj", (_camera.projectionMatrix * _camera.worldToCameraMatrix).inverse);
        _volMaterial.SetMatrix("_CamInvProj", (_camera.projectionMatrix * _camera.worldToCameraMatrix).inverse);
        
        // Shader needs to know the position and scale of cameras used to render input textures
        if(GridTransform != null)
            _volMaterial.SetVector("_GridTransform", new Vector4(GridTransform.position.x,GridTransform.position.z,GridTransform.localScale.x));
        
        _mrt[0] = destination.colorBuffer;
        _mrt[1] = _blurMask.colorBuffer;

        // Blit with a MRT.
        Graphics.SetRenderTarget(_mrt, source.depthBuffer);
        
        Graphics.Blit( source, _volMaterial, 0 );
        
        //Graphics.Blit(rt1, destination);
        
        if(EnableDepth)
            Shader.SetGlobalTexture("_DoFBlurTex", _blurMask);
        else
            Shader.SetGlobalTexture("_DoFBlurTex", _disabledBlurMask);
    }
}
