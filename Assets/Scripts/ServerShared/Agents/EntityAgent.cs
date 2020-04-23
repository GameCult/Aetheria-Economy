using System.Collections;
using System.Collections.Generic;

public class EntityAgent
{
    public AgentBehavior CurrentBehavior;
    
    public Entity Entity { get; }
    public GameContext Context { get; }
    public ZoneData Zone { get; set; }
    
    public EntityAgent(GameContext context, ZoneData zone, Entity entity)
    {
        Entity = entity;
        Zone = zone;
        Context = context;
    }

    public void Update(float delta)
    {
        CurrentBehavior.Update(delta);
    }
}
