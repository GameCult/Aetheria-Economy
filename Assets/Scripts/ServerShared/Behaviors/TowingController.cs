using System;
using System.Collections;
using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;

[MessagePackObject, JsonObject(MemberSerialization.OptIn), EntityTypeRestriction(HullType.Ship)]
public class TowingControllerData : ControllerData
{
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new TowingController(context, this, entity, item);
    }
}

public class TowingController : IBehavior, IPersistentBehavior, IController
{
    public TaskType JobType => TaskType.Tow;
    public bool Available => _towingTask != Guid.Empty;
    public BehaviorData Data => _data;
    
    private TowingControllerData _data;
    private GameContext _context;
    private Entity _entity;
    private Gear _item;
    private Guid _towingTask;
    
    public TowingController(GameContext context, TowingControllerData data, Entity entity, Gear item)
    {
        _context = context;
        _data = data;
        _entity = entity;
        _item = item;
    }
    
    public void Initialize()
    {
        
    }

    public void Update(float delta)
    {
    }

    public PersistentBehaviorData Store()
    {
        return new TowingControllerPersistence
        {
            TowingTask = _towingTask
        };
    }

    public void Restore(PersistentBehaviorData data)
    {
        var towingControllerPersistence = data as TowingControllerPersistence;
        _towingTask = towingControllerPersistence.TowingTask;
    }

    public void AssignTask(Guid task)
    {
        _towingTask = task;
    }
}

public class TowingControllerPersistence : PersistentBehaviorData
{
    [JsonProperty("towingTask"), Key(0)]
    public Guid TowingTask;
}
