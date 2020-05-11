using System;
using System.Collections;
using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;

[Union(0, typeof(TowingControllerData))]
public abstract class ControllerData : BehaviorData
{
    [InspectableField, JsonProperty("thrustSensitivity"), Key(1)]
    public float ThrustSensitivity = 1;
    
    [InspectableField, JsonProperty("thrustSpecificity"), Key(2)]
    public float ThrustSpecificity = 8;
    
    [InspectableField, JsonProperty("turningSensitivity"), Key(3)]
    public float TurningSensitivity = 1;
    
    [InspectableField, JsonProperty("tangentSensitivity"), Key(4)]
    public float TangentSensitivity = 4;
}

// public abstract class Controller : IController
// {
//     public abstract bool Available { get; }
//     public abstract TaskType TaskType { get; }
//     public Guid Zone { get; }
//     
//     private GameContext _context;
//     private Entity _entity;
//     private Guid _task;
//     private List<SimplifiedZoneData> _path;
//
//     public Controller(GameContext context, Entity entity)
//     {
//         
//     }
//     
//     public void AssignTask(Guid task, List<SimplifiedZoneData> path)
//     {
//         _task = task;
//         _path = path;
//     }
// }
