﻿using UnityEngine;
using System.Runtime.InteropServices;
using System;

public class GPUTrailIndirectSample : GPUTrailIndirect
{
    public GPUTrailIndirectSampleParticle _particle;

    protected override int TrailNumMax
    {
        get
        {
            return _particle._particleNum;
        }
    }

    protected override void Awake()
    {
        base.Awake();

        _particle.Init();
    }



    protected override bool UpdateInputBuffer()
    {
        _particle.UpdateInputBuffer(_inputBuffer);
        return true;
    }

    protected override void ReleaseBuffer()
    {
        base.ReleaseBuffer();

        _particle.ReleaseBuffer();
    }
}