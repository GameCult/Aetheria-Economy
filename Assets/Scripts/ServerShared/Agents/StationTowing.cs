using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StationTowing : AgentTask
{
    public override AgentJob JobType => AgentJob.Tow;
    
    private Guid _entity;
    private Guid _targetOrbit;

    public StationTowing(Guid zone, Guid entity, Guid targetOrbit)
    {
        Zone = zone;
        _entity = entity;
        _targetOrbit = targetOrbit;
    }
}
