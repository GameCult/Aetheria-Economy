/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

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

    [InspectableField, JsonProperty("hostile"), Key(5)]
    public bool PlayerHostile;
    
    [IgnoreMember] public string EntryName
    {
        get => Name;
        set => Name = value;
    }
}
