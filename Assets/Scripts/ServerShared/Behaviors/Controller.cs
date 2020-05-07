using System.Collections;
using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;

[Union(0, typeof(TowingControllerData))]
public abstract class ControllerData : BehaviorData
{
    [InspectableField, JsonProperty("thrustSensitivity"), Key(0)]
    public float ThrustSensitivity = 1;
    
    [InspectableField, JsonProperty("thrustSpecificity"), Key(1)]
    public float ThrustSpecificity = 8;
    
    [InspectableField, JsonProperty("turningSensitivity"), Key(2)]
    public float TurningSensitivity = 1;
    
    [InspectableField, JsonProperty("tangentSensitivity"), Key(3)]
    public float TangentSensitivity = 4;
}

// public abstract class Controller
// {
//     
// }
