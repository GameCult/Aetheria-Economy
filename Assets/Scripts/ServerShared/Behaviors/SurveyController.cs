/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

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
public class SurveyControllerData : ControllerData
{
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new SurveyController(context, this, entity, item);
    }
}

public class SurveyController : ControllerBase<Survey>, IBehavior, IPersistentBehavior, IInitializableBehavior
{
    private EquippedItem Item { get; }
    
    private SurveyControllerData _data;
    private bool _taskStarted;
    private ResourceScanner _scanningTool;
    private Switch _toolSwitch;
    private Guid _targetPlanet;
    private int _asteroid = -1;
    
    public SurveyController(ItemManager itemManager, SurveyControllerData data, Entity entity, EquippedItem item) : base(itemManager, data, entity)
    {
        _data = data;
        Item = item;
    }

    public new void Initialize()
    {
        _scanningTool = Entity.GetBehavior<ResourceScanner>();
        _toolSwitch = Entity.GetSwitch<ResourceScanner>();
        base.Initialize();
    }

    public new bool Execute(float delta)
    {
        // var corporation = _context.ItemData.Get<Corporation>(_entity.Corporation);
        // if (surveyTask != null)
        // {
        //     if (!_taskStarted)
        //     {
        //         if (surveyTask.Planets.Any())
        //         {
        //             _entity.SetMessage("Moving to survey target.");
        //             NextPlanet();
        //             _scanningTool.ScanTarget = _targetPlanet;
        //             _scanningTool.Asteroid = _asteroid;
        //             if (!corporation.PlanetSurveyFloor.ContainsKey(_targetPlanet) ||
        //                 corporation.PlanetSurveyFloor[_targetPlanet] + .5f < _scanningTool.MinimumDensity)
        //             {
        //                 var planetData = _context.ItemData.Get<BodyData>(_targetPlanet);
        //                 MoveTo(() =>
        //                     {
        //                         var target = GetPosition(planetData, _asteroid);
        //                         var targetToUs = _entity.Position - target;
        //                         return target + normalize(targetToUs) * _scanningTool.Range / 2;
        //                     },
        //                     () => GetVelocity(planetData, _asteroid), 
        //                     () => _entity.SetMessage("Arrived at target. Scanning for resources."));
        //                 _taskStarted = true;
        //             }
        //             else
        //                 surveyTask.Planets.Remove(_targetPlanet);
        //         }
        //         else
        //         {
        //             _entity.SetMessage("Survey complete. Returning Home.");
        //             FinishTask();
        //         }
        //     }
        //     else
        //     {
        //         if (!Moving)
        //         {
        //             var planetData = _context.ItemData.Get<BodyData>(_targetPlanet);
        //             if ((!corporation.PlanetSurveyFloor.ContainsKey(_targetPlanet) ||
        //                 corporation.PlanetSurveyFloor[_targetPlanet] + .5f < _scanningTool.MinimumDensity) &&
        //                 length(_entity.Position - GetPosition(planetData, _asteroid)) < _scanningTool.Range)
        //             {
        //                 _toolSwitch.Activated = true;
        //                 Aim.Objective = GetPosition(planetData, _asteroid);
        //                 Aim.Update(delta);
        //             }
        //             else
        //             {
        //                 _toolSwitch.Activated = false;
        //                 _taskStarted = false;
        //             }
        //         }
        //     }
        // }
        return base.Execute(delta);
    }

    private void NextPlanet()
    {
        var planets = Task.Planets.Select(id => Zone.Planets[id]);
        var nearestPlanet = planets.MinBy(p => lengthsq(Entity.Position.xz - GetPosition(p)));
        _asteroid = nearestPlanet is AsteroidBeltData ? Zone.NearestAsteroid(nearestPlanet.ID, Entity.Position.xz) : -1;
        _targetPlanet = nearestPlanet.ID;
    }

    private float2 GetPosition(BodyData planet, int asteroid = -1)
    {
        if (planet is AsteroidBeltData)
        {
            if(asteroid == -1) asteroid = Zone.NearestAsteroid(planet.ID, Entity.Position.xz);
            return Zone.GetAsteroidTransform(planet.ID, asteroid).xy;
        }
        return Zone.GetOrbitPosition(planet.Orbit);
    }

    private float2 GetVelocity(BodyData planet, int asteroid = -1)
    {
        if (planet is AsteroidBeltData)
        {
            if(asteroid == -1) asteroid = Zone.NearestAsteroid(planet.ID, Entity.Position.xz);
            return Zone.GetAsteroidVelocity(planet.ID, asteroid);
        }
        return Zone.GetOrbitVelocity(planet.Orbit);
    }

    public PersistentBehaviorData Store()
    {
        return new SurveyControllerPersistence
        {
            Task = Task
        };
    }

    public void Restore(PersistentBehaviorData data)
    {
        var surveyControllerPersistence = data as SurveyControllerPersistence;
        Task = surveyControllerPersistence.Task;
    }
}

public class SurveyControllerPersistence : PersistentBehaviorData
{
    [JsonProperty("task"), Key(0)]
    public Survey Task;
}