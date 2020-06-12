using System;
using System.Collections;
using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;
using UnityEngine;

public class HaulingTask : AgentTask
{
    public override TaskType Type => TaskType.Haul;
    
    [JsonProperty("origin"), Key(4)]
    public Guid Origin;
    
    [JsonProperty("target"), Key(5)]
    public Guid Target;
    
    [JsonProperty("itemType"), Key(6)]
    public Guid ItemType;
    
    [JsonProperty("quantity"), Key(7)]
    public int Quantity;
}
