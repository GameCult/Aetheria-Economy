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
    private float _stoppingTime;
    private float _stoppingDistance;
    private float _turnaroundDistance;
    private float _turnaroundTime;
    private IAnalogBehavior _thrust;
    private IAnalogBehavior _turning;
    private KeyValuePair<DatabaseEntry, Entity> _objective;
    private float _velocityLimit;

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
        _thrust = entity.Axes.Keys.FirstOrDefault(x => x is Thruster);
        _turning = entity.Axes.Keys.FirstOrDefault(x => x is Turning);
        var velocityLimitData = entity.GetBehaviorData<VelocityLimitData>().FirstOrDefault();
        _velocityLimit = Context.Evaluate(velocityLimitData.TopSpeed, entity.Hull, entity);
        _stoppingTime = _velocityLimit / Context.Evaluate((_thrust.Data as ThrusterData).Thrust, _thrust.Item, Entity) * Entity.Mass;
        _stoppingDistance = _stoppingTime * _velocityLimit / 2;
        _turnaroundTime = PI / Context.Evaluate((_turning.Data as TurningData).Torque, _turning.Item, entity) * entity.Mass;
        _turnaroundDistance = _turnaroundTime * _velocityLimit;
        _locomotion.LeadTime = _stoppingTime;
    }

    public void Update(float delta)
    {
        EntityAgent.Update(delta);
        var distance = length(_locomotion.Objective.Position - Entity.Position);
        Context.Log(
            $"Agent Mode: {EntityAgent.CurrentBehavior.GetType()} " +
            $"Agent Distance: {distance} " +
            $"Turnaround Distance: {_stoppingDistance + _turnaroundDistance} " +
            $"Objective: {_objective.Key.ID.ToString().Substring(0, 8)}");

        if (EntityAgent.CurrentBehavior == _locomotion)
        {
            var velocity = length(Entity.Velocity);
            _stoppingTime = velocity / Context.Evaluate((_thrust.Data as ThrusterData).Thrust, _thrust.Item, Entity) * Entity.Mass;
            _stoppingDistance = _stoppingTime * velocity / 2;
            if (distance < _stoppingDistance + _turnaroundDistance)
                EntityAgent.CurrentBehavior = _velocityMatch;
            _velocityMatch.Objective = _locomotion.Objective;
        }
    }

    private void RandomTarget()
    {
        var entities = Context.ZoneContents[Zone].ToArray();
        _objective = entities[_random.NextInt(entities.Length)];
        _locomotion.Objective = _objective.Value;
    }
}
