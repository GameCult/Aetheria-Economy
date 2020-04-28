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
    private VelocityMatch _velocityMatch;
    private Random _random = new Random((uint) (DateTime.Now.Ticks%uint.MaxValue));
    private KeyValuePair<DatabaseEntry, Entity> _objective;

    public AgentController(GameContext context, ZoneData zone, Entity entity)
    {
        Entity = entity;
        Zone = zone;
        Context = context;
        EntityAgent = new EntityAgent(context, zone, entity);
        _locomotion = new Locomotion(entity);
        _velocityMatch = new VelocityMatch(entity);
        _velocityMatch.OnMatch += () =>
        {
            EntityAgent.CurrentBehavior = _locomotion;
            RandomTarget();
        };
        EntityAgent.CurrentBehavior = _locomotion;
        RandomTarget();
    }

    public void Update(float delta)
    {
        EntityAgent.Update(delta);
        var distance = length(_locomotion.Objective.Position - Entity.Position);
        // Context.Log(
        //     $"Agent Mode: {EntityAgent.CurrentBehavior.GetType()} " +
        //     $"Agent Distance: {distance} " +
        //     $"Turnaround Distance: {_stoppingDistance + _turnaroundDistance} " +
        //     $"Objective: {_objective.Key.ID.ToString().Substring(0, 8)}");

        if (EntityAgent.CurrentBehavior == _locomotion)
        {
            _velocityMatch.Objective = _locomotion.Objective;
            var matchDistanceTime = _velocityMatch.MatchDistanceTime;
            _locomotion.LeadTime = matchDistanceTime.y;
            if (distance < matchDistanceTime.x)
                EntityAgent.CurrentBehavior = _velocityMatch;
        }
    }

    private void RandomTarget()
    {
        var entities = Context.ZoneContents[Zone].Where(kvp=>!(kvp.Key is ShipData)).ToArray();
        _objective = entities[_random.NextInt(entities.Length)];
        _locomotion.Objective = _objective.Value;
    }
}
