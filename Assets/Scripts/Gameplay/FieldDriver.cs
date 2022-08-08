using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Mathematics;
using UnityEngine.Animations;
using static Unity.Mathematics.math;

public class FieldDriver : MonoBehaviour
{
    [Inspectable]
    public float TestMagnitude;
    public float2 Throttle;
    [Inspectable]
    public float ThrottleDecay = 1;
    [Inspectable]
    public float FlowSpeed;
    [Inspectable]
    public float FlowSpeedThrottleExponent;
    [Inspectable]
    public float FlowMagnitude;
    [Inspectable]
    public float FlowMagnitudeThrottleExponent;
    [Inspectable]
    public float FlowOpacityThrottleExponent;
    public int MaxHits;
    [Inspectable]
    public ExponentialCurve MagnitudeTimeScaling;

    [Inspectable]
    public float MeleeRange;
    [Inspectable]
    public float MeleeRangeExponent;
    [Inspectable]
    public float MeleeFlatness;
    [Inspectable]
    public float MeleeShaping;
    [Inspectable]
    public float MeleeDuration;
    [Inspectable]
    public float MeleeAngle;
    [Inspectable]
    public float MeleeAngleExponent;
    [Inspectable]
    public Axis RefractionAxis = Axis.X;
    
    private Material _field;
    private ComputeBuffer _hitBuffer;
    private List<FieldHit> _hits = new List<FieldHit>();
    private float _pushWaveOffset;
    private double2 _throttle;
    private float _meleeTime;
    private bool _meleeActive;

    private struct FieldHit
    {
        public float3 Position;
        public float3 Direction;
        public float Magnitude;
        public float Time;
    }
    
    void Start()
    {
        _field = GetComponent<MeshRenderer>().material;
        var clickableCollider = GetComponent<ClickableCollider>();
        if(clickableCollider!=null)
        {
            clickableCollider.OnClick += (_, _, ray, hit) =>
            {
                AddHit(hit.point, ray.direction, TestMagnitude);
            };
        }
        _hitBuffer = new ComputeBuffer(MaxHits, 32);
    }

    public void AddHit(float3 position, float3 direction, float magnitude)
    {
        if (_hits.Count >= MaxHits) return;
        var hit = new FieldHit
        {
            Position = normalize(transform.InverseTransformPoint(position)),
            Direction = normalize(transform.rotation * direction),
            Magnitude = magnitude,
            Time = 0
        };
        Debug.Log($"Received hit: Position={hit.Position}, Direction={hit.Direction}");
        _hits.Add(hit);
    }

    public int HitCount => _hits.Count;

    public void Melee()
    {
        _meleeActive = true;
        _meleeTime = 0;
    }

    void Update()
    {
        for (int i = 0; i < _hits.Count; i++)
        {
            var hit = _hits[i];
            hit.Time += Time.deltaTime / MagnitudeTimeScaling.Evaluate(_hits[i].Magnitude);
            _hits[i] = hit;
            if (hit.Time > 1)
            {
                _hits.RemoveAt(i);
                i--;
            }
        }

        if (_meleeActive)
        {
            _meleeTime += Time.deltaTime / MeleeDuration;
            if (_meleeTime > 1)
            {
                _meleeActive = false;
            }
        }

        _throttle = AetheriaMath.Damp(_throttle, Throttle, ThrottleDecay, Time.deltaTime);
        var throttleDir = (float2) normalize(_throttle);

        var throttleMag = min(length(_throttle), 1);

        _pushWaveOffset = frac(_pushWaveOffset + Time.deltaTime * FlowSpeed * (float) pow(throttleMag, FlowSpeedThrottleExponent));

        var refractionRotation = Matrix4x4.Rotate(transform.rotation);
        if (RefractionAxis == Axis.Y) refractionRotation = Matrix4x4.Rotate(Quaternion.Euler(90, 0, 0)) * refractionRotation;
        if (RefractionAxis == Axis.X) refractionRotation = Matrix4x4.Rotate(Quaternion.Euler(0, 90, 0)) * refractionRotation;
        _hitBuffer.SetData(_hits);
        _field.SetBuffer("_Hits", _hitBuffer);
        _field.SetInt("_HitCount", _hits.Count);
        _field.SetFloat("_PushWaveMagnitude", FlowMagnitude * (float) pow(throttleMag, FlowMagnitudeThrottleExponent));
        _field.SetFloat("_PushOpacity", (float) pow(throttleMag, FlowOpacityThrottleExponent));
        _field.SetFloat("_PushWaveOffset", _pushWaveOffset);
        _field.SetVector("_PushDirection", new Vector4(throttleDir.x,0,throttleDir.y));
        _field.SetVector("_InverseScale", new Vector4(1/transform.localScale.x,1/transform.localScale.y,1/transform.localScale.z));
        _field.SetMatrix("_ReflRotate", refractionRotation);
        _field.SetVector("_MeleeDirection", Quaternion.AngleAxis(sign(_meleeTime*2-1) * MeleeAngle * pow(abs(_meleeTime * 2 - 1), MeleeAngleExponent), Vector3.up) * -Vector3.forward);
        _field.SetFloat("_MeleeDisplacement", pow(1 - 2 * abs(_meleeTime - .5f), MeleeRangeExponent) * MeleeRange);
        _field.SetFloat("_MeleeShape", MeleeShaping);
        _field.SetFloat("_MeleeFlattening", MeleeFlatness);
    }
}
