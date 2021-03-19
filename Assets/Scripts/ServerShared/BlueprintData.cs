﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[RethinkTable("Items"), MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class BlueprintData : DatabaseEntry, INamedEntry
{
    [InspectableField, JsonProperty("name"), Key(1)]  
    public string Name;
    
    [InspectableDatabaseLink(typeof(ItemData)), JsonProperty("ingredients"), Key(2)]  
    public Dictionary<Guid, int> Ingredients = new Dictionary<Guid, int>();

    [JsonProperty("item"), Key(3)]  
    public Guid Item;

    [InspectableField, JsonProperty("productionTime"), Key(4)]
    public float ProductionTime;

    [InspectableField, JsonProperty("quality"), Key(5)]
    public float Quality;

    [InspectableField, JsonProperty("qualityExponent"), Key(6)]
    public float QualityExponent = 1;

    [InspectableField, JsonProperty("productionExponent"), Key(7)]
    public float ProductionExponent = 1;

    [InspectableField, JsonProperty("randomExponent"), Key(8)]
    public float RandomQualityExponent = 1;

    [InspectableField, JsonProperty("personalityExponent"), Key(9)]
    public float PersonalityExponent = 1;

    [InspectableField, JsonProperty("qualityFloor"), Key(10)]
    public float QualityFloor = .25f;

    [InspectableField, JsonProperty("statEffects"), Key(11)]
    public List<BlueprintStatEffect> StatEffects = new List<BlueprintStatEffect>();

    [InspectableField, JsonProperty("researchTime"), Key(12)]
    public float ResearchTime;
    
    [InspectableDatabaseLink(typeof(BlueprintData)), JsonProperty("researchDependencies"), Key(13)]
    public List<Guid> Dependencies = new List<Guid>();

    [InspectableDatabaseLink(typeof(CraftedItemData)), JsonProperty("factoryItem"), Key(14)]
    public Guid FactoryItem;

    [InspectableDatabaseLink(typeof(SimpleCommodityData)), JsonProperty("resourceRequirements"), Key(15)]  
    public Dictionary<Guid, int> ResourceRequirements = new Dictionary<Guid, int>();
    
    [IgnoreMember] public string EntryName
    {
        get => Name;
        set => Name = value;
    }
}

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class BlueprintStatEffect
{
    [InspectableField, JsonProperty("ingredient"), Key(1)]
    public Guid Ingredient;
    
    [InspectableField, JsonProperty("stat"), Key(2)]
    public StatReference StatReference = new StatReference();
}

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class StatReference
{
    [InspectableType(typeof(BehaviorData)), JsonProperty("behavior"), Key(1)]
    public string Target;
    
    [InspectableField, JsonProperty("stat"), Key(2)]
    public string Stat;
}
