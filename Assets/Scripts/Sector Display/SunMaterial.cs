using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]
public class SunMaterial : MonoBehaviour
{
    public float SpeedMultiplier = 1;
    public float FirstOffsetDomainRotationSpeed = .01f;
    public float FirstOffsetRotationSpeed = .01f;
    public float SecondOffsetDomainRotationSpeed = .01f;
    public float SecondOffsetRotationSpeed = .01f;
    public float AlbedoRotationSpeed = .01f;
    public Vector3 LightingDirection;

    private float _firstOffsetDomainRotation;
    private float _firstOffsetRotation;
    private float _secondOffsetDomainRotation;
    private float _secondOffsetRotation;
    private float _albedoRotation;

    private Material _material;
    private Orbit _orbit;

    void OnEnable()
    {
        var mesh = GetComponent<MeshRenderer>();
        if (mesh == null)
        {
            enabled = false;
            return;
        }

        _material = mesh.material;

        _firstOffsetDomainRotation = Random.value * Mathf.PI;
        _firstOffsetRotation = Random.value * Mathf.PI;
        _secondOffsetDomainRotation = Random.value * Mathf.PI;
        _secondOffsetRotation = Random.value * Mathf.PI;
        _albedoRotation = Random.value * Mathf.PI;
    }

    private void OnWillRenderObject()
    {
        _firstOffsetDomainRotation = FirstOffsetDomainRotationSpeed * SpeedMultiplier * Time.time;
        _firstOffsetRotation = FirstOffsetRotationSpeed * SpeedMultiplier * Time.time;
        _secondOffsetDomainRotation = SecondOffsetDomainRotationSpeed * SpeedMultiplier * Time.time;
        _secondOffsetRotation = SecondOffsetRotationSpeed * SpeedMultiplier * Time.time;
        _albedoRotation = AlbedoRotationSpeed * SpeedMultiplier * Time.time;

        _material.SetVector("_LightingDirection", LightingDirection);
        _material.SetMatrix("_FirstOffsetDomainRotation", Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, Mathf.Rad2Deg * _firstOffsetDomainRotation, 0), Vector3.one));
        _material.SetMatrix("_FirstOffsetRotation", Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(Mathf.Rad2Deg * Mathf.Sin(_firstOffsetRotation), Mathf.Rad2Deg * Mathf.Cos(_firstOffsetRotation), 0), Vector3.one));
        _material.SetMatrix("_SecondOffsetDomainRotation", Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, Mathf.Rad2Deg * _secondOffsetDomainRotation, 0), Vector3.one));
        _material.SetMatrix("_SecondOffsetRotation", Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(Mathf.Rad2Deg * Mathf.Sin(_secondOffsetRotation), Mathf.Rad2Deg * Mathf.Cos(_secondOffsetRotation), 0), Vector3.one));
        _material.SetMatrix("_AlbedoRotation", Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, Mathf.Rad2Deg * _albedoRotation, 0), Vector3.one));
    }
}
