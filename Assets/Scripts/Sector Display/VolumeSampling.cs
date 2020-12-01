using System;
using UnityEngine;

/// <summary>
/// Drives the volume render.
/// </summary>
//[ExecuteInEditMode]
[RequireComponent( typeof( Camera ) ), ImageEffectAllowedInSceneView]
public class VolumeSampling : MonoBehaviour
{
    public Material _volMaterial;
    public Transform GridTransform;
    private Camera _camera;

    private void Start()
    {
        _camera = GetComponent<Camera>();
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
            _volMaterial.SetVector("_GridTransform", new Vector4(GridTransform.position.x,GridTransform.position.z,GridTransform.localScale.x*4));
        
        Graphics.Blit( source, destination, _volMaterial, 0 );
    }
}
