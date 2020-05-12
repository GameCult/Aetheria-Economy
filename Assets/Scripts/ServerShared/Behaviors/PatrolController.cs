using System;
using System.Collections;
using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, JsonObject(MemberSerialization.OptIn), EntityTypeRestriction(HullType.Ship)]
public class PatrolControllerData : ControllerData
{
    [InspectableField, JsonProperty("targetDistance"), Key(5)]  
    public float TargetDistance = 10;
    
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new PatrolController(context, this, entity, item);
    }
}

[UpdateOrder(-100)]
public class PatrolController : IBehavior, IController, IInitializableBehavior
{
    public TaskType TaskType => TaskType.Tow;
    public bool Available => false;
    public Guid Zone => _entity.Zone;
    public BehaviorData Data => _data;
    
    private PatrolControllerData _data;
    private GameContext _context;
    private Entity _entity;
    private Gear _item;
    private Guid _towingTask;
    private Locomotion _locomotion;
    private Guid _targetOrbit;
    
    public PatrolController(GameContext context, PatrolControllerData data, Entity entity, Gear item)
    {
        _context = context;
        _data = data;
        _entity = entity;
        _item = item;
    }
    
    public void Initialize()
    {
        _locomotion = new Locomotion(_context, _entity, _data);
        RandomTarget();
    }

    public bool Update(float delta)
    {
        _locomotion.Objective = _context.GetOrbitPosition(_targetOrbit);
        _locomotion.Update(delta);
        
        if(length(_entity.Position - _locomotion.Objective) < _data.TargetDistance)
            RandomTarget();
        
        return true;
    }

    public void AssignTask(Guid task, List<SimplifiedZoneData> path)
    {
        throw new NotImplementedException();
    }
    
    private void RandomTarget()
    {
        var entities = _context.ZonePlanets[_entity.Zone];
        _targetOrbit = _context.Cache.Get<PlanetData>(entities[_context.Random.NextInt(entities.Length)]).Orbit;
    }
}
