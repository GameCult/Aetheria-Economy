using System;
using System.Collections;
using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;

public class HaulingTask : AgentTask
{
    public override TaskType Type => TaskType.Haul;
    
    [JsonProperty("origin"), Key(4)]
    public Entity Origin;
    
    [JsonProperty("target"), Key(5)]
    public Entity Target;
    
    [JsonProperty("itemType"), Key(6)]
    public Guid ItemType;
    
    [JsonProperty("quantity"), Key(7)]
    public int Quantity;
}
