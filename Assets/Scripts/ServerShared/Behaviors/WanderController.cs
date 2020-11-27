using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;
using RethinkDb.Driver.Ast;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, JsonObject(MemberSerialization.OptIn), EntityTypeRestriction(HullType.Ship), Order(-100)]
public class WanderControllerData : ControllerData
{
    [InspectableField, JsonProperty("randomDockTime"), Key(6)]  
    public float RandomDockTime = 5;
    
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new WanderController(context, this, entity, item);
    }
}

public class WanderController : ControllerBase, IBehavior
{
    public WanderTarget WanderTarget;
    public override TaskType TaskType => TaskType.None;
    public BehaviorData Data => _data;
    
    private WanderControllerData _data;
    private GameContext _context;
    private Entity _entity;
    private Gear _item;
    private Guid _target;
    private float _dockTime = -1;
    
    public WanderController(GameContext context, WanderControllerData data, Entity entity, Gear item) : base(context, data, entity)
    {
        _context = context;
        _data = data;
        _entity = entity;
        _item = item;
        Task = Guid.NewGuid();
    }

    public new bool Update(float delta)
    {
        if (!Moving && _dockTime < 0)
        {
            NextTarget();
        }

        _dockTime -= delta;
        return base.Update(delta);
    }

    private void NextTarget()
    {
        if (WanderTarget == WanderTarget.Planets)
        {
            var planets = Zone.Planets.Values.ToArray();
            var randomPlanet = planets[_context.Random.NextInt(planets.Length)];
            MoveTo(() => Zone.GetOrbitPosition(randomPlanet.Orbit), () => Zone.GetOrbitVelocity(randomPlanet.Orbit));
        }
        else if (WanderTarget == WanderTarget.Orbitals)
        {
            var entities = Zone.Entities.Values.ToArray();
            var randomEntity = entities[_context.Random.NextInt(entities.Length)];
            MoveTo(randomEntity, true, () =>
            {
                _context.SetParent(_entity, randomEntity);
                _dockTime = _context.Random.NextFloat(_data.RandomDockTime);
            });
        }
    }
}

public enum WanderTarget
{
    Planets,
    Orbitals
}