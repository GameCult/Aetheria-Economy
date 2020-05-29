using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class ResourceScannerData : BehaviorData
{
    [InspectableField, JsonProperty("range"), Key(1)]
    public PerformanceStat Range = new PerformanceStat();
    
    [InspectableField, JsonProperty("minDensity"), Key(2)]
    public PerformanceStat MinimumDensity = new PerformanceStat();
    
    [InspectableField, JsonProperty("scanDuration"), Key(3)]
    public PerformanceStat ScanDuration = new PerformanceStat();
    
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new ResourceScanner(context, this, entity, item);
    }
}

public class ResourceScanner : IBehavior, IAlwaysUpdatedBehavior
{
    public int Asteroid = -1;
    
    private ResourceScannerData _data;
    private float _scanTime;
    private Guid _scanTarget;

    private Entity Entity { get; }
    private Gear Item { get; }
    private GameContext Context { get; }

    public BehaviorData Data => _data;
    public float Range { get; private set; }
    public float MinimumDensity { get; private set; }
    public float ScanDuration { get; private set; }

    public Guid ScanTarget
    {
        get => _scanTarget;
        set
        {
            if (value != _scanTarget)
            {
                _scanTarget = value;
                _scanTime = 0;
            }
        }
    }

    public ResourceScanner(GameContext context, ResourceScannerData data, Entity entity, Gear item)
    {
        _data = data;
        Entity = entity;
        Item = item;
        Context = context;
    }

    public bool Update(float delta)
    {
        var planetData = Context.Cache.Get<PlanetData>(ScanTarget);
        if (planetData != null)
        {
            if (planetData.Belt)
            {
                if(Asteroid > -1 &&
                   Asteroid < planetData.Asteroids.Length &&
                   length(Entity.Position - Context.GetAsteroidTransform(ScanTarget, Asteroid).xy) < Range)
                {
                    _scanTime += delta;
                    if (_scanTime > ScanDuration)
                    {
                        Context.Cache.Get<Corporation>(Entity.Corporation).PlanetSurveyFloor[ScanTarget] = MinimumDensity;
                        _scanTime = 0;
                    }
                    return true;
                }
            }
            else
            {
                if(length(Entity.Position - Context.GetOrbitPosition(planetData.Orbit)) < Range)
                {
                    _scanTime += delta;
                    if (_scanTime > ScanDuration)
                    {
                        Context.Cache.Get<Corporation>(Entity.Corporation).PlanetSurveyFloor[ScanTarget] = MinimumDensity;
                        _scanTime = 0;
                    }
                    return true;
                }
            }
        }
        return false;
    }

    public void AlwaysUpdate(float delta)
    {
        Range = Context.Evaluate(_data.Range, Item, Entity);
        MinimumDensity = Context.Evaluate(_data.MinimumDensity, Item, Entity);
        ScanDuration = Context.Evaluate(_data.ScanDuration, Item, Entity);
    }
}