/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using static Unity.Mathematics.math;
using static Unity.Mathematics.noise;

/// <summary>
/// Drives the volume render.
/// </summary>
[ExecuteInEditMode]
[RequireComponent( typeof( Camera ) )]
public class VolumeSampling : MonoBehaviour
{
    public Transform GridTransform;
    public GameSettings Settings;
    private float _flowScroll;

    public RenderTexture NebulaSurfaceHeight;
    public RenderTexture NebulaPatchHeight;
    public RenderTexture NebulaPatch;
    public RenderTexture NebulaTint;

    void Update()
    {
        // Shader needs to know the position and scale of cameras used to render input textures
        if(GridTransform != null)
            Shader.SetGlobalVector("_GridTransform", new Vector4(GridTransform.position.x,GridTransform.position.z,GridTransform.localScale.x));

        // Volumetric sampling parameters are used by several shaders
        Shader.SetGlobalTexture("_NebulaSurfaceHeight", NebulaSurfaceHeight);
        Shader.SetGlobalTexture("_NebulaPatchHeight", NebulaPatchHeight);
        Shader.SetGlobalTexture("_NebulaPatch", NebulaPatch);
        Shader.SetGlobalTexture("_NebulaTint", NebulaTint);
        
        Shader.SetGlobalFloat("_NebulaFillDensity", Settings.DefaultEnvironment.Nebula.FillDensity);
        Shader.SetGlobalFloat("_NebulaFillDistance", Settings.DefaultEnvironment.Nebula.FillDistance);
        Shader.SetGlobalFloat("_NebulaFillExponent", Settings.DefaultEnvironment.Nebula.FillExponent);
        Shader.SetGlobalFloat("_NebulaFillOffset", Settings.DefaultEnvironment.Nebula.FillOffset);
        Shader.SetGlobalFloat("_NebulaFloorDensity", Settings.DefaultEnvironment.Nebula.FloorDensity);
        Shader.SetGlobalFloat("_NebulaPatchDensity", Settings.DefaultEnvironment.Nebula.PatchDensity);
        Shader.SetGlobalFloat("_NebulaFloorOffset", Settings.DefaultEnvironment.Nebula.FloorOffset);
        Shader.SetGlobalFloat("_NebulaFloorBlend", Settings.DefaultEnvironment.Nebula.FloorBlend);
        Shader.SetGlobalFloat("_NebulaPatchBlend", Settings.DefaultEnvironment.Nebula.PatchBlend);
        Shader.SetGlobalFloat("_NebulaLuminance", Settings.DefaultEnvironment.Nebula.Luminance);
        Shader.SetGlobalFloat("_ExtinctionCoefficient", Settings.DefaultEnvironment.Nebula.Extinction);
        Shader.SetGlobalFloat("_TintExponent", Settings.DefaultEnvironment.Nebula.TintExponent);
        Shader.SetGlobalFloat("_TintLodExponent", Settings.DefaultEnvironment.Nebula.TintLodExponent);
        Shader.SetGlobalFloat("_SafetyDistance", Settings.DefaultEnvironment.Nebula.SafetyDistance);
        Shader.SetGlobalFloat("_DynamicSkyBoost", Settings.DefaultEnvironment.Nebula.DynamicSkyBoost);
        Shader.SetGlobalFloat("_DynamicLodRange", Settings.DefaultEnvironment.Nebula.DynamicLodRange);
        Shader.SetGlobalFloat("_DynamicLodBias", Settings.DefaultEnvironment.Nebula.DynamicLodBias);
        Shader.SetGlobalFloat("_DynamicIntensity", Settings.DefaultEnvironment.Nebula.DynamicIntensity);
        
        Shader.SetGlobalFloat("_NebulaNoiseScale", Settings.DefaultEnvironment.Noise.Scale);
        Shader.SetGlobalFloat("_NebulaNoiseExponent", Settings.DefaultEnvironment.Noise.Exponent);
        Shader.SetGlobalFloat("_NebulaNoiseAmplitude", Settings.DefaultEnvironment.Noise.Amplitude);
        Shader.SetGlobalFloat("_NebulaNoiseSpeed", Settings.DefaultEnvironment.Noise.Speed);
        
        Shader.SetGlobalFloat("_FlowScale", Settings.DefaultEnvironment.Flow.Scale);
        Shader.SetGlobalFloat("_FlowAmplitude", Settings.DefaultEnvironment.Flow.Amplitude);
        _flowScroll += Settings.DefaultEnvironment.Flow.ScrollSpeed * Time.deltaTime;
        Shader.SetGlobalFloat("_FlowScroll", _flowScroll);
        Shader.SetGlobalFloat("_FlowSpeed", Settings.DefaultEnvironment.Flow.Speed);
    }
}
