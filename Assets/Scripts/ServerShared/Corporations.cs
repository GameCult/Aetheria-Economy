using System;
using System.Collections;
using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;


[RethinkTable("Galaxy"), MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class Corporation : DatabaseEntry, INamedEntry
{
    [JsonProperty("name"), Key(1)]
    public string Name;

    [JsonProperty("parent"), Key(2)]
    public Guid Parent;
    
    [JsonProperty("tasks"), Key(3)]
    public List<Guid> Tasks = new List<Guid>();
    
    [JsonProperty("planetSurveyFloor"), Key(4)]  
    public Dictionary<Guid, float> PlanetSurveyFloor = new Dictionary<Guid, float>();

    [JsonProperty("unlockedBlueprints"), Key(5)]  
    public List<Guid> UnlockedBlueprints = new List<Guid>();

    [IgnoreMember] public string EntryName
    {
        get => Name;
        set => Name = value;
    }
}

[RethinkTable("Galaxy"), Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class MegaCorporation : DatabaseEntry, INamedEntry
{
    [InspectableField, JsonProperty("name"), Key(1)]
    public string Name;
    
    [InspectableText, JsonProperty("description"), Key(2)]
    public string Description;
    
    [InspectableTexture, JsonProperty("logo"), Key(3)]
    public string Logo;
    
    [InspectableDatabaseLink(typeof(PersonalityAttribute)), JsonProperty("personality"), Key(4)]  
    public Dictionary<Guid, float> Personality = new Dictionary<Guid, float>();
    
    [InspectableDatabaseLink(typeof(LoadoutData)), JsonProperty("initialFleet"), Key(5)]  
    public Dictionary<Guid, int> InitialFleet = new Dictionary<Guid, int>();
    
    [InspectableDatabaseLink(typeof(BlueprintData)), JsonProperty("initialTechs"), Key(6)]  
    public List<Guid> InitialTechnologies = new List<Guid>();

    [JsonProperty("parent"), Key(7)]
    public Guid HomeZone;
    
    [InspectableField, JsonProperty("placement"), Key(8)]
    public MegaPlacementType PlacementType;
    
    [IgnoreMember] public string EntryName
    {
        get => Name;
        set => Name = value;
    }
}
