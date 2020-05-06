using System;
using System.Collections;
using System.Collections.Generic;
using JsonKnownTypes;
using MessagePack;
using Newtonsoft.Json;

[MessagePackObject, 
 JsonObject(MemberSerialization.OptIn), JsonConverter(typeof(JsonKnownTypesConverter<Entity>))]
public class EntityAgent
{
    [IgnoreMember] public AgentBehavior CurrentBehavior;
    [IgnoreMember] public Guid Entity;
    [IgnoreMember] public GameContext Context;
    [JsonProperty("homeZone"), Key(0)] public Guid HomeZone;
    [JsonProperty("homeColony"), Key(0)] public Guid HomeColony;
    
    public EntityAgent(GameContext context, Guid entity, Guid homeZone, Guid homeColony)
    {
        Entity = entity;
        HomeZone = homeZone;
        Context = context;
    }

    public void Update(float delta)
    {
        CurrentBehavior?.Update(delta);
    }
}
