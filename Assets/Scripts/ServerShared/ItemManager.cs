using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;
using Random = Unity.Mathematics.Random;
using JM.LinqFaster;
using UniRx;
using float4 = Unity.Mathematics.float4;

public class ItemManager
{
    public Random Random = new Random((uint) (DateTime.Now.Ticks%uint.MaxValue));
    // public Dictionary<string, GalaxyMapLayerData> MapLayers = new Dictionary<string, GalaxyMapLayerData>();
    // public SimpleCommodityData[] Resources;
    // public Dictionary<Guid, List<IController>> CorporationControllers = new Dictionary<Guid, List<IController>>();
    // public Dictionary<Guid, ZoneDefinition> GalaxyZones;
    
    private Action<string> _logger;

    private Dictionary<BlueprintStatEffect, PerformanceStat> AffectedStats =
        new Dictionary<BlueprintStatEffect, PerformanceStat>();

    private double _time;
    private float _deltaTime;
    private Dictionary<Guid, Zone> _zones = new Dictionary<Guid, Zone>();
    // private Guid _forceLoadZone;
    
    // public GlobalData GlobalData => _globalData ?? (_globalData = ItemData.GetAll<GlobalData>().FirstOrDefault());
    public DatabaseCache ItemData { get; }
    public GameplaySettings GameplaySettings { get; }

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

    // private readonly Dictionary<CraftedItemData, int> Tier = new Dictionary<CraftedItemData, int>();

    public ItemManager(DatabaseCache itemData, GameplaySettings settings, Action<string> logger)
    {
        ItemData = itemData;
        _logger = logger;
    }

    public void Log(string s)
    {
        _logger(s);
    }

    // public void Update()
    // {
    //     foreach(var zone in _zones.Values)
    //         zone.Update((float) Time, _deltaTime);
    //     
    //     foreach (var corporation in Cache.GetAll<Corporation>())
    //     {
    //         foreach (var tasks in corporation.Tasks
    //             .Select(id => Cache.Get<AgentTask>(id)) // Fetch the tasks from the database cache
    //             .Where(task => !task.Reserved) // Filter out tasks that have already been reserved
    //             .GroupBy(task => task.Type)) // Group tasks by type
    //         {
    //             // Create a list of available controllers for this task type
    //             var availableControllers = CorporationControllers[corporation.ID]
    //                 .Where(controller => controller.Available && controller.TaskType == tasks.Key).ToList();
    //             
    //             // Iterate over the highest priority tasks for which controllers are available
    //             foreach (var task in tasks.OrderByDescending(task => task.Priority).Take(availableControllers.Count))
    //             {
    //                 // Find the nearest controller for this task
    //                 IController nearestController = availableControllers[0];
    //                 List<ZoneDefinition> nearestControllerPath = FindPath(GalaxyZones[availableControllers.First().Zone.Data.ID], GalaxyZones[task.Zone], true);
    //                 foreach (var controller in availableControllers.Skip(1))
    //                 {
    //                     var path = FindPath(GalaxyZones[controller.Zone.Data.ID], GalaxyZones[task.Zone], true);
    //                     if (path.Count < nearestControllerPath.Count)
    //                     {
    //                         nearestControllerPath = path;
    //                         nearestController = controller;
    //                     }
    //                 }
    //                 task.Reserved = true;
    //                 nearestController.AssignTask(task.ID);
    //             }
    //         }
    //     }
    //     
    // }

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
        return ItemData.Get<ItemData>(item.Data);
    }

    public SimpleCommodityData GetData(SimpleCommodity item)
    {
        return ItemData.Get<SimpleCommodityData>(item.Data);
    }

    public CraftedItemData GetData(CraftedItemInstance item)
    {
        return ItemData.Get<CraftedItemData>(item.Data);
    }

    public EquippableItemData GetData(EquippableItem equippableItem)
    {
        return ItemData.Get<EquippableItemData>(equippableItem.Data);
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
        var blueprintItem = ItemData.Get<EquippableItemData>(blueprint.Item);
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
            var ingredientQualityWeight = ItemData.Get<CraftedItemData>(item.Data).IngredientQualityWeight;
            quality = quality * (1 - ingredientQualityWeight) +
                      craftedIngredients.Cast<CraftedItemInstance>().Average(CompoundQuality) * ingredientQualityWeight;
        }

        ItemQuality[item] = quality;

        return ItemQuality[item];
    }

    // Determine quality of either the item itself or the specific ingredient this stat depends on
    public float Quality(PerformanceStat stat, EquippableItem item)
    {
        var itemData = GetData(item);
        var blueprint = ItemData.Get<BlueprintData>(item.Blueprint);
        var activeEffects = blueprint.StatEffects.Where(x => GetAffectedStat(blueprint, x) == stat).ToArray();
        float quality;
        if (!activeEffects.Any())
            quality = CompoundQuality(item);
        else
        {
            var ingredients = item.Ingredients.Where(i => activeEffects.Any(e => e.Ingredient == i.Data)).ToArray();
            var distinctIngredients = ingredients.Select(i => i.Data).Distinct();
            if(distinctIngredients.Count() != activeEffects.Length)
            {
                _logger($"Item {itemData.Name} does not have the ingredients specified by the stat effects of its blueprint!");
                return 0;
            }

            float sum = 0;
            foreach (var i in ingredients)
            {
                if (i is CraftedItemInstance ci)
                    sum += CompoundQuality(ci);
                else _logger($"Blueprint stat effect for item {itemData.Name} specifies invalid (non crafted) ingredient!");
            }

            quality = sum / ingredients.Length;
        }

        return quality;
    }

    // Returns stat when not equipped
    public float Evaluate(PerformanceStat stat, EquippableItem item)
    {
        var quality = pow(Quality(stat, item), stat.QualityExponent);

        var result = lerp(stat.Min, stat.Max, quality);
        
        if (float.IsNaN(result))
            return stat.Min;
        
        return result;
    }

    // Returns stat using ship temperature and modifiers
    public float Evaluate(PerformanceStat stat, EquippableItem item, Entity entity)
    {
        var itemData = GetData(item);
    
        //var heat = !stat.HeatDependent ? 1 : pow(itemData.Performance(entity.Temperature), Evaluate(itemData.HeatExponent,item));
        var durability = !stat.DurabilityDependent ? 1 : pow(item.Durability / itemData.Durability, Evaluate(itemData.DurabilityExponent,item));
        var quality = pow(Quality(stat, item), stat.QualityExponent);
    
        var scaleModifier = stat.GetScaleModifiers(entity).Values.Aggregate(1.0f, (current, mod) => current * mod);
    
        var constantModifier = stat.GetConstantModifiers(entity).Values.Sum();
    
        var result = lerp(stat.Min, stat.Max, durability * quality) * scaleModifier + constantModifier;
        if (float.IsNaN(result))
            return stat.Min;
        return result;
    }

    // public Entity CreateEntity(Guid zoneID, Guid corporation, Guid loadout)
    // {
    //     if (!(ItemData.Get(loadout) is LoadoutData loadoutData))
    //     {
    //         _logger("Attempted to spawn invalid loadout ID");
    //         return null;
    //     }
    //
    //     if (!(ItemData.Get(loadoutData.Hull) is HullData hullData))
    //     {
    //         _logger("Attempted to spawn loadout with invalid hull ID");
    //         return null;
    //     }
    //
    //     if (!(ItemData.Get(zoneID) is ZoneData zoneData))
    //     {
    //         _logger("Attempted to spawn entity with invalid zone ID");
    //         return null;
    //     }
    //
    //     if (!(ItemData.Get(corporation) is Corporation corpData))
    //     {
    //         _logger("Attempted to spawn entity with invalid corporation ID");
    //         return null;
    //     }
    //
    //     var gearData = loadoutData.Gear
    //         .Take(hullData.Hardpoints.Count)
    //         .Where(id => id != Guid.Empty)
    //         .Select(id => ItemData.Get<GearData>(id))
    //         .ToArray();
    //
    //     if (gearData.Any(g=>g==null))
    //     {
    //         _logger("Attempted to spawn loadout with invalid gear ID");
    //         return null;
    //     }
    //
    //     var gear = gearData
    //         .Select(gd => CreateInstance(gd.ID, GlobalData.StartingGearQualityMin, GlobalData.StartingGearQualityMax))
    //         .ToArray();
    //
    //     if (gear.Any(g=>g==null))
    //         return null;
    //     
    //     foreach(var g in gear)
    //         ItemData.Add(g);
    //
    //     var cargoData = loadoutData.CompoundCargo
    //         .SelectMany(x => Enumerable.Repeat(ItemData.Get<CraftedItemData>(x.Key), x.Value))
    //         .ToArray();
    //
    //     if (cargoData.Any(g=>g==null))
    //     {
    //         _logger("Attempted to spawn loadout with invalid cargo ID");
    //         return null;
    //     }
    //
    //     var cargo = cargoData
    //         .Select(gd => CreateInstance(gd.ID, GlobalData.StartingGearQualityMin, GlobalData.StartingGearQualityMax))
    //         .ToArray();
    //
    //     if (cargo.Any(g=>g==null))
    //         return null;
    //     
    //     foreach(var c in cargo)
    //         ItemData.Add(c);
    //
    //     var simpleCargo = loadoutData.SimpleCargo?
    //         .Select(x => CreateInstance(x.Key, x.Value))
    //         .ToArray() ?? new SimpleCommodity[0];
    //
    //     if (simpleCargo.Any(g=>g==null))
    //         return null;
    //     
    //     var hull = CreateInstance(loadoutData.Hull, GlobalData.StartingGearQualityMin, GlobalData.StartingGearQualityMax);
    //     
    //     if(hull==null)
    //         return null;
    //
    //     // Get target zone
    //     var zone = _zones[zoneID];
    //
    //     if (hullData.HullType == HullType.Ship)
    //     {
    //         var entity = CreateShip(hull.ID, gear.Select(g => g.ID),
    //             cargo.Select(c => c.ID).Concat(simpleCargo.Select(c => c.ID)), zone, corporation, Guid.Empty, loadoutData.Name);
    //         entity.Active = true;
    //         return entity;
    //     }
    //
    //     if (hullData.HullType == HullType.Station)
    //     {
    //         var distance = lerp(.2f, .8f, Random.NextFloat()) * zoneData.Radius;
    //         var orbit = new OrbitData
    //         {
    //             Context = this,
    //             Distance = new ReactiveProperty<float>(distance),
    //             Parent = zone.Orbits.Values.Select(o=>o.Data)
    //                 .First(orbitData => orbitData.Parent == Guid.Empty).ID,
    //             Phase = Random.NextFloat()
    //         };
    //         zone.AddOrbit(orbit);
    //         
    //         var entity = new OrbitalEntity(this, hull.ID, gear.Select(g => g.ID),
    //             cargo.Select(c => c.ID).Concat(simpleCargo.Select(c => c.ID)), orbit.ID, zone, corporation)
    //         {
    //             Name = $"{loadoutData.Name} {Random.NextInt(1,255):X}",
    //             Temperature = 293,
    //             Population = 4
    //         };
    //         entity.Active = true;
    //         var parentCorp = ItemData.Get<MegaCorporation>(corpData.Parent);
    //         foreach (var attribute in parentCorp.Personality)
    //             entity.Personality[attribute.Key] = attribute.Value;
    //         ItemData.Add(entity);
    //
    //         zone.Entities[entity.ID] = entity;
    //         return entity;
    //     }
    //
    //     return null;
    // }
    //
    // public Ship CreateShip(Guid hull, IEnumerable<Guid> gear, IEnumerable<Guid> cargo, Zone zone, Guid corporation, Guid homeEntity, string typeName)
    // {
    //
    //     var ship = new Ship(this, hull, gear, cargo, zone, corporation)
    //     {
    //         Name = $"{typeName} {Random.NextInt(1,255):X}",
    //         HomeEntity = homeEntity
    //     };
    //     ItemData.Add(ship);
    //
    //     zone.Entities[ship.ID] = ship;
    //     return ship;
    // }
    
    public SimpleCommodity CreateInstance(Guid data, int count)
    {
        var item = ItemData.Get<SimpleCommodityData>(data);
        if (item != null)
        {
            var newItem = new SimpleCommodity
            {
                Data = data,
                Quantity = count
            };
            //ItemData.Add(newItem);
            return newItem;
        }
        
        _logger("Attempted to create Simple Commodity instance using missing or incorrect item id");
        return null;
    }
    
    public CraftedItemInstance CreateInstance(Guid data, float qualityMin, float qualityMax)
    {
        var item = ItemData.Get<CraftedItemData>(data);
        if (item == null)
        {
            _logger("Attempted to create crafted item instance using missing or incorrect item id!");
            return null;
        }

        var blueprint = ItemData.GetAll<BlueprintData>().FirstOrDefault(b => b.Item == data);
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
            var itemData = ItemData.Get<ItemData>(ingredient.Key);
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
        
        if (item is EquippableItemData equippableItemData)
        {
            var newGear = new EquippableItem
            {
                Data = data,
                Ingredients = ingredients,
                Quality = Random.NextFloat(qualityMin, qualityMax),
                Blueprint = blueprint.ID,
                Name = $"{item.Name}"
            };
            newGear.Durability = equippableItemData.Durability;
            return newGear;
        }

        var newCommodity = new CompoundCommodity
        {
            Data = data,
            Ingredients = ingredients,
            Quality = Random.NextFloat(qualityMin, qualityMax),
            Blueprint = blueprint.ID,
            Name = $"{item.Name}"
        };
        return newCommodity;
    }
    
    // class DijkstraNode
    // {
    //     public float Cost;
    //     public DijkstraNode Parent;
    //     public ZoneDefinition Zone;
    // }
	   //
    // public List<ZoneDefinition> FindPath(ZoneDefinition source, ZoneDefinition target, bool bestFirst = false)
    // {
    //     SortedList<float,DijkstraNode> members = new SortedList<float,DijkstraNode>{{0,new DijkstraNode{Zone = source}}};
    //     List<DijkstraNode> searched = new List<DijkstraNode>();
    //     while (true)
    //     {
    //         var s = members.FirstOrDefault(m => !searched.Contains(m.Value)).Value; // Lowest cost unsearched node
    //         if (s == null) return null; // No vertices left unsearched
    //         if (s.Zone == target) // We found the path
    //         {
    //             Stack<DijkstraNode> path = new Stack<DijkstraNode>(); // Since we start at the end, use a LIFO collection
    //             path.Push(s);
    //             while(path.Peek().Parent!=null) // Keep pushing until we reach the start, which has no parent
    //                 path.Push(path.Peek().Parent);
    //             return path.Select(dv => dv.Zone).ToList();
    //         }
    //         // For each adjacent star (filter already visited zones unless heuristic is in use)
    //         foreach (var dijkstraStar in s.Zone.Links.WhereSelectF(i => !bestFirst || members.All(m => m.Value.Zone != GalaxyZones[i]),
    //                 // Cost is parent cost plus distance
    //                 i => new DijkstraNode {Parent = s, Zone = GalaxyZones[i], Cost = s.Cost + length(s.Zone.Position - GalaxyZones[i].Position)}))
    //             // Add new member to list, sorted by cost plus optional heuristic
    //             members.Add(bestFirst ? dijkstraStar.Cost + length(dijkstraStar.Zone.Position - target.Position) : dijkstraStar.Cost, dijkstraStar);
    //         searched.Add(s);
    //     }
    // }

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