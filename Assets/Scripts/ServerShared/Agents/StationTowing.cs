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
    public Guid Station;
    
    [JsonProperty("orbitParent"), Key(5)]
    public Guid OrbitParent;
    
    [JsonProperty("orbitDistance"), Key(6)]
    public float OrbitDistance;

    // public StationTowing(Guid zone, Guid station, Guid orbitParent, float orbitDistance)
    // {
    //     Zone = zone;
    //     Station = station;
    //     OrbitParent = orbitParent;
    //     OrbitDistance = orbitDistance;
    // }
}
