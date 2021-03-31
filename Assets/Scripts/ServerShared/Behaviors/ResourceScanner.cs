/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn), RuntimeInspectable]
public class ResourceScannerData : BehaviorData
{
    [Inspectable, JsonProperty("range"), Key(1), RuntimeInspectable]
    public PerformanceStat Range = new PerformanceStat();
    
    [Inspectable, JsonProperty("minDensity"), Key(2), RuntimeInspectable]
    public PerformanceStat MinimumDensity = new PerformanceStat();
    
    [Inspectable, JsonProperty("scanDuration"), Key(3), RuntimeInspectable]
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

    public bool Execute(float delta)
    {
        var planetData = Context.ItemData.Get<BodyData>(ScanTarget);
        if (planetData != null)
        {
            if (planetData is AsteroidBeltData beltData)
            {
                if(Asteroid > -1 &&
                   Asteroid < beltData.Asteroids.Length &&
                   length(Entity.Position.xz - Entity.Zone.AsteroidBelts[ScanTarget].Positions[Asteroid].xz) < Range)
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
                if(length(Entity.Position.xz - Entity.Zone.GetOrbitPosition(planetData.Orbit)) < Range)
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

    public void Update(float delta)
    {
        Range = Item.Evaluate(_data.Range);
        MinimumDensity = Item.Evaluate(_data.MinimumDensity);
        ScanDuration = Item.Evaluate(_data.ScanDuration);
    }
}