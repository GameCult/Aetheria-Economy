/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Serialization;
using static Unity.Mathematics.math;
using static Unity.Mathematics.noise;

/// <summary>
/// Drives the volume render.
/// </summary>
//[ExecuteInEditMode]
[RequireComponent( typeof( Camera ) ), ImageEffectAllowedInSceneView]
public class VolumeSampling : MonoBehaviour
{
    [FormerlySerializedAs("_volMaterial")] public Material VolMaterial;
    public Transform GridTransform;
    public GameSettings Settings;
    //public int DownsampleBlurMask = 2;
    private Camera _camera;
    private RenderBuffer[] _mrt;
    private RenderTexture _blurMask;
    private Texture2D _disabledBlurMask;
    private float _flowScroll;

    public RenderTexture NebulaSurfaceHeight;
    public RenderTexture NebulaPatchHeight;
    public RenderTexture NebulaPatch;
    public RenderTexture NebulaTint;

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
        if( VolMaterial == null )
        {
            Graphics.Blit(source, destination);
            return;
        }
        
        // Shader needs this matrix to generate world space rays
        VolMaterial.SetMatrix("_CamProj", (_camera.projectionMatrix * _camera.worldToCameraMatrix).inverse);
        VolMaterial.SetMatrix("_CamInvProj", (_camera.projectionMatrix * _camera.worldToCameraMatrix).inverse);
        
        // Shader needs to know the position and scale of cameras used to render input textures
        if(GridTransform != null)
            VolMaterial.SetVector("_GridTransform", new Vector4(GridTransform.position.x,GridTransform.position.z,GridTransform.localScale.x));
        
        _mrt[0] = destination.colorBuffer;
        _mrt[1] = _blurMask.colorBuffer;

        // Blit with a MRT.
        Graphics.SetRenderTarget(_mrt, source.depthBuffer);
        
        Graphics.Blit( source, VolMaterial, 0 );
        
        //Graphics.Blit(rt1, destination);
        
        if(EnableDepth)
            Shader.SetGlobalTexture("_DoFBlurTex", _blurMask);
        else
            Shader.SetGlobalTexture("_DoFBlurTex", _disabledBlurMask);
        
        Shader.SetGlobalTexture("_NebulaSurfaceHeight", NebulaSurfaceHeight);
        Shader.SetGlobalTexture("_NebulaPatchHeight", NebulaPatchHeight);
        Shader.SetGlobalTexture("_NebulaPatch", NebulaPatch);
        Shader.SetGlobalTexture("_NebulaTint", NebulaTint);
        
        Shader.SetGlobalFloat("_NebulaFillDensity", Settings.DefaultEnvironment.Nebula.FillDensity);
        Shader.SetGlobalFloat("_SafetyDistance", Settings.DefaultEnvironment.Nebula.FillDistance);
        Shader.SetGlobalFloat("_NebulaFloorDensity", Settings.DefaultEnvironment.Nebula.FloorDensity);
        Shader.SetGlobalFloat("_NebulaPatchDensity", Settings.DefaultEnvironment.Nebula.PatchDensity);
        Shader.SetGlobalFloat("_NebulaFloorOffset", Settings.DefaultEnvironment.Nebula.FloorOffset);
        Shader.SetGlobalFloat("_NebulaFloorBlend", Settings.DefaultEnvironment.Nebula.FloorBlend);
        Shader.SetGlobalFloat("_NebulaPatchBlend", Settings.DefaultEnvironment.Nebula.PatchBlend);
        // Shader.SetGlobalFloat("_TintExponent", Settings.DefaultEnvironment.);
        Shader.SetGlobalFloat("_NoiseScale", Settings.DefaultEnvironment.Noise.Scale);
        Shader.SetGlobalFloat("_NoiseExponent", Settings.DefaultEnvironment.Noise.Exponent);
        Shader.SetGlobalFloat("_NoiseAmplitude", Settings.DefaultEnvironment.Noise.Amplitude);
        Shader.SetGlobalFloat("_NoiseSpeed", Settings.DefaultEnvironment.Noise.Speed);
        Shader.SetGlobalFloat("_FlowScale", Settings.DefaultEnvironment.Flow.Scale);
        Shader.SetGlobalFloat("_FlowAmplitude", Settings.DefaultEnvironment.Flow.Amplitude);
        _flowScroll += Settings.DefaultEnvironment.Flow.ScrollSpeed * Time.deltaTime;
        Shader.SetGlobalFloat("_FlowScroll", _flowScroll);
        Shader.SetGlobalFloat("_FlowSpeed", Settings.DefaultEnvironment.Flow.Speed);
        // Shader.SetGlobalFloat("_SafetyDistance", Settings.DefaultEnvironment.);
    }
}
