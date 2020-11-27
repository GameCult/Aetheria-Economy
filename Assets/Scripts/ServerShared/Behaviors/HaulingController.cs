using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, JsonObject(MemberSerialization.OptIn), EntityTypeRestriction(HullType.Ship), Order(-100)]
public class HaulingControllerData : ControllerData
{
    [InspectableField, JsonProperty("targetDistance"), Key(6)]  
    public float DockTime = 2;
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new HaulingController(context, this, entity, item);
    }
}

public class HaulingController : ControllerBase, IBehavior, IPersistentBehavior
{
    public override TaskType TaskType => TaskType.Haul;
    public BehaviorData Data => _data;
    
    private HaulingControllerData _data;
    private GameContext _context;
    private Entity _entity;
    private Gear _item;
    private bool _taskStarted;
    private int _itemsDelivered;
    private SimpleCommodity _simpleCommodityDelivery;
    private List<CraftedItemInstance> _craftedItemDelivery = new List<CraftedItemInstance>();
    
    public HaulingController(GameContext context, HaulingControllerData data, Entity entity, Gear item) : base(context, data, entity)
    {
        _context = context;
        _data = data;
        _entity = entity;
        _item = item;
    }

    public new bool Update(float delta)
    {
        var haulingTask = _context.Cache.Get<HaulingTask>(Task);
        if (haulingTask != null)
        {
            if (!_taskStarted)
            {
                MoveTo(haulingTask.Zone, OnOriginZoneArrival);
                _taskStarted = true;
            }
        }
        return base.Update(delta);
    }
    
    void OnOriginZoneArrival()
    {
        var haulingTask = _context.Cache.Get<HaulingTask>(Task);
        
        _entity.SetMessage("Entering pickup phase.");
        
        MoveTo(_context.Cache.Get<Entity>(haulingTask.Origin), true, OnPickup);
    }
    
    void OnPickup()
    {
        var haulingTask = _context.Cache.Get<HaulingTask>(Task);
        var target = Zone.Entities[haulingTask.Origin] as OrbitalEntity;
        
        _context.SetParent(_entity, target);
        var itemData = _context.Cache.Get<ItemData>(haulingTask.ItemType);
        int quantity = min((int) ((_entity.Capacity - _entity.OccupiedCapacity) / itemData.Size), haulingTask.Quantity - _itemsDelivered);
        if (itemData is SimpleCommodityData)
        {
            var itemMatch = target.Cargo
                .Select(id => _context.Cache.Get<SimpleCommodity>(id))
                .FirstOrDefault(ii => ii != null && ii.Data == haulingTask.ItemType);
            if(itemMatch==null)
                FinishTask();
            _simpleCommodityDelivery = _entity.AddCargo(target.RemoveCargo(itemMatch, quantity));
        }
        else if (itemData is CraftedItemData)
        {
            var itemMatches = target.Cargo
                .Select(id => _context.Cache.Get<CraftedItemInstance>(id))
                .Where(ii => ii != null && ii.Data == haulingTask.ItemType)
                .ToArray();
            if(!itemMatches.Any())
                FinishTask();
            foreach (var match in itemMatches)
                _craftedItemDelivery.Add(_entity.AddCargo(target.RemoveCargo(match)));
        }
        Wait(_data.DockTime, OnItemObtained);
    }

    void OnItemObtained()
    {
        var haulingTask = _context.Cache.Get<HaulingTask>(Task);
        _entity.SetMessage("Entering delivery phase.");

        var target = _context.Cache.Get<Entity>(haulingTask.Target);

        MoveTo(target.Zone.Data.ID, OnTargetZoneArrival);
    }
    
    void OnTargetZoneArrival()
    {
        var haulingTask = _context.Cache.Get<HaulingTask>(Task);
        
        MoveTo(_context.Cache.Get<Entity>(haulingTask.Target), true, OnDelivery);
    }
    
    void OnDelivery()
    {
        var haulingTask = _context.Cache.Get<HaulingTask>(Task);
        var target = Zone.Entities[haulingTask.Target] as OrbitalEntity;
        
        _context.SetParent(_entity, target);
        
        var itemData = _context.Cache.Get<ItemData>(haulingTask.ItemType);
        if (itemData is SimpleCommodityData)
        {
            target.AddCargo(_entity.RemoveCargo(_simpleCommodityDelivery, _simpleCommodityDelivery.Quantity));
            _itemsDelivered += _simpleCommodityDelivery.Quantity;
            _simpleCommodityDelivery = null;
        }
        else if (itemData is CraftedItemData)
        {
            foreach (var item in _craftedItemDelivery)
                target.AddCargo(_entity.RemoveCargo(item));
            _itemsDelivered += _craftedItemDelivery.Count;
            _craftedItemDelivery.Clear();
        }
        
        Wait(_data.DockTime, () =>
        {
            if(_itemsDelivered == haulingTask.Quantity)
                FinishTask();
            else
                _taskStarted = false;
        });
    }

    public PersistentBehaviorData Store()
    {
        return new HaulingControllerPersistence
        {
            Task = Task
        };
    }

    public void Restore(PersistentBehaviorData data)
    {
        var towingControllerPersistence = data as HaulingControllerPersistence;
        Task = towingControllerPersistence.Task;
    }
}

public class HaulingControllerPersistence : PersistentBehaviorData
{
    [JsonProperty("task"), Key(0)]
    public Guid Task;
}