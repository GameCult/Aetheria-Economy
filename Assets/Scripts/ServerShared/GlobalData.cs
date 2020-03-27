using System.Collections;
using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;

[RethinkTable("Galaxy")]
[MessagePackObject]
[JsonObject(MemberSerialization.OptIn)]
public class GlobalData : DatabaseEntry
{
    [InspectableField] [JsonProperty("targetPersistenceDuration")] [Key(1)]
    public float TargetPersistenceDuration = 3;
    
    [InspectableField] [JsonProperty("heatRadiationPower")] [Key(2)]
    public float HeatRadiationPower = 1;
    
    [InspectableField] [JsonProperty("heatRadiationMultiplier")] [Key(3)]
    public float HeatRadiationMultiplier = 1;

    [InspectableField] [JsonProperty("radiusPower")] [Key(4)]
    public float RadiusPower = 1.75f;

    [InspectableField] [JsonProperty("massFloor")] [Key(5)]
    public float MassFloor = 1;

    [InspectableField] [JsonProperty("sunMass")] [Key(6)]
    public float SunMass = 10000;

    [InspectableField] [JsonProperty("gasGiantMass")] [Key(7)]
    public float GasGiantMass = 2000;
    
    [InspectableField] [JsonProperty("dockingDistance")] [Key(8)]
    public float DockingDistance = 10;
    
    [InspectableField] [JsonProperty("satelliteCreationMassFloor")] [Key(9)]
    public float SatelliteCreationMassFloor = 100;
    
    [InspectableField] [JsonProperty("satelliteCreationProbability")] [Key(10)]
    public float SatelliteCreationProbability = .25f;
    
    [InspectableField] [JsonProperty("binaryCreationProbability")] [Key(11)]
    public float BinaryCreationProbability = .25f;
    
    [InspectableField] [JsonProperty("rosetteProbability")] [Key(12)]
    public float RosetteProbability = .25f;
}