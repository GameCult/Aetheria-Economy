using System;
using System.Collections;
using System.Collections.Generic;
using JsonKnownTypes;
using MessagePack;
using Newtonsoft.Json;
using UnityEngine;

[MessagePackObject, 
 JsonObject(MemberSerialization.OptIn), JsonConverter(typeof(JsonKnownTypesConverter<DatabaseEntry>))]
public class StationTowing : AgentTask
{
    [IgnoreMember] public override AgentJob JobType => AgentJob.Tow;
    
    private Guid _entity;
    private Guid _targetOrbit;

    public StationTowing(Guid zone, Guid entity, Guid targetOrbit)
    {
        Zone = zone;
        _entity = entity;
        _targetOrbit = targetOrbit;
    }
}
