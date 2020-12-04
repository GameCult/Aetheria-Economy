﻿using System;
using System.Collections;
using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[Serializable, MessagePackObject, JsonObject]
public class ExponentialCurve
{
    [JsonProperty("exponent"), Key(0)]
    public float Exponent;
    
    [JsonProperty("multiplier"), Key(1)]
    public float Multiplier;
    
    [JsonProperty("constant"), Key(2)]
    public float Constant;
    
    public float Evaluate(float value) => Multiplier * pow(value, Exponent) + Constant;
}

[Serializable, MessagePackObject, JsonObject]
public class ExponentialLerp
{
    [JsonProperty("exponent"), Key(0)]
    public float Exponent;
    
    [JsonProperty("max"), Key(1)]
    public float Minimum;
    
    [JsonProperty("min"), Key(2)]
    public float Maximum;
    
    public float Evaluate(float value) => Minimum + pow(saturate(value), Exponent) * (Maximum - Minimum);
}