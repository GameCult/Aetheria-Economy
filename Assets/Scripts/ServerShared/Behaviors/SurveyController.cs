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
    public override IBehavior CreateInstance(GameContext context, Entity entity, Gear item)
    {
        return new SurveyController(context, this, entity, item);
    }
}

public class SurveyController : ControllerBase, IBehavior, IPersistentBehavior, IInitializableBehavior
{
    public override TaskType TaskType => TaskType.Explore;
    public BehaviorData Data => _data;
    
    private SurveyControllerData _data;
    private GameContext _context;
    private Entity _entity;
    private Gear _item;
    private bool _taskStarted;
    private ResourceScanner _scanningTool;
    private Switch _toolSwitch;
    private Guid _targetPlanet;
    private int _asteroid = -1;
    
    public SurveyController(GameContext context, SurveyControllerData data, Entity entity, Gear item) : base(context, data, entity)
    {
        _context = context;
        _data = data;
        _entity = entity;
        _item = item;
    }

    public new void Initialize()
    {
        _scanningTool = _entity.GetBehaviors<ResourceScanner>().First();
        _toolSwitch = _entity.GetSwitch(_scanningTool);
        base.Initialize();
    }

    public new bool Update(float delta)
    {
        var surveyTask = _context.Cache.Get<Survey>(Task);
        var corporation = _context.Cache.Get<Corporation>(_entity.Corporation);
        if (surveyTask != null)
        {
            if (!_taskStarted)
            {
                if (surveyTask.Planets.Any())
                {
                    _entity.SetMessage("Moving to survey target.");
                    NextPlanet();
                    _scanningTool.ScanTarget = _targetPlanet;
                    _scanningTool.Asteroid = _asteroid;
                    if (!corporation.PlanetSurveyFloor.ContainsKey(_targetPlanet) ||
                        corporation.PlanetSurveyFloor[_targetPlanet] + .5f < _scanningTool.MinimumDensity)
                    {
                        var planetData = _context.Cache.Get<PlanetData>(_targetPlanet);
                        MoveTo(() =>
                            {
                                var target = GetPosition(planetData, _asteroid);
                                var targetToUs = _entity.Position - target;
                                return target + normalize(targetToUs) * _scanningTool.Range / 2;
                            },
                            () => GetVelocity(planetData, _asteroid), 
                            () => _entity.SetMessage("Arrived at target. Scanning for resources."));
                        _taskStarted = true;
                    }
                    else
                        surveyTask.Planets.Remove(_targetPlanet);
                }
                else
                {
                    _entity.SetMessage("Survey complete. Returning Home.");
                    FinishTask();
                }
            }
            else
            {
                if (!Moving)
                {
                    var planetData = _context.Cache.Get<PlanetData>(_targetPlanet);
                    if ((!corporation.PlanetSurveyFloor.ContainsKey(_targetPlanet) ||
                        corporation.PlanetSurveyFloor[_targetPlanet] + .5f < _scanningTool.MinimumDensity) &&
                        length(_entity.Position - GetPosition(planetData, _asteroid)) < _scanningTool.Range)
                    {
                        _toolSwitch.Activated = true;
                        Aim.Objective = GetPosition(planetData, _asteroid);
                        Aim.Update(delta);
                    }
                    else
                    {
                        _toolSwitch.Activated = false;
                        _taskStarted = false;
                    }
                }
            }
        }
        return base.Update(delta);
    }

    private void NextPlanet()
    {
        var surveyTask = _context.Cache.Get<Survey>(Task);
        var planets = surveyTask.Planets.Select(id => _context.Cache.Get<PlanetData>(id));
        var nearestPlanet = planets.MinBy(p => lengthsq(_entity.Position - GetPosition(p)));
        _asteroid = nearestPlanet.Belt ? _context.NearestAsteroid(nearestPlanet.ID, _entity.Position) : -1;
        _targetPlanet = nearestPlanet.ID;
    }

    private float2 GetPosition(PlanetData planet, int asteroid = -1)
    {
        if (planet.Belt)
        {
            if(asteroid == -1) asteroid = _context.NearestAsteroid(planet.ID, _entity.Position);
            return _context.GetAsteroidTransform(planet.ID, asteroid).xy;
        }
        return _context.GetOrbitPosition(planet.Orbit);
    }

    private float2 GetVelocity(PlanetData planet, int asteroid = -1)
    {
        if (planet.Belt)
        {
            if(asteroid == -1) asteroid = _context.NearestAsteroid(planet.ID, _entity.Position);
            return _context.GetAsteroidVelocity(planet.ID, asteroid);
        }
        return _context.GetOrbitVelocity(planet.Orbit);
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
    public Guid Task;
}