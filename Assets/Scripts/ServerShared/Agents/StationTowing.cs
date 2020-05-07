using System;
using System.Collections;
using System.Collections.Generic;
using JsonKnownTypes;
using MessagePack;
using Newtonsoft.Json;

[MessagePackObject, 
 JsonObject(MemberSerialization.OptIn), JsonConverter(typeof(JsonKnownTypesConverter<DatabaseEntry>))]
public class StationTowing : AgentTask
{
    [IgnoreMember] public override TaskType Type => TaskType.Tow;
    
    [JsonProperty("station"), Key(4)]
    private Guid _station;
    
    [JsonProperty("targetOrbit"), Key(5)]
    private Guid _targetOrbit;

    public StationTowing(Guid zone, Guid station, Guid targetOrbit)
    {
        Zone = zone;
        _station = station;
        _targetOrbit = targetOrbit;
    }
}
