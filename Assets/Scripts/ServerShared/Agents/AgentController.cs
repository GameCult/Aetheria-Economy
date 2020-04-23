using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static Unity.Mathematics.math;
using Random = Unity.Mathematics.Random;

public class AgentController
{
    public Entity Entity { get; }
    public EntityAgent EntityAgent { get; }
    public GameContext Context { get; }
    public ZoneData Zone { get; set; }

    private Locomotion _locomotion;
    private Random _random = new Random((uint) (DateTime.Now.Ticks%uint.MaxValue));
    
    public AgentController(GameContext context, ZoneData zone, Entity entity)
    {
        Entity = entity;
        Zone = zone;
        Context = context;
        EntityAgent = new EntityAgent(context, zone, entity);
        _locomotion = new Locomotion(entity);
        EntityAgent.CurrentBehavior = _locomotion;
        RandomTarget();
    }

    public void Update(float delta)
    {
        EntityAgent.Update(delta);
        var distance = length(_locomotion.Objective.Position - Entity.Position);
        Context.Log($"Agent Distance: {distance}");
        if(distance < 20)
            RandomTarget();
    }

    private void RandomTarget()
    {
        var entities = Context.ZoneEntities(Zone).ToArray();
        _locomotion.Objective = entities[_random.NextInt(entities.Length)];
    }
}
