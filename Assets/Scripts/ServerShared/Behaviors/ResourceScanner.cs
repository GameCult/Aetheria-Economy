using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class ResourceScannerData : BehaviorData
{
    [InspectableField, JsonProperty("range"), Key(1)]
    public PerformanceStat Range = new PerformanceStat();
    
    [InspectableField, JsonProperty("minDensity"), Key(2)]
    public PerformanceStat MinimumDensity = new PerformanceStat();
    
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new ResourceScanner(context, this, entity, item);
    }
}

public class ResourceScanner : IBehavior
{
    private ResourceScannerData _data;

    private Entity Entity { get; }
    private Gear Item { get; }
    private GameContext Context { get; }

    public BehaviorData Data => _data;

    public ResourceScanner(GameContext context, ResourceScannerData data, Entity entity, Gear item)
    {
        _data = data;
        Entity = entity;
        Item = item;
        Context = context;
    }

    public bool Update(float delta)
    {
        // TODO: Implement resource scanners!
        return true;
    }
}