using System;
using System.Collections;
using System.Collections.Generic;
using JsonKnownTypes;
using MessagePack;
using Newtonsoft.Json;
using UnityEngine;

[MessagePackObject, 
 Union(0, typeof(StationTowing)),
 JsonObject(MemberSerialization.OptIn), JsonConverter(typeof(JsonKnownTypesConverter<DatabaseEntry>))]
public abstract class AgentTask
{
    [IgnoreMember] public abstract AgentJob JobType { get; }
    [JsonProperty("id"), Key(0)] public int Priority;
    [JsonProperty("zone"), Key(1)] public Guid Zone;
}

public enum AgentJob
{
    Mine,
    Haul,
    Tow,
    Defend,
    Attack,
    Explore
}
