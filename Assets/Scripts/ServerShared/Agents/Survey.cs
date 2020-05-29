using System;
using System.Collections;
using System.Collections.Generic;
using JsonKnownTypes;
using MessagePack;
using Newtonsoft.Json;

[MessagePackObject, 
 JsonObject(MemberSerialization.OptIn), JsonConverter(typeof(JsonKnownTypesConverter<DatabaseEntry>))]
public class Survey : AgentTask
{
    [IgnoreMember] public override TaskType Type => TaskType.Explore;
    
    [JsonProperty("station"), Key(4)]
    public List<Guid> Planets;
}