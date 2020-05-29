using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, JsonObject(MemberSerialization.OptIn), EntityTypeRestriction(HullType.Ship), Order(-100)]
public class MiningControllerData : ControllerData
{
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new MiningController(context, this, entity, item);
    }
}

public class MiningController : ControllerBase, IBehavior, IPersistentBehavior, IInitializableBehavior
{
    public override TaskType TaskType => TaskType.Mine;
    public BehaviorData Data => _data;
    
    private MiningControllerData _data;
    private GameContext _context;
    private Entity _entity;
    private Gear _item;
    private bool _taskStarted;
    private int _asteroid = -1;
    private MiningTool _miningTool;
    private Switch _toolSwitch;
    
    public MiningController(GameContext context, MiningControllerData data, Entity entity, Gear item) : base(context, data, entity)
    {
        _context = context;
        _data = data;
        _entity = entity;
        _item = item;
    }

    public new void Initialize()
    {
        _miningTool = _entity.GetBehaviors<MiningTool>().First();
        _toolSwitch = _entity.GetSwitch(_miningTool);
        base.Initialize();
    }

    public new bool Update(float delta)
    {
        var miningTask = _context.Cache.Get<Mining>(Task);
        if (miningTask != null)
        {
            if (!_taskStarted)
            {
                NextAsteroid();
                _taskStarted = true;
            }
            else
            {
                if (!Moving)
                {
                    if (_entity.OccupiedCapacity < _entity.Capacity - 1)
                    {
                        var asteroidTransform = _context.GetAsteroidTransform(miningTask.Asteroids, _asteroid);
                        if (length(_entity.Position - asteroidTransform.xy) - asteroidTransform.w > _miningTool.Range)
                        {
                            _entity.SetMessage("Moving to target asteroid.");
                            MoveTo(() => _context.GetAsteroidTransform(miningTask.Asteroids, _asteroid).xy, 
                                () => _context.GetAsteroidVelocity(miningTask.Asteroids, _asteroid));
                            _toolSwitch.Activated = false;
                        }
                        else
                        {
                            if (_context.AsteroidExists(miningTask.Asteroids, _asteroid))
                            {
                                _miningTool.AsteroidBelt = miningTask.Asteroids;
                                _miningTool.Asteroid = _asteroid;
                                _toolSwitch.Activated = true;
                                Aim.Objective = _context.GetAsteroidTransform(miningTask.Asteroids, _asteroid).xy;
                                Aim.Update(delta);
                            }
                            else NextAsteroid();
                        }
                    }
                    else
                    {
                        _entity.SetMessage("Out of cargo space. Returning home to offload cargo.");
                        GoHome(() =>
                        {
                            var homeEntity = _context.Cache.Get<Entity>(HomeEntity);
                            if (!_entity.Cargo.ToArray().All(ii =>
                                _context.MoveCargo(_entity, homeEntity, _context.Cache.Get<ItemInstance>(ii))))
                            {
                                homeEntity.SetMessage("Colony is out of cargo space. Closing Mining Task.");
                                FinishTask();
                                _taskStarted = false;
                            }
                        });
                    }
                }
            }
        }
        return base.Update(delta);
    }

    private void NextAsteroid()
    {
        var miningTask = _context.Cache.Get<Mining>(Task);
        _asteroid = _context.NearestAsteroid(miningTask.Asteroids, _entity.Position);
        _entity.SetMessage("Selecting new asteroid.");
    }

    public PersistentBehaviorData Store()
    {
        return new MiningControllerPersistence
        {
            Task = Task
        };
    }

    public void Restore(PersistentBehaviorData data)
    {
        var miningControllerPersistence = data as MiningControllerPersistence;
        Task = miningControllerPersistence.Task;
    }
}

public class MiningControllerPersistence : PersistentBehaviorData
{
    [JsonProperty("task"), Key(0)]
    public Guid Task;
}