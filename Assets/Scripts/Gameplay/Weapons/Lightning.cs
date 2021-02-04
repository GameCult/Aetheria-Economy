using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using static Unity.Mathematics.noise;
using Random = UnityEngine.Random;

public class Lightning : MonoBehaviour
{
    public LineRenderer Trunk;
    public Prototype BranchPrototype;
    public float HitRadius;
    public float Duration;
    public AnimationCurve IntensityCurve;
    public int BranchCount;
    public float BranchDistance;
    public int PointCount;
    public int BranchPointCount;
    public int MorphSpeed;
    
    public int Octaves;
    public float Frequency;
    public float Offset;
    public float Amplitude;
    public float Lacunarity;
    public float Gain;
    
    public float Damage { get; set; }
    public float Penetration { get; set; }
    public float Spread { get; set; }
    public DamageType DamageType { get; set; }
    public EntityInstance Source { get; set; }
    public float Range { get; set; }
    public EntityInstance Target { get; set; }
    public Transform Barrel { get; set; }

    private float _startTime;
    private LineRenderer[] _branches;
    private Vector3[] _points;
    private Vector3[][] _branchPoints;
    private int[] _branchStarts;
    private bool _colliderHit;
    private Vector3 _colliderLocalPosition;
    private Transform _colliderTransform;
    private Vector3 _endpoint;
    private float _offset;

    private void Awake()
    {
        _points = new Vector3[PointCount];
        _branchPoints = new Vector3[BranchCount][];
        _branchStarts = new int[BranchCount];
        _branches = new LineRenderer[BranchCount];
        for (int i = 0; i < BranchCount; i++)
        {
            _branchPoints[i] = new Vector3[BranchPointCount];
            _branches[i] = BranchPrototype.Instantiate<LineRenderer>();
        }
    }

    public void Fire()
    {
        _offset = Random.value * 1000;
        _startTime = Time.time;
        Trunk.widthMultiplier = 0;
        foreach(var line in _branches)
            line.widthMultiplier = 0;

        for (int i = 0; i < BranchCount; i++)
        {
            _branchStarts[i] = Random.Range(0, PointCount);
            var lerp = (float) _branchStarts[i] / PointCount;
            _branches[i].startColor = Trunk.colorGradient.Evaluate(lerp);
        }

        _colliderHit = false;
        var hits = Physics.SphereCastAll(Barrel.position, HitRadius, Barrel.forward, Range, 1);
        foreach (var hit in hits)
        {
            var hull = hit.collider.GetComponent<HullCollider>();
            if (hull)
            {
                if (hull.Entity == Source.Entity) continue;
                hull.SendHit(Damage, Penetration, Spread, DamageType, Source.Entity, hit, Barrel.forward);
            }

            _colliderHit = true;
            _colliderLocalPosition = hit.collider.transform.InverseTransformPoint(hit.point);
            _colliderTransform = hit.collider.transform;
            break;
        }
    }

    private void Update()
    {
        if (Barrel == null) return;
        var time = (Time.time - _startTime) / Duration;
        if (time > 1)
        {
            GetComponent<Prototype>().ReturnToPool();
            return;
        }
        Trunk.widthMultiplier = IntensityCurve.Evaluate(time);

        if (_colliderHit)
        {
            if(_colliderTransform) _endpoint = _colliderTransform.TransformPoint(_colliderLocalPosition);
        }
        else _endpoint = Barrel.position + Barrel.forward * Range + (Vector3) NoiseFbm.fBm3(Time.time, Octaves, Frequency, Offset, Amplitude, Lacunarity, Gain);
        var startPoint = Barrel.position;
        var dist = (_endpoint - startPoint).magnitude;
        var startOffset = -NoiseFbm.fBm3(float2(0+_offset, Time.time*MorphSpeed), Octaves, Frequency, Offset, Amplitude, Lacunarity, Gain);
        var endOffset = -NoiseFbm.fBm3(float2(dist+_offset, Time.time*MorphSpeed), Octaves, Frequency, Offset, Amplitude, Lacunarity, Gain);
        for (int i = 0; i < PointCount; i++)
        {
            var l = (float) i / (PointCount - 1);
            var sampleDist = dist * l;
            var pos = lerp(startPoint, _endpoint, l) + 
                      NoiseFbm.fBm3(float2(sampleDist+_offset, Time.time*MorphSpeed), Octaves, Frequency, Offset, Amplitude, Lacunarity, Gain);
            if (l < .5f) pos += startOffset * (1 - l * 2);
            else pos += endOffset * (2 * (l - .5f));
            _points[i] = pos;
        }
        Trunk.SetPositions(_points);

        for (int i = 0; i < BranchCount; i++)
        {
            _branches[i].widthMultiplier = Trunk.widthMultiplier * Trunk.widthCurve.Evaluate((float) _branchStarts[i] / PointCount);
            var branchStartPoint = _points[_branchStarts[i]];
            var branchEndPoint = branchStartPoint + Random.onUnitSphere * BranchDistance;
            var branchstartOffset = -NoiseFbm.fBm3(float2(_branchStarts[i]*10, Time.time*MorphSpeed), Octaves, Frequency, Offset, Amplitude, Lacunarity, Gain);
            for (int j = 0; j < BranchPointCount; j++)
            {
                var l = (float) j / (BranchPointCount - 1);
                var sampleDist = BranchDistance * l + _branchStarts[i]*10;
                var pos = lerp(branchStartPoint, branchEndPoint, l) +
                    NoiseFbm.fBm3(float2(sampleDist * 2, Time.time * MorphSpeed), Octaves, Frequency, Offset, Amplitude, Lacunarity, Gain);
                if (l < .5f) pos += branchstartOffset * (1 - l * 2);
                _branchPoints[i][j] = pos;
            }
            _branches[i].SetPositions(_branchPoints[i]);
        }
    }
}
