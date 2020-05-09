using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;
using Random = Unity.Mathematics.Random;
using JM.LinqFaster;

public class GameContext
{
    public Random Random = new Random((uint) (DateTime.Now.Ticks%uint.MaxValue));
    public Dictionary<Guid, Guid[]> ZonePlanets = new Dictionary<Guid, Guid[]>();
    public Dictionary<Guid, Dictionary<Guid, Entity>> ZoneEntities = new Dictionary<Guid, Dictionary<Guid, Entity>>();
    public Dictionary<string, GalaxyMapLayerData> MapLayers = new Dictionary<string, GalaxyMapLayerData>();
    public Dictionary<Guid, List<IController>> CorporationControllers = new Dictionary<Guid, List<IController>>();
    public Dictionary<Guid, SimplifiedZoneData> GalaxyZones;
    
    private Action<string> _logger;

    private Dictionary<BlueprintStatEffect, PerformanceStat> AffectedStats =
        new Dictionary<BlueprintStatEffect, PerformanceStat>();

    private double _time;
    private float _deltaTime;
    private GlobalData _globalData;
    private HashSet<Guid> _loadedZones = new HashSet<Guid>();
    private HashSet<Guid> _orbits = new HashSet<Guid>();
    private Dictionary<Guid, float2> _orbitVelocities = new Dictionary<Guid, float2>();
    private Dictionary<Guid, float2> _orbitPositions = new Dictionary<Guid, float2>();
    private Dictionary<Guid, float2> _previousOrbitPositions = new Dictionary<Guid, float2>();
    private Guid _forceLoadZone;

    public GlobalData GlobalData => _globalData ?? (_globalData = Cache.GetAll<GlobalData>().FirstOrDefault());
    public DatabaseCache Cache { get; }

    public Guid ForceLoadZone
    {
        get => _forceLoadZone;
        set
        {
            if(_forceLoadZone != Guid.Empty && ZoneEntities[_forceLoadZone].Count == 0)
                UnloadZone(_forceLoadZone);
            _forceLoadZone = value;
            if(!_loadedZones.Contains(_forceLoadZone))
                LoadZone(_forceLoadZone);
        }
    }

    public double Time
    {
        get => _time;
        set
        {
            _deltaTime = (float) (value - _time);
            _time = value;
            //Log($"GameContext delta time: {_deltaTime}");
        }
    }

    public float2 WormholePosition(Guid zone, Guid other)
    {
        var zoneData = Cache.Get<ZoneData>(zone);
        var direction = Cache.Get<ZoneData>(other).Position - zoneData.Position;
        return normalize(direction) * zoneData.Radius * .95f;
    }

    // private readonly Dictionary<CraftedItemData, int> Tier = new Dictionary<CraftedItemData, int>();

    public GameContext(DatabaseCache cache, Action<string> logger)
    {
        Cache = cache;
        _logger = logger;
        // var globalData = Cache.GetAll<GlobalData>().FirstOrDefault();
        // if (globalData == null)
        // {
        //     globalData = new GlobalData();
        //     Cache.Add(globalData);
        // }
    }

    public void Log(string s)
    {
        _logger(s);
    }

    public void Update()
    {
        _previousOrbitPositions = _orbitPositions;
        _orbitPositions = new Dictionary<Guid, float2>(_orbits.Count);
        
        foreach(var orbit in _orbits)
        {
            _orbitPositions[orbit] = GetOrbitPosition(orbit);
            _orbitVelocities[orbit] = _previousOrbitPositions.ContainsKey(orbit)
                ? (_orbitPositions[orbit] - _previousOrbitPositions[orbit]) / _deltaTime
                : float2.zero;
        }

        foreach (var corporation in Cache.GetAll<Corporation>())
        {
            foreach (var tasks in corporation.Tasks
                .Select(id => Cache.Get<AgentTask>(id)) // Fetch the tasks from the database cache
                .Where(task => !task.Reserved) // Filter out tasks that have already been reserved
                .GroupBy(task => task.Type)) // Group tasks by type
            {
                // Create a list of available controllers for this task type
                var availableControllers = CorporationControllers[corporation.ID]
                    .Where(controller => controller.TaskType == tasks.Key && controller.Available).ToList();
                
                // Iterate over the highest priority tasks for which controllers are available
                foreach (var task in tasks.OrderByDescending(task => task.Priority).Take(availableControllers.Count))
                {
                    // Find the nearest controller for this task
                    IController nearestController = availableControllers[0];
                    List<SimplifiedZoneData> nearestControllerPath = FindPath(GalaxyZones[availableControllers.First().Zone], GalaxyZones[task.Zone], true);
                    foreach (var controller in availableControllers.Skip(1))
                    {
                        var path = FindPath(GalaxyZones[controller.Zone], GalaxyZones[task.Zone], true);
                        if (path.Count < nearestControllerPath.Count)
                        {
                            nearestControllerPath = path;
                            nearestController = controller;
                        }
                    }
                    task.Reserved = true;
                    nearestController.AssignTask(task.ID, nearestControllerPath);
                }
            }
                
        }
        
        foreach (var kvp in ZoneEntities)
        {
            foreach (var entity in kvp.Value.Values) entity.Update(_deltaTime);
        }
    }

    public IEnumerable<Entity> GetEntities(Guid zone)
    {
        return ZoneEntities[zone].Values;
    }

    public void LoadZone(Guid zone)
    {
        var zoneData = Cache.Get<ZoneData>(zone);

        _loadedZones.Add(zone);
        
        foreach (var orbit in zoneData.Orbits) _orbits.Add(orbit);

        // TODO: Associate planets with stored entities for planetary colonies
        ZonePlanets[zone] = zoneData.Planets;
        
        // TODO: Load stored entities
        ZoneEntities[zone] = new Dictionary<Guid, Entity>();
    }

    public void UnloadZone(Guid zone)
    {
        var zoneData = Cache.Get<ZoneData>(zone);

        _loadedZones.Remove(zone);
        ZoneEntities.Remove(zone);
        ZonePlanets.Remove(zone);
        foreach (var orbit in zoneData.Orbits) _orbits.Remove(orbit);
    }

    public void Warp(Entity entity, Guid targetZone)
    {
        if (length(entity.Position - WormholePosition(entity.Zone, targetZone)) < GlobalData.WarpDistance)
        {
            var sourceZone = entity.Zone;
            
            // Remove entity from source zone
            ZoneEntities[sourceZone].Remove(entity.ID);
            
            // Unload source zone if empty and unobserved
            if(ForceLoadZone != sourceZone && ZoneEntities[sourceZone].Count == 0)
                UnloadZone(sourceZone);
            
            // Load target zone if not yet loaded
            if(!_loadedZones.Contains(targetZone))
                LoadZone(targetZone);
            
            entity.Zone = targetZone;
            entity.Position = WormholePosition(targetZone, sourceZone);
            ZoneEntities[targetZone][entity.ID] = entity;
        }
    }

    // Determine orbital position recursively, caching parent positions to avoid repeated calculations
    public float2 GetOrbitPosition(Guid orbit)
    {
        // Root orbit is fixed at center
        if(orbit==Guid.Empty)
            return float2.zero;
        
        if (!_orbitPositions.ContainsKey(orbit))
        {
            var orbitData = Cache.Get<OrbitData>(orbit);
            _orbitPositions[orbit] = GetOrbitPosition(orbitData.Parent) + (orbitData.Period < .01f ? float2.zero : 
                OrbitData.Evaluate((float) frac(Time / -orbitData.Period * GlobalData.OrbitSpeedMultiplier + orbitData.Phase)) *
                orbitData.Distance);
        }

        var position = _orbitPositions[orbit];
        if (float.IsNaN(position.x))
        {
            Log("Orbit position is NaN, something went very wrong!");
            return float2.zero;
        }
        return _orbitPositions[orbit];
    }

    public float2 GetOrbitVelocity(Guid orbit)
    {
        return _orbitVelocities.ContainsKey(orbit) ? _orbitVelocities[orbit] : float2.zero;
    }

    public OrbitData CreateOrbit(Guid parent, float2 position)
    {
        var parentOrbit = Cache.Get<OrbitData>(parent);
        var parentPosition = GetOrbitPosition(parent);
        var delta = position - parentPosition;
        var distance = length(delta);
        var period = OrbitalPeriod(distance);
        var phase = atan2(delta.y, delta.x) / (PI * 2);
        var storedPhase = (float) frac(Time / -period * GlobalData.OrbitSpeedMultiplier - phase);

        var orbit = new OrbitData
        {
            Context = this,
            Distance = distance,
            ID = Guid.NewGuid(),
            Parent = parent,
            Period = period,
            Phase = storedPhase,
            Zone = parentOrbit.Zone
        };
        Cache.Add(orbit);
        return orbit;
    }

    public float OrbitalPeriod(float distance)
    {
        return pow(distance, GlobalData.OrbitPeriodExponent) * GlobalData.OrbitPeriodMultiplier;
    }
    
    // public int ItemTier(CraftedItemData itemData)
    // {
    //     if (Tier.ContainsKey(itemData)) return Tier[itemData];
    //
    //     Tier[itemData] = itemData.Ingredients.Keys.Max(ci => _cache.Get<ItemData>(ci) is CraftedItemData craftableIngredient ? ItemTier(craftableIngredient) : 0);
		  //
    //     return Tier[itemData];
    // }

    public ItemData GetData(ItemInstance item)
    {
        return Cache.Get<ItemData>(item.Data);
    }

    public SimpleCommodityData GetData(SimpleCommodity item)
    {
        return Cache.Get<SimpleCommodityData>(item.Data);
    }

    public CraftedItemData GetData(CraftedItemInstance item)
    {
        return Cache.Get<CraftedItemData>(item.Data);
    }

    public EquippableItemData GetData(Gear gear)
    {
        return Cache.Get<EquippableItemData>(gear.Data);
    }

    public float GetMass(ItemInstance item)
    {
        var data = GetData(item);
        switch (item)
        {
            case CraftedItemInstance _:
                return data.Mass;
            case SimpleCommodity commodity:
                return data.Mass * commodity.Quantity;
        }

        return 0;
    }

    public float GetHeatCapacity(ItemInstance item)
    {
        var data = GetData(item);
        switch (item)
        {
            case CraftedItemInstance _:
                return data.Mass * data.SpecificHeat;
            case SimpleCommodity commodity:
                return data.Mass * data.SpecificHeat * commodity.Quantity;
        }

        return 0;
    }

    public PerformanceStat GetAffectedStat(BlueprintData blueprint, BlueprintStatEffect effect)
    {
        // We've already cached this effect's stat, return it directly
        if (AffectedStats.ContainsKey(effect)) return AffectedStats[effect];
        
        // Get the data for the item the blueprint is for, return null if not found or not equippable
        var blueprintItem = Cache.Get<EquippableItemData>(blueprint.Item);
        if (blueprintItem == null)
        {
            _logger($"Attempted to get stat effect but Blueprint {blueprint.ID} is not for an equippable item!");
            return AffectedStats[effect] = null;
        }
        
        // Get the first behavior of the type specified in the blueprint stat effect, return null if not found
        var effectObject = effect.StatReference.Behavior == blueprintItem.Name ? (object) blueprintItem : 
            blueprintItem.Behaviors.FirstOrDefault(b => b.GetType().Name == effect.StatReference.Behavior);
        if (effectObject == null)
        {
            _logger($"Attempted to get stat effect for Blueprint {blueprint.ID} but stat references missing behavior \"{effect.StatReference.Behavior}\"!");
            return AffectedStats[effect] = null;
        }
        
        // Get the first field in the behavior matching the name specified in the stat effect, return null if not found
        var type = effectObject.GetType();
        var field = type.GetField(effect.StatReference.Stat);
        if (field == null)
        {
            _logger($"Attempted to get stat effect for Blueprint {blueprint.ID} but object {effect.StatReference.Behavior} does not have a stat named \"{effect.StatReference.Stat}\"!");
            return AffectedStats[effect] = null;
        }
        
        // Finally we've confirmed the stat effect is valid, return the affected stat
        return AffectedStats[effect] = field.GetValue(effectObject) as PerformanceStat;
    }

    // Determine quality of either the item itself or the specific ingredient this stat depends on
    public float Quality(PerformanceStat stat, Gear item)
    {
        var blueprint = Cache.Get<BlueprintData>(item.Blueprint);
        var activeEffects = blueprint.StatEffects.Where(x => GetAffectedStat(blueprint, x) == stat).ToArray();
        float quality;
        if (!activeEffects.Any())
            quality = item.CompoundQuality();
        else
        {
            var ingredientInstances = item.Ingredients.Select(i => Cache.Get<ItemInstance>(i)).ToArray();
            var ingredients = ingredientInstances.Where(i => activeEffects.Any(e => e.Ingredient == i.Data)).ToArray();
            if(ingredients.Length != activeEffects.Length)
            {
                _logger($"Item {item.ID} does not have the ingredients specified by the stat effects of its blueprint!");
                return 0;
            }

            float sum = 0;
            foreach (var i in ingredients)
            {
                if (i is CraftedItemInstance ci)
                    sum += ci.CompoundQuality();
                else _logger($"Blueprint stat effect for item {item.ID} specifies invalid (non crafted) ingredient!");
            }

            quality = sum / ingredients.Length;
        }

        return quality;
    }

    // Returns stat when not equipped
    public float Evaluate(PerformanceStat stat, Gear item)
    {
        var quality = pow(Quality(stat, item), stat.QualityExponent);

        var result = lerp(stat.Min, stat.Max, quality);
        
        if (float.IsNaN(result))
            return stat.Min;
        
        return result;
    }

    // Returns stat using ship temperature and modifiers
    public float Evaluate(PerformanceStat stat, Gear item, Entity entity)
    {
        var itemData = GetData(item);

        var heat = !stat.HeatDependent ? 1 : pow(itemData.Performance(entity.Temperature), Evaluate(itemData.HeatExponent,item));
        var durability = !stat.DurabilityDependent ? 1 : pow(item.Durability / Evaluate(itemData.Durability, item), Evaluate(itemData.DurabilityExponent,item));
        var quality = pow(Quality(stat, item), stat.QualityExponent);

        var scaleModifier = stat.GetScaleModifiers(entity).Values.Aggregate(1.0f, (current, mod) => current * mod);

        var constantModifier = stat.GetConstantModifiers(entity).Values.Sum();

        var result = lerp(stat.Min, stat.Max, heat * durability * quality) * scaleModifier + constantModifier;
        if (float.IsNaN(result))
            return stat.Min;
        return result;
    }
    
    public SimpleCommodity CreateInstance(Guid data, int count)
    {
        var item = Cache.Get<SimpleCommodityData>(data);
        if (item != null)
            return new SimpleCommodity
            {
                Data = data,
                Quantity = count,
                ID = Guid.NewGuid()
            };
        
        _logger("Attempted to create Simple Commodity instance using missing or incorrect item id");
        return null;
    }
    
    public CraftedItemInstance CreateInstance(Guid data, float quality)
    {
        var item = Cache.Get<CraftedItemData>(data);
        if (item == null)
        {
            _logger("Attempted to create crafted item instance using missing or incorrect item id!");
            return null;
        }

        var blueprint = Cache.GetAll<BlueprintData>().FirstOrDefault(b => b.Item == data);
        if (blueprint == null)
        {
            _logger("Attempted to create crafted item instance which has no blueprint!");
            return null;
        }
        
        var ingredients = blueprint.Ingredients.SelectMany(ci =>
            {
                var ingredient = Cache.Get(ci.Key);
                return ingredient is SimpleCommodityData
                    ? (IEnumerable<ItemInstance>) new[] {CreateInstance(ci.Key, ci.Value)}
                    : Enumerable.Range(0, ci.Value).Select(i => CreateInstance(ci.Key, quality));
            })
            .ToList();
        
        Cache.AddAll(ingredients);
        
        if (item is EquippableItemData equippableItemData)
        {
            var newGear = new Gear
            {
                Context = this,
                Data = data,
                ID = Guid.NewGuid(),
                Ingredients = ingredients.Select(i=>i.ID).ToList(),
                Quality = quality,
                Blueprint = blueprint.ID
            };
            newGear.Durability = Evaluate(equippableItemData.Durability, newGear);
            Cache.Add(newGear);
            return newGear;
        }

        var newCommodity = new CompoundCommodity
        {
            Context = this,
            Data = data,
            ID = Guid.NewGuid(),
            Ingredients = ingredients.Select(i=>i.ID).ToList(),
            Quality = quality,
            Blueprint = blueprint.ID
        };
        Cache.Add(newCommodity);
        return newCommodity;
    }
    
    class DijkstraNode
    {
        public float Cost;
        public DijkstraNode Parent;
        public SimplifiedZoneData Zone;
    }
	
    public List<SimplifiedZoneData> FindPath(SimplifiedZoneData source, SimplifiedZoneData target, bool bestFirst = false)
    {
        SortedList<float,DijkstraNode> members = new SortedList<float,DijkstraNode>{{0,new DijkstraNode{Zone = source}}};
        List<DijkstraNode> searched = new List<DijkstraNode>();
        while (true)
        {
            var s = members.FirstOrDefault(m => !searched.Contains(m.Value)).Value; // Lowest cost unsearched node
            if (s == null) return null; // No vertices left unsearched
            if (s.Zone == target) // We found the path
            {
                Stack<DijkstraNode> path = new Stack<DijkstraNode>(); // Since we start at the end, use a LIFO collection
                path.Push(s);
                while(path.Peek().Parent!=null) // Keep pushing until we reach the start, which has no parent
                    path.Push(path.Peek().Parent);
                return path.Select(dv => dv.Zone).ToList();
            }
            // For each adjacent star (filter already visited zones unless heuristic is in use)
            foreach (var dijkstraStar in s.Zone.Links.WhereSelectF(i => !bestFirst || members.All(m => m.Value.Zone != GalaxyZones[i]),
                    // Cost is parent cost plus distance
                    i => new DijkstraNode {Parent = s, Zone = GalaxyZones[i], Cost = s.Cost + length(s.Zone.Position - GalaxyZones[i].Position)}))
                // Add new member to list, sorted by cost plus optional heuristic
                members.Add(bestFirst ? dijkstraStar.Cost + length(dijkstraStar.Zone.Position - target.Position) : dijkstraStar.Cost, dijkstraStar);
            searched.Add(s);
        }
    }
}