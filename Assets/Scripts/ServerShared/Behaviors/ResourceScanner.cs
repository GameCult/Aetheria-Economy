using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn), RuntimeInspectable]
public class ResourceScannerData : BehaviorData
{
    [InspectableField, JsonProperty("range"), Key(1), RuntimeInspectable]
    public PerformanceStat Range = new PerformanceStat();
    
    [InspectableField, JsonProperty("minDensity"), Key(2), RuntimeInspectable]
    public PerformanceStat MinimumDensity = new PerformanceStat();
    
    [InspectableField, JsonProperty("scanDuration"), Key(3), RuntimeInspectable]
    public PerformanceStat ScanDuration = new PerformanceStat();
    
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
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
    private EquippedItem Item { get; }
    private ItemManager Context { get; }

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

    public ResourceScanner(ItemManager context, ResourceScannerData data, Entity entity, EquippedItem item)
    {
        _data = data;
        Entity = entity;
        Item = item;
        Context = context;
    }

    public bool Update(float delta)
    {
        var planetData = Context.ItemData.Get<BodyData>(ScanTarget);
        if (planetData != null)
        {
            if (planetData is AsteroidBeltData beltData)
            {
                if(Asteroid > -1 &&
                   Asteroid < beltData.Asteroids.Length &&
                   length(Entity.Position - Entity.Zone.GetAsteroidTransform(ScanTarget, Asteroid).xy) < Range)
                {
                    _scanTime += delta;
                    if (_scanTime > ScanDuration)
                    {
                        // TODO: Implement Scanning!
                        //Context.ItemData.Get<Corporation>(Entity.Corporation).PlanetSurveyFloor[ScanTarget] = MinimumDensity;
                        _scanTime = 0;
                    }
                    return true;
                }
            }
            else
            {
                if(length(Entity.Position - Entity.Zone.GetOrbitPosition(planetData.Orbit)) < Range)
                {
                    _scanTime += delta;
                    if (_scanTime > ScanDuration)
                    {
                        //Context.ItemData.Get<Corporation>(Entity.Corporation).PlanetSurveyFloor[ScanTarget] = MinimumDensity;
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
        Range = Context.Evaluate(_data.Range, Item.EquippableItem, Entity);
        MinimumDensity = Context.Evaluate(_data.MinimumDensity, Item.EquippableItem, Entity);
        ScanDuration = Context.Evaluate(_data.ScanDuration, Item.EquippableItem, Entity);
    }
}