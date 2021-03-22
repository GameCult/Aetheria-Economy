/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

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
        GameplaySettings = settings;
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

    // Returns stat when not equipped
    public float Evaluate(PerformanceStat stat, EquippableItem item)
    {
        var data = GetData(item);
        var quality = pow(item.Quality, stat.QualityExponent);
        var durabilityExponent = lerp(
            GameplaySettings.DurabilityQualityMin,
            GameplaySettings.DurabilityQualityMax,
            pow(item.Quality, GameplaySettings.DurabilityQualityExponent));
        var durability = pow(item.Durability / data.Durability, durabilityExponent * stat.DurabilityExponentMultiplier);
        var result = lerp(stat.Min, stat.Max, quality * durability);
        if (float.IsNaN(result)) throw new InvalidOperationException("Performance Stat evaluating as NaN: input data is invalid!");
        return result;

    }

    public int GetPrice(CraftedItemInstance item)
    {
        var data = GetData(item);
        return (int) (GameplaySettings.QualityPriceModifier.Evaluate(item.Quality) * data.Price);
    }

    public SimpleCommodity CreateInstance(SimpleCommodityData item, int count)
    {
        if (item != null)
        {
            var newItem = new SimpleCommodity
            {
                Data = item.ID,
                Quantity = count
            };
            //ItemData.Add(newItem);
            return newItem;
        }
        
        _logger("Attempted to create Simple Commodity instance using missing or incorrect item id");
        return null;
    }

    public ItemInstance Instantiate(ItemInstance item)
    {
        var data = GetData(item);
        if(data is CraftedItemData c)
        {
            var i = CreateInstance(c);
            i.Rotation = item.Rotation;
            return i;
        }
        if (item is SimpleCommodity s)
        {
            var i = CreateInstance(data as SimpleCommodityData, s.Quantity);
            i.Rotation = item.Rotation;
            return i;
        }
        return null;
    }
    
    public CraftedItemInstance CreateInstance(CraftedItemData item)
    {
        if (item == null)
        {
            _logger("Attempted to create crafted item instance using missing or incorrect item data!");
            return null;
        }

        var quality = Random.NextFloat();
        var tier = GameplaySettings.Tiers[0];
        foreach (var t in GameplaySettings.Tiers)
        {
            if (t.Rarity > quality)
                tier = t;
        }
        
        if (item is EquippableItemData equippableItemData)
        {
            var newGear = new EquippableItem
            {
                Data = item.ID,
                Quality = tier.Quality
            };
            newGear.Durability = equippableItemData.Durability;
            return newGear;
        }

        var newCommodity = new CompoundCommodity
        {
            Data = item.ID,
            Quality = tier.Quality
        };
        return newCommodity;
    }

    public (RarityTier tier, int upgrades) GetTier(CraftedItemInstance item)
    {
        var tier = GameplaySettings.Tiers[0];
        foreach (var t in GameplaySettings.Tiers)
            if (item.Quality + .001f > t.Quality)
                tier = t;
        int upgrades = (int) ((item.Quality - tier.Quality) / .0499f);
        return (tier, upgrades);
    }
}