using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ShieldAnimation : MonoBehaviour
{
    public AnimationCurve Alpha;
    public AnimationCurve Radius;
    public AnimationCurve AlbedoGamma;
    public AnimationCurve TextureGamma;
    public AnimationCurve GradientMultiplier;
    public float Duration;

    private float _lerp;
    private Material _material;
    
    public Vector3 Direction
    {
        set { _material.SetVector("_Impact", value); }
    }

    void OnEnable()
    {
        _material = GetComponent<MeshRenderer>().material;
        _material.SetFloat("_DitherSampleOffset", Random.value);
        _lerp = 0;
    }

    void Update()
    {
        _lerp += Time.deltaTime / Duration;
        if(_lerp > 1) GetComponent<Prototype>().ReturnToPool();
        _material.SetFloat("_Alpha", Alpha.Evaluate(_lerp));
        _material.SetFloat("_Radius", Radius.Evaluate(_lerp));
        _material.SetFloat("_AlbedoGamma", AlbedoGamma.Evaluate(_lerp));
        _material.SetFloat("_TextureGamma", TextureGamma.Evaluate(_lerp));
        _material.SetFloat("_GradientMul", GradientMultiplier.Evaluate(_lerp));
    }
}
