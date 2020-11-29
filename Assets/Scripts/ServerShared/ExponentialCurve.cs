using System;
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
public class GravitySettings
{
    [JsonProperty("gravityDepth"), Key(0)]
    public ExponentialCurve GravityDepth;
    
    [JsonProperty("gravityRadius"), Key(1)]
    public ExponentialCurve GravityRadius;
    
    [JsonProperty("waveDepth"), Key(2)]
    public ExponentialCurve WaveDepth;
    
    [JsonProperty("waveRadius"), Key(3)]
    public ExponentialCurve WaveRadius;
    
    [JsonProperty("waveFrequency"), Key(4)]
    public ExponentialCurve WaveFrequency;
    
    [JsonProperty("waveSpeed"), Key(5)]
    public ExponentialCurve WaveSpeed;
    
    [JsonProperty("gravityStrength"), Key(6)]
    public float GravityStrength;
}
