using System;
using System.Collections;
using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[RethinkTable("Items"), Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class BlueprintData : DatabaseEntry, INamedEntry
{
    [InspectableField, JsonProperty("name"), Key(1)]  
    public string Name;
    
    [InspectableDatabaseLink(typeof(ItemData)), JsonProperty("ingredients"), Key(2)]  
    public Dictionary<Guid, int> Ingredients = new Dictionary<Guid, int>();

    [InspectableDatabaseLink(typeof(CraftedItemData)), JsonProperty("item"), Key(3)]  
    public Guid Item;

    [InspectableField, JsonProperty("quantity"), Key(4)]
    public int Quantity;

    [InspectableField, JsonProperty("productionTime"), Key(5)]
    public float ProductionTime;

    [InspectableField, JsonProperty("quality"), Key(6)]
    public float Quality;

    [InspectableField, JsonProperty("statEffects"), Key(7)]
    public List<BlueprintStatEffect> StatEffects = new List<BlueprintStatEffect>();

    [InspectableField, JsonProperty("researchTime"), Key(8)]
    public float ResearchTime;
    
    [InspectableDatabaseLink(typeof(BlueprintData)), JsonProperty("researchDependencies"), Key(9)]  
    public List<Guid> Dependencies = new List<Guid>();
    
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
    [InspectableType(typeof(IBehaviorData)), JsonProperty("behavior"), Key(1)]  
    public string Behavior;
    
    [InspectableField, JsonProperty("stat"), Key(2)]  
    public string Stat;
}
