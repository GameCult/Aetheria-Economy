using System;
using System.Collections;
using System.Collections.Generic;
using JsonKnownTypes;
using MessagePack;
using Newtonsoft.Json;

[MessagePackObject, 
 JsonObject(MemberSerialization.OptIn), JsonConverter(typeof(JsonKnownTypesConverter<DatabaseEntry>))]
public class Mining : AgentTask
{
    [IgnoreMember] public override TaskType Type => TaskType.Mine;
    
    [JsonProperty("asteroids"), Key(4)]
    public Guid Asteroids;

    // public StationTowing(Guid zone, Guid station, Guid orbitParent, float orbitDistance)
    // {
    //     Zone = zone;
    //     Station = station;
    //     OrbitParent = orbitParent;
    //     OrbitDistance = orbitDistance;
    // }
}