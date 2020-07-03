using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;
using Random = Unity.Mathematics.Random;
using JM.LinqFaster;
using float4 = Unity.Mathematics.float4;

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
    private Dictionary<Guid, float4[]> _asteroidTransforms = new Dictionary<Guid, float4[]>(); // x, y, rotation, scale
    private Dictionary<Guid, float4[]> _previousAsteroidTransforms = new Dictionary<Guid, float4[]>(); // x, y, rotation, scale
    private Dictionary<(Guid, int), float> _asteroidRespawnTimers = new Dictionary<(Guid, int), float>();
    private Dictionary<(Guid, int), float> _asteroidDamage = new Dictionary<(Guid, int), float>();
    private Dictionary<(Guid, Guid, int), float> _asteroidMiningAccumulator = new Dictionary<(Guid, Guid, int), float>();
    private Guid _forceLoadZone;
    private Type[] _statObjects;
    
    public GlobalData GlobalData => _globalData ?? (_globalData = Cache.GetAll<GlobalData>().FirstOrDefault());
    public DatabaseCache Cache { get; }

    public Type[] StatObjects => _statObjects ?? (_statObjects = typeof(BehaviorData).GetAllChildClasses()
        .Concat(typeof(EquippableItemData).GetAllChildClasses()).ToArray());

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

        foreach (var asteroids in _asteroidTransforms.Keys)
            UpdateAsteroidTransforms(asteroids);

        foreach (var corporation in Cache.GetAll<Corporation>())
        {
            foreach (var tasks in corporation.Tasks
                .Select(id => Cache.Get<AgentTask>(id)) // Fetch the tasks from the database cache
                .Where(task => !task.Reserved) // Filter out tasks that have already been reserved
                .GroupBy(task => task.Type)) // Group tasks by type
            {
                // Create a list of available controllers for this task type
                var availableControllers = CorporationControllers[corporation.ID]
                    .Where(controller => controller.Available && controller.TaskType == tasks.Key).ToList();
                
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
                    nearestController.AssignTask(task.ID);
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

        foreach (var belt in zoneData.Planets
            .Select(id => Cache.Get<PlanetData>(id))
            .Where(p => p.Belt))
        {
            _asteroidTransforms[belt.ID] = new float4[belt.Asteroids.Length];
            _previousAsteroidTransforms[belt.ID] = new float4[belt.Asteroids.Length];
        }
        
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
        foreach (var belt in zoneData.Planets
            .Select(id => Cache.Get<PlanetData>(id))
            .Where(p => p.Belt))
        {
            _asteroidTransforms.Remove(belt.ID);
            _previousAsteroidTransforms.Remove(belt.ID);
        }
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
            float2 pos = float2.zero;
            if (orbitData.Period > .01f)
            {
                var phase = (float) frac(Time / orbitData.Period * GlobalData.OrbitSpeedMultiplier);
                pos = OrbitData.Evaluate(frac(phase + orbitData.Phase)) * orbitData.Distance;
            }

            _orbitPositions[orbit] = GetOrbitPosition(orbitData.Parent) + pos;
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

    public int NearestAsteroid(Guid planetDataID, float2 position)
    {
        var planetData = Cache.Get<PlanetData>(planetDataID);

        var asteroidPositions = GetAsteroidTransforms(planetDataID);

        int nearest = 0;
        float nearestDistance = Single.MaxValue;
        for (int i = 0; i < planetData.Asteroids.Length; i++)
        {
            var dist = lengthsq(asteroidPositions[i].xy - position);
            if (AsteroidExists(planetDataID, i) && dist < nearestDistance)
            {
                nearest = i;
                nearestDistance = dist;
            }
        }

        return nearest;
    }

    public bool AsteroidExists(Guid planetDataID, int asteroid) =>
        !_asteroidRespawnTimers.ContainsKey((planetDataID, asteroid));

    public float2 GetAsteroidVelocity(Guid planetDataID, int asteroid)
    {
        return (_asteroidTransforms[planetDataID][asteroid].xy - _previousAsteroidTransforms[planetDataID][asteroid].xy) / _deltaTime;
    }

    public float4 GetAsteroidTransform(Guid planetDataID, int asteroid)
    {
        return _asteroidTransforms[planetDataID][asteroid];
    }

    public float4[] GetAsteroidTransforms(Guid planetDataID)
    {
        return _asteroidTransforms[planetDataID];
    }

    private void UpdateAsteroidTransforms(Guid planetDataID)
    {
        var planetData = Cache.Get<PlanetData>(planetDataID);
        
        Array.Copy(_asteroidTransforms[planetDataID], _previousAsteroidTransforms[planetDataID], planetData.Asteroids.Length);
        
        var orbitData = Cache.Get<OrbitData>(planetData.Orbit);
        var orbitPosition = GetOrbitPosition(orbitData.Parent);
        for (var i = 0; i < planetData.Asteroids.Length; i++)
        {
            var size = planetData.Asteroids[i].Size;
            if(_asteroidRespawnTimers.ContainsKey((planetDataID, i))) size = 0;
            else if (_asteroidDamage.ContainsKey((planetDataID, i)))
            {
                var hpLerp = unlerp(GlobalData.AsteroidSizeMin, GlobalData.AsteroidSizeMax, size);
                var asteroidHitpoints = lerp(GlobalData.AsteroidHitpointsMin, GlobalData.AsteroidHitpointsMax, pow(hpLerp, GlobalData.AsteroidHitpointsPower));
                var sizeLerp = (asteroidHitpoints - _asteroidDamage[(planetDataID, i)]) / asteroidHitpoints;
                size = lerp(GlobalData.AsteroidSizeMin, size, sizeLerp);
            }
        
            _asteroidTransforms[planetDataID][i] = float4(
                OrbitData.Evaluate((float) frac(Time / OrbitalPeriod(planetData.Asteroids[i].Distance) * GlobalData.OrbitSpeedMultiplier +
                                                planetData.Asteroids[i].Phase)) * planetData.Asteroids[i].Distance + orbitPosition,
                (float) (Time * planetData.Asteroids[i].RotationSpeed % 360.0), size);
        }
    }

    public OrbitData CreateOrbit(Guid zone, Guid parent, float2 position)
    {
        var parentPosition = GetOrbitPosition(parent);
        var delta = position - parentPosition;
        var distance = length(delta);
        var period = OrbitalPeriod(distance);
        var phase = atan2(delta.y, delta.x) / (PI * 2);
        var currentPhase = frac(Time / period * GlobalData.OrbitSpeedMultiplier);
        var storedPhase = (float) frac(phase - currentPhase);

        var orbit = new OrbitData
        {
            Context = this,
            Distance = distance,
            Parent = parent,
            Period = period,
            Phase = storedPhase,
            Zone = zone
        };
        Cache.Add(orbit);
        _orbits.Add(orbit.ID);
        return orbit;
    }

    public float OrbitalPeriod(float distance)
    {
        return pow(distance, GlobalData.OrbitPeriodExponent) * GlobalData.OrbitPeriodMultiplier;
    }

    public void PlaceMegas()
    {
        var megas = Cache.GetAll<MegaCorporation>();
        
        var availableZones = Cache.GetAll<ZoneData>()
            .Where(z => megas.All(m => m.HomeZone != z.ID))
            .ToList();
        
        foreach(var megaPlacement in megas
            .OrderBy(m=>m.PlacementType)
            .GroupBy(m=>m.PlacementType))
            switch (megaPlacement.Key)
            {
                case MegaPlacementType.Mass:
                    foreach (var mega in megaPlacement)
                    {
                        var zone = availableZones
                            .MaxBy(z => z.Mass);
                        availableZones.Remove(zone);
                        mega.HomeZone = zone.ID;
                    }
                    break;
                case MegaPlacementType.Planets:
                    foreach (var mega in megaPlacement)
                    {
                        var zone = availableZones
                            .MaxBy(z => z.Planets.Length);
                        availableZones.Remove(zone);
                        mega.HomeZone = zone.ID;
                    }
                    break;
                case MegaPlacementType.Resources:
                    foreach (var mega in megaPlacement)
                    {
                        var zone = availableZones
                            .MaxBy(z => z.Planets
                                .SelectMany(id=>Cache.Get<PlanetData>(id).Resources
                                    .Select(r=>r.Key))
                                .Distinct()
                                .Count());
                        availableZones.Remove(zone);
                        mega.HomeZone = zone.ID;
                    }
                    break;
                case MegaPlacementType.Connected:
                    foreach (var mega in megaPlacement)
                    {
                        var zone = availableZones
                            .OrderByDescending(z => z.Wormholes.Count)
                            .ThenBy(z=>megas
                                .Sum(m=>m.HomeZone==Guid.Empty?0:lengthsq(Cache.Get<ZoneData>(m.HomeZone).Position - z.Position)))
                            .First();
                        availableZones.Remove(zone);
                        mega.HomeZone = zone.ID;
                    }
                    break;
                case MegaPlacementType.Isolated:
                    foreach (var mega in megaPlacement)
                    {
                        var zone = availableZones
                            .Where(z=>z.Wormholes.Count==1)
                            .MaxBy(z =>
                            {
                                int soloChain = 0;
                                var previous = z;
                                var next = Cache.Get<ZoneData>(z.Wormholes.First());
                                while (next.Wormholes.Count == 2)
                                {
                                    var temp = next;
                                    next = Cache.Get<ZoneData>(next.Wormholes.First(w=>w != previous.ID));
                                    previous = temp;
                                    soloChain++;
                                }

                                return soloChain;
                            });
                        availableZones.Remove(zone);
                        mega.HomeZone = zone.ID;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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

    public float GetSize(ItemInstance item)
    {
        var data = GetData(item);
        switch (item)
        {
            case CraftedItemInstance _:
                return data.Size;
            case SimpleCommodity commodity:
                return data.Size * commodity.Quantity;
        }

        return 0;
    }

    public float GetThermalMass(ItemInstance item)
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
        var effectObject = effect.StatReference.Target == blueprintItem.Name ? (object) blueprintItem : 
            blueprintItem.Behaviors.FirstOrDefault(b => b.GetType().Name == effect.StatReference.Target);
        if (effectObject == null)
        {
            _logger($"Attempted to get stat effect for Blueprint {blueprint.ID} but stat references missing behavior \"{effect.StatReference.Target}\"!");
            return AffectedStats[effect] = null;
        }
        
        // Get the first field in the behavior matching the name specified in the stat effect, return null if not found
        var type = effectObject.GetType();
        var field = type.GetField(effect.StatReference.Stat);
        if (field == null)
        {
            _logger($"Attempted to get stat effect for Blueprint {blueprint.ID} but object {effect.StatReference.Target} does not have a stat named \"{effect.StatReference.Stat}\"!");
            return AffectedStats[effect] = null;
        }
        
        // Finally we've confirmed the stat effect is valid, return the affected stat
        return AffectedStats[effect] = field.GetValue(effectObject) as PerformanceStat;
    }

    private readonly Dictionary<CraftedItemInstance, float> ItemQuality = new Dictionary<CraftedItemInstance, float>();

    public float CompoundQuality(CraftedItemInstance item)
    {
        if (ItemQuality.ContainsKey(item)) return ItemQuality[item];
		
        var quality = item.Quality;
			
        var craftedIngredients = item.Ingredients.Where(i => i is CraftedItemInstance).ToArray();
        if (craftedIngredients.Length > 0)
        {
            var ingredientQualityWeight = Cache.Get<CraftedItemData>(item.Data).IngredientQualityWeight;
            quality = quality * (1 - ingredientQualityWeight) +
                      craftedIngredients.Cast<CraftedItemInstance>().Average(CompoundQuality) * ingredientQualityWeight;
        }

        ItemQuality[item] = quality;

        return ItemQuality[item];
    }

    // Determine quality of either the item itself or the specific ingredient this stat depends on
    public float Quality(PerformanceStat stat, Gear item)
    {
        var blueprint = Cache.Get<BlueprintData>(item.Blueprint);
        var activeEffects = blueprint.StatEffects.Where(x => GetAffectedStat(blueprint, x) == stat).ToArray();
        float quality;
        if (!activeEffects.Any())
            quality = CompoundQuality(item);
        else
        {
            var ingredientInstances = item.Ingredients.Select(i => Cache.Get<ItemInstance>(i)).ToArray();
            var ingredients = ingredientInstances.Where(i => activeEffects.Any(e => e.Ingredient == i.Data)).ToArray();
            var distinctIngredients = ingredients.Select(i => i.Data).Distinct();
            if(distinctIngredients.Count() != activeEffects.Length)
            {
                _logger($"Item {item.ID} does not have the ingredients specified by the stat effects of its blueprint!");
                return 0;
            }

            float sum = 0;
            foreach (var i in ingredients)
            {
                if (i is CraftedItemInstance ci)
                    sum += CompoundQuality(ci);
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

    public Entity CreateEntity(Guid zone, Guid corporation, Guid loadout)
    {
        if (!(Cache.Get(loadout) is LoadoutData loadoutData))
        {
            _logger("Attempted to spawn invalid loadout ID");
            return null;
        }

        if (!(Cache.Get(loadoutData.Hull) is HullData hullData))
        {
            _logger("Attempted to spawn loadout with invalid hull ID");
            return null;
        }

        if (!(Cache.Get(zone) is ZoneData zoneData))
        {
            _logger("Attempted to spawn entity with invalid zone ID");
            return null;
        }

        if (!(Cache.Get(corporation) is Corporation corpData))
        {
            _logger("Attempted to spawn entity with invalid corporation ID");
            return null;
        }

        var gearData = loadoutData.Gear
            .Take(hullData.Hardpoints.Count)
            .Where(id => id != Guid.Empty)
            .Select(id => Cache.Get<GearData>(id))
            .ToArray();

        if (gearData.Any(g=>g==null))
        {
            _logger("Attempted to spawn loadout with invalid gear ID");
            return null;
        }

        var gear = gearData
            .Select(gd => CreateInstance(gd.ID, GlobalData.StartingGearQualityMin, GlobalData.StartingGearQualityMax))
            .ToArray();

        if (gear.Any(g=>g==null))
            return null;
        
        foreach(var g in gear)
            Cache.Add(g);

        var cargoData = loadoutData.CompoundCargo
            .SelectMany(x => Enumerable.Repeat(Cache.Get<CraftedItemData>(x.Key), x.Value))
            .ToArray();

        if (cargoData.Any(g=>g==null))
        {
            _logger("Attempted to spawn loadout with invalid cargo ID");
            return null;
        }

        var cargo = cargoData
            .Select(gd => CreateInstance(gd.ID, GlobalData.StartingGearQualityMin, GlobalData.StartingGearQualityMax))
            .ToArray();

        if (cargo.Any(g=>g==null))
            return null;
        
        foreach(var c in cargo)
            Cache.Add(c);

        var simpleCargo = loadoutData.SimpleCargo?
            .Select(x => CreateInstance(x.Key, x.Value))
            .ToArray() ?? new SimpleCommodity[0];

        if (simpleCargo.Any(g=>g==null))
            return null;
        
        var hull = CreateInstance(loadoutData.Hull, GlobalData.StartingGearQualityMin, GlobalData.StartingGearQualityMax);
        
        if(hull==null)
            return null;

        // Load target zone if not yet loaded
        if (!_loadedZones.Contains(zone))
            LoadZone(zone);

        if (hullData.HullType == HullType.Ship)
        {
            var entity = CreateShip(hull.ID, gear.Select(g => g.ID),
                cargo.Select(c => c.ID).Concat(simpleCargo.Select(c => c.ID)), zone, corporation, Guid.Empty, loadoutData.Name);
            entity.Active = true;
            return entity;
        }

        if (hullData.HullType == HullType.Station)
        {
            var distance = lerp(.2f, .8f, Random.NextFloat()) * zoneData.Radius;
            var orbit = new OrbitData
            {
                Context = this,
                Distance = distance,
                Parent = _orbits
                    .Select(id => Cache.Get<OrbitData>(id))
                    .First(orbitData => orbitData.Zone == zone && orbitData.Parent == Guid.Empty).ID,
                Period = OrbitalPeriod(distance),
                Phase = Random.NextFloat(),
                Zone = zone
            };
            Cache.Add(orbit);
            _orbits.Add(orbit.ID);
            
            var entity = new OrbitalEntity(this, hull.ID, gear.Select(g => g.ID),
                cargo.Select(c => c.ID).Concat(simpleCargo.Select(c => c.ID)), orbit.ID, zone, corporation)
            {
                Name = $"{loadoutData.Name} {Random.NextInt(1,255):X}",
                Temperature = 293,
                Population = 4
            };
            entity.Active = true;
            var parentCorp = Cache.Get<MegaCorporation>(corpData.Parent);
            foreach (var attribute in parentCorp.Personality)
                entity.Personality[attribute.Key] = attribute.Value;
            Cache.Add(entity);

            ZoneEntities[zone][entity.ID] = entity;
            return entity;
        }

        return null;
    }

    public Ship CreateShip(Guid hull, IEnumerable<Guid> gear, IEnumerable<Guid> cargo, Guid zone, Guid corporation, Guid homeEntity, string typeName)
    {

        var ship = new Ship(this, hull, gear, cargo, zone, corporation)
        {
            Name = $"{typeName} {Random.NextInt(1,255):X}",
            HomeEntity = homeEntity
        };
        Cache.Add(ship);

        ZoneEntities[zone][ship.ID] = ship;
        return ship;
    }
    
    public SimpleCommodity CreateInstance(Guid data, int count)
    {
        var item = Cache.Get<SimpleCommodityData>(data);
        if (item != null)
        {
            var newItem = new SimpleCommodity
            {
                Context = this,
                Data = data,
                Quantity = count
            };
            Cache.Add(newItem);
            return newItem;
        }
        
        _logger("Attempted to create Simple Commodity instance using missing or incorrect item id");
        return null;
    }
    
    public CraftedItemInstance CreateInstance(Guid data, float qualityMin, float qualityMax)
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

        bool invalidIngredientFound = false;
        ItemData invalidIngredient = null;
        var ingredients = new List<ItemInstance>();
        foreach (var ingredient in blueprint.Ingredients)
        {
            var itemData = Cache.Get<ItemData>(ingredient.Key);
            if (itemData is SimpleCommodityData)
            {
                var itemInstance = CreateInstance(ingredient.Key, ingredient.Value);
                if(itemInstance == null)
                {
                    invalidIngredientFound = true;
                    invalidIngredient = itemData;
                }
                else ingredients.Add(itemInstance);
            }
            else
            {
                for (int i = 0; i < ingredient.Value; i++)
                {
                    var itemInstance = CreateInstance(ingredient.Key, qualityMin, qualityMax);
                    if (itemInstance == null)
                    {
                        invalidIngredientFound = true;
                        invalidIngredient = itemData;
                        break;
                    }

                    ingredients.Add(itemInstance);
                }
            }
            
        }
        
        if (invalidIngredientFound)
        {
            _logger($"Unable to create crafted item ingredient: {invalidIngredient?.Name??"null"} for item {item.Name}");
        }
        
        Cache.AddAll(ingredients);
        
        if (item is EquippableItemData equippableItemData)
        {
            var newGear = new Gear
            {
                Context = this,
                Data = data,
                Ingredients = ingredients.Select(i=>i.ID).ToList(),
                Quality = Random.NextFloat(qualityMin, qualityMax),
                Blueprint = blueprint.ID,
                Name = $"{item.Name}"
            };
            newGear.Durability = Evaluate(equippableItemData.Durability, newGear);
            Cache.Add(newGear);
            return newGear;
        }

        var newCommodity = new CompoundCommodity
        {
            Context = this,
            Data = data,
            Ingredients = ingredients.Select(i=>i.ID).ToList(),
            Quality = Random.NextFloat(qualityMin, qualityMax),
            Blueprint = blueprint.ID,
            Name = $"{item.Name}"
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

    public void SetParent(Entity child, Entity parent)
    {
        child.Parent = parent.ID;
        parent.AddChild(child);
    }

    public void RemoveParent(Entity child)
    {
        if (child.Parent == Guid.Empty)
            return;

        var parent = Cache.Get<Entity>(child.Parent);
        parent.RemoveChild(child);
        child.Parent = Guid.Empty;
    }

    public void MineAsteroid(Entity miner, Guid asteroidBelt, int asteroid, float damage, float efficiency, float penetration)
    {
        var planetData = Cache.Get<PlanetData>(asteroidBelt);
        var asteroidTransform = GetAsteroidTransform(asteroidBelt, asteroid);
        var hpLerp = unlerp(GlobalData.AsteroidSizeMin, GlobalData.AsteroidSizeMax, asteroidTransform.w);
        var asteroidHitpoints = lerp(GlobalData.AsteroidHitpointsMin, GlobalData.AsteroidHitpointsMax, pow(hpLerp, GlobalData.AsteroidHitpointsPower));
        
        if (!_asteroidDamage.ContainsKey((asteroidBelt, asteroid)))
            _asteroidDamage[(asteroidBelt, asteroid)] = 0;
        _asteroidDamage[(asteroidBelt, asteroid)] = _asteroidDamage[(asteroidBelt, asteroid)] + damage;
        
        if (!_asteroidMiningAccumulator.ContainsKey((asteroidBelt, miner.ID, asteroid)))
            _asteroidMiningAccumulator[(asteroidBelt, miner.ID, asteroid)] = 0;
        _asteroidMiningAccumulator[(asteroidBelt, miner.ID, asteroid)] = _asteroidMiningAccumulator[(asteroidBelt, miner.ID, asteroid)] + damage;
        
        if (_asteroidDamage[(asteroidBelt, asteroid)] > asteroidHitpoints)
        {
            _asteroidRespawnTimers[(asteroidBelt, asteroid)] =
                lerp(GlobalData.AsteroidRespawnMin, GlobalData.AsteroidRespawnMax, hpLerp);
            _asteroidDamage.Remove((asteroidBelt, asteroid));
            _asteroidMiningAccumulator.Remove((asteroidBelt, miner.ID, asteroid));
            return;
        }

        var resourceCount = planetData.Resources.Sum(x => x.Value);
        var resource = planetData.Resources.MaxBy(x => pow(x.Value, 1f / penetration) * Random.NextFloat());
        if (efficiency * Random.NextFloat() * _asteroidMiningAccumulator[(asteroidBelt, miner.ID, asteroid)] * resourceCount / GlobalData.MiningDifficulty > 1 && miner.OccupiedCapacity < miner.Capacity - 1)
        {
            _asteroidMiningAccumulator.Remove((asteroidBelt, miner.ID, asteroid));
            var newSimpleCommodity = new SimpleCommodity
            {
                Context = this,
                Data = resource.Key,
                Quantity = 1
            };
            Cache.Add(newSimpleCommodity);
            miner.AddCargo(newSimpleCommodity);
        }
    }

    // public bool MoveCargo(Entity source, Entity target, ItemInstance item, int quantity = int.MaxValue)
    // {
    //     if (item is CraftedItemInstance craftedItemInstance)
    //     {
    //         var craftedItemData = Cache.Get<CraftedItemData>(craftedItemInstance.Data);
    //         if (target.Capacity - target.OccupiedCapacity > craftedItemData.Size)
    //         {
    //             // Target has the cargo capacity for the item, simply move the instance
    //             source.Cargo.Remove(item.ID);
    //             target.Cargo.Add(item.ID);
    //             source.RecalculateMass();
    //             target.RecalculateMass();
    //             return true;
    //         }
    //         return false;
    //     }
    //     
    //     if (item is SimpleCommodity simpleCommoditySource)
    //     {
    //         quantity = min(quantity, simpleCommoditySource.Quantity);
    //         var simpleCommodityData = Cache.Get<SimpleCommodityData>(simpleCommoditySource.Data);
    //         var spareCapacity = (int) (target.Capacity - target.OccupiedCapacity);
    //         var simpleCommodityTarget = target.Cargo
    //             .Select(i => Cache.Get<ItemInstance>(i))
    //             .FirstOrDefault(i => i.Data == simpleCommodityData.ID) as SimpleCommodity;
    //         var success = spareCapacity >= quantity;
    //         quantity = min(quantity, spareCapacity);
    //         if (simpleCommodityTarget == null)
    //         {
    //             if (quantity == simpleCommoditySource.Quantity)
    //             {
    //                 // Target has the cargo capacity to hold the full quantity,
    //                 // and the target has no matching item instance;
    //                 // simply move the existing item instance to the target
    //                 source.Cargo.Remove(simpleCommoditySource.ID);
    //                 target.Cargo.Add(simpleCommoditySource.ID);
    //                 source.RecalculateMass();
    //                 target.RecalculateMass();
    //                 return success;
    //             }
    //             else
    //             {
    //                 // Target has the cargo capacity to hold the desired quantity
    //                 // and the target has no matching item instance;
    //                 // Create a new item instance on the target and decrement the quantity of the source instance
    //                 var newSimpleCommodity = new SimpleCommodity
    //                 {
    //                     Context = this,
    //                     Data = simpleCommodityData.ID,
    //                     Quantity = simpleCommoditySource.Quantity
    //                 };
    //                 Cache.Add(newSimpleCommodity);
    //                 target.Cargo.Add(newSimpleCommodity.ID);
    //                 simpleCommoditySource.Quantity -= quantity;
    //                 source.RecalculateMass();
    //                 target.RecalculateMass();
    //                 return success;
    //             }
    //         }
    //         else
    //         {
    //             if (quantity == simpleCommoditySource.Quantity)
    //             {
    //                 // Target has the cargo capacity to hold the full quantity,
    //                 // and there is already a matching item instance;
    //                 // delete the source entity's item instance and increment the target's quantity
    //                 source.Cargo.Remove(simpleCommoditySource.ID);
    //                 Cache.Delete(simpleCommoditySource);
    //                 simpleCommodityTarget.Quantity += quantity;
    //                 source.RecalculateMass();
    //                 target.RecalculateMass();
    //                 return success;
    //             }
    //             else
    //             {
    //                 // Target has the cargo capacity to hold the desired quantity,
    //                 // and there is already a matching item instance;
    //                 // decrement the source's quantity and increment the target's quantity
    //                 simpleCommoditySource.Quantity -= quantity;
    //                 simpleCommodityTarget.Quantity += quantity;
    //                 source.RecalculateMass();
    //                 target.RecalculateMass();
    //                 return success;
    //             }
    //         }
    //         
    //     }
    //     
    //     return false;
    // }
}