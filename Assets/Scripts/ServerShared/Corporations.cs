/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;


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
    
    [InspectableField, JsonProperty("shortName"), Key(2)]
    public string ShortName;
    
    [InspectableText, JsonProperty("description"), Key(3)]
    public string Description;
    
    [InspectableTexture, JsonProperty("logo"), Key(4)]
    public string Logo;
    
    [InspectableDatabaseLink(typeof(PersonalityAttribute)), JsonProperty("personality"), Key(5)]  
    public Dictionary<Guid, float> Personality = new Dictionary<Guid, float>();

    [InspectableField, JsonProperty("hostile"), Key(6)]
    public bool PlayerHostile;

    [InspectableColor, JsonProperty("primaryColor"), Key(7)]
    public float3 PrimaryColor;
    
    [InspectableColor, JsonProperty("secondaryColor"), Key(8)]
    public float3 SecondaryColor;

    [InspectableDatabaseLink(typeof(NameFile)), JsonProperty("nameFile"), Key(9)]  
    public Guid GeonameFile;

    [InspectableDatabaseLink(typeof(HullData)), JsonProperty("bossHull"), Key(10)]  
    public Guid BossHull;

    [InspectableField, JsonProperty("influence"), Key(11)]
    public int InfluenceDistance = 4;
    
    [IgnoreMember] public string EntryName
    {
        get => Name;
        set => Name = value;
    }
}

[Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class NameFile : DatabaseEntry
{
    [JsonProperty("names"), Key(1)]
    public string[] Names;
}
