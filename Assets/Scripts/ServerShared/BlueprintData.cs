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

    [InspectableField, JsonProperty("quantity"), Key(4)]
    public int Quantity;

    [InspectableField, JsonProperty("productionTime"), Key(5)]
    public float ProductionTime;

    [InspectableField, JsonProperty("quality"), Key(6)]
    public float Quality;

    [InspectableField, JsonProperty("qualityExponent"), Key(7)]
    public float QualityExponent = 1;

    [InspectableField, JsonProperty("productionExponent"), Key(8)]
    public float ProductionExponent = 1;

    [InspectableField, JsonProperty("randomExponent"), Key(9)]
    public float RandomExponent = 1;

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
    
    [InspectableDatabaseLink(typeof(PersonalityAttribute)), JsonProperty("productionProfile"), Key(16)]  
    public Dictionary<Guid, float> ProductionProfile = new Dictionary<Guid, float>();
    
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
