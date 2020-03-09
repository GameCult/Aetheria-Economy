using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using JM.LinqFaster;
using MessagePack;
using MessagePack.Formatters;
using MIConvexHull;
using UniRx;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.UI.Extensions;
using Object = UnityEngine.Object;
// TODO: USE THIS EVERYWHERE
using static Unity.Mathematics.math;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

[MessagePackObject]
public class Ship
{
    [Key("hull")] 
    public Gear Hull;
    [Key("hardpoints")] 
    public List<Hardpoint> Hardpoints = new List<Hardpoint>();
    [Key("cargo")] 
    public List<IItemInstance> Cargo = new List<IItemInstance>();
    
    // [Key("bindings")]   public Dictionary<KeyCode,Guid>    Bindings = new Dictionary<KeyCode,Guid>();
    //[IgnoreMember] public int HullHardpointCount;
    [IgnoreMember] public Dictionary<Guid, IActivatedItemBehavior[]> ItemBindings;
    [IgnoreMember] public IItemBehavior[] ItemBehaviors;

    [IgnoreMember] public bool ForceThrust;
    //[IgnoreMember]      public Ship                        Ship;
    [IgnoreMember] public Dictionary<Targetable, float> Contacts = new Dictionary<Targetable, float>();
    [IgnoreMember] public float Temperature;
    [IgnoreMember] public float Charge;
    [IgnoreMember] public Targetable Target;
    [IgnoreMember] public Vector2 Direction;
    [IgnoreMember] public Dictionary<object, float> VisibilitySources = new Dictionary<object, float>();
    //[IgnoreMember] public float Visibility;
    
    private Hull _hullData;
    private bool _hydrated = false;

    [IgnoreMember] public Hull HullData => _hullData ?? (_hullData = Hull.EquippedItemData as Hull);

    [IgnoreMember] public float Mass { get; private set; }

    //[IgnoreMember] public float Visibility => VisibilitySources.Values.Sum();

    public Gear GetEquipped(HardpointType type)
    {
        return Hardpoints.FirstOrDefault(hp => hp.HardpointData.Type == type)?.Item;
    }

    public T GetEquipped<T>() where T : class, IEquippable
    {
        return Hardpoints.FirstOrDefault(hp => hp.Item?.EquippedItemData is T)?.Item.EquippedItemData as T;
    }

    public void AddHeat(float heat)
    {
        Temperature += heat / Mass;
    }

    public void Hydrate()
    {
        GenerateBehaviors();
        Mass = Hull.Mass + Hardpoints.Sum(hp => hp.Item?.Mass ?? 0) + Cargo.Sum(ii => ii.Mass);
        //HullHardpointCount = Hardpoints.Count(hp => hp.HardpointData.Type == HardpointType.Hull);
        _hydrated = true;
    }

    private void GenerateBehaviors()
    {
        ItemBehaviors = Hardpoints.Where(hp=>hp.Item!=null)
            .Where(hp=>hp.Item.EquippedItemData.Equippable.Behaviors?.Any()??false)
            .SelectMany(hp => hp.Item.EquippedItemData.Equippable.Behaviors
                .Select(bd => bd.CreateInstance(this, hp.Item)))
            .OrderBy(b => b.GetType().GetCustomAttribute<UpdateOrderAttribute>()?.Order ?? 0).ToArray();
        
        ItemBindings = ItemBehaviors
            .Where(b => b is IActivatedItemBehavior)
            .GroupBy(b => b.Item.GetId(), (guid, behaviors) => new {guid, behaviors})
            .ToDictionary(g => g.guid, g => g.behaviors.Cast<IActivatedItemBehavior>().ToArray());
    }

    // public void GenerateBindings()
    // {
    //     Bindings = new Dictionary<KeyCode, Guid>();
    //     
    //     var bindables =
    //         ItemBindings.Select(id => new {id=id.Key, eq = id.Value.First().Hardpoint.Item.EquippedItemData}).ToList();
    //
    //     var weapons = bindables.Where(i => i.eq.HardpointType.IsWeapon()).ToArray();
    //     
    //     var mouseButtons = Enumerable.Range((int) KeyCode.Mouse0, 3).Cast<KeyCode>();
    //     foreach (var x in weapons.Zip(mouseButtons, (binding, key) => new {binding,key}))
    //     {
    //         Bindings.Add(x.key, x.binding.id);
    //         bindables.Remove(x.binding);
    //     }
    //
    //     var thruster = bindables.FirstOrDefault(x => x.eq.HardpointType == HardpointType.Thruster);
    //     if (thruster != null)
    //     {
    //         Bindings.Add(KeyCode.LeftShift, thruster.id);
    //         bindables.Remove(thruster);
    //     }
    //
    //     var utility = bindables.FirstOrDefault(x => !x.eq.HardpointType.IsWeapon());
    //     if (utility != null)
    //     {
    //         Bindings.Add(KeyCode.Space, utility.id);
    //         bindables.Remove(utility);
    //     }
    //     
    //     var numKeys = Enumerable.Range((int) KeyCode.Alpha1, 9).Cast<KeyCode>();
    //     foreach (var x in bindables.Zip(numKeys, (binding, key) => new {binding,key}))
    //     {
    //         Bindings.Add(x.key, x.binding.id);
    //     }
    //
    // }

    // public void Activate(KeyCode binding)
    // {
    //     if (Bindings.ContainsKey(binding))
    //         foreach (var behavior in ItemBindings[Bindings[binding]])
    //         {
    //             behavior.Activate();
    //             if(behavior.Hardpoint.ActionBarButton!=null)
    //                 behavior.Hardpoint.ActionBarButton.Background.color = Color.yellow;
    //         }
    // }
    //
    // public void Deactivate(KeyCode binding)
    // {
    //     if(Bindings.ContainsKey(binding))
    //         foreach (var behavior in ItemBindings[Bindings[binding]])
    //         {
    //             behavior.Deactivate();
    //             if(behavior.Hardpoint.ActionBarButton!=null)
    //                 behavior.Hardpoint.ActionBarButton.Background.color = GlobalData.Instance.UnselectedElementColor;
    //         }
    // }

    public void FixedUpdate(float delta)
    {
        foreach (var kvp in Contacts.ToArray())
            if (kvp.Key == null || Time.time - kvp.Value > GlobalData.Instance.TargetPersistenceDuration)
            {
                Contacts.Remove(kvp.Key);
                if (kvp.Key == Target)
                    Target = null;
            }

        if (_hydrated)
        {
            var hull = Database.Get<Hull>(Hull.Data);
            
            var rad = pow(Temperature, GlobalData.Instance.HeatRadiationPower) * GlobalData.Instance.HeatRadiationMultiplier;
            Temperature -= rad * delta;
            VisibilitySources[this] = rad;
            
            foreach(var behavior in ItemBehaviors)
                behavior.FixedUpdate(delta);

            foreach (var hardpoint in Hardpoints)
            {
                if (hardpoint.Item.EquippedItemData.Performance(Temperature) < .01f)
                    hardpoint.Item.Durability -= delta;
            }
        }
    }

    public void Update(float delta)
    {
        foreach(var behavior in ItemBehaviors)
            behavior.Update(delta);
    }

    // public void UpdateInput(float delta)
    // {
    //     foreach (var key in Bindings.Keys)
    //     {
    //         if(Input.GetKeyDown(key))
    //             Activate(key);
    //         if(Input.GetKeyUp(key))
    //             Deactivate(key);
    //     }
    //
    // }
}

[MessagePackObject]
public class SimpleCommodity : IItemInstance
{
    [Key("data")]     public Guid Data;
    [Key("quantity")] public int  Quantity;
    [Key("entry")]    public DatabaseEntry DatabaseEntry = new DatabaseEntry();
    
    [IgnoreMember] private ItemData _item;
    [IgnoreMember] public Guid ItemGuid => Data;
    [IgnoreMember] public IItem ItemData => _item ?? (_item = Database.Get<ItemData>(Data));
    [IgnoreMember] public DatabaseEntry Entry => DatabaseEntry;
    [IgnoreMember] public float Mass => ItemData.Data.Mass * Quantity;

    public static SimpleCommodity CreateInstance(Guid data, int count)
    {
        var item = Database.Get<ItemData>(data);
        if (item != null)
            return new SimpleCommodity
            {
                Data = data,
                Quantity = count,
                DatabaseEntry = new DatabaseEntry
                {
                    ID = Guid.NewGuid(),
                    Name = item.Entry.Name
                }
            };
        
        Debug.LogError("Attempted to create Simple Commodity instance using missing or incorrect item id");
        return null;

    }
}

[MessagePackObject]
public class Gear : ICraftedItemInstance
{
    [Key("data")]        public Guid                Data;
    [Key("durability")]  public float               Durability;
    [Key("quality")]     public float               Quality;
    [Key("ingredients")] public List<IItemInstance> Ingredients = new List<IItemInstance>();
    [Key("entry")]    public DatabaseEntry DatabaseEntry = new DatabaseEntry();
    
    [IgnoreMember] private IEquippable _equippedItem;

    [IgnoreMember] public Guid ItemGuid => Data;

    [IgnoreMember] public IItem ItemData => EquippedItemData;
    [IgnoreMember] public DatabaseEntry Entry => DatabaseEntry;

    [IgnoreMember] public float CraftedQuality => Quality;
    [IgnoreMember] public IEnumerable<IItemInstance> CraftedIngredients => Ingredients;

    [IgnoreMember] public IEquippable EquippedItemData => _equippedItem ?? (_equippedItem = Database.Get<IEquippable>(Data));
    [IgnoreMember] public float Mass => ItemData.Data.Mass;

    public static Gear CreateInstance(Guid data)
    {
        var item = Database.Get<IEquippable>(data);
        if (item == null)
        {
            Debug.LogError("Attempted to create Gear instance using missing or incorrect item id");
            return null;
        }
        
        var equippable = item.Equippable;
        return new Gear
        {
            Data = data,
            Durability = equippable.Durability,
            Quality = Random.Range(.5f, 1),
            Ingredients = item.CraftingIngredients.SelectMany(ci =>
                {
                    var ingredient = Database.Get(ci.Key);
                    return ingredient is ItemData
                        ? (IEnumerable<IItemInstance>) new[] {SimpleCommodity.CreateInstance(ci.Key, ci.Value)}
                        : Enumerable.Range(0, ci.Value).Select(i => CompoundCommodity.CreateInstance(ci.Key));
                })
                .ToList(),
            DatabaseEntry = new DatabaseEntry
            {
                ID = Guid.NewGuid(),
                Name = item.Entry.Name
            }
        };
    }
}

[MessagePackObject]
public class CompoundCommodity : ICraftedItemInstance
{
    [Key("data")]        public Guid                Data;
    [Key("quality")]     public float               Quality;
    [Key("ingredients")] public List<IItemInstance> Ingredients = new List<IItemInstance>();
    [Key("entry")]       public DatabaseEntry DatabaseEntry = new DatabaseEntry();
    
    [IgnoreMember] private ICraftable _item;

    [IgnoreMember] public Guid ItemGuid => Data;

    [IgnoreMember] public IItem ItemData => _item ?? (_item = Database.Get<ICraftable>(Data));
    [IgnoreMember] public DatabaseEntry Entry => DatabaseEntry;

    [IgnoreMember] public float CraftedQuality => Quality;
    [IgnoreMember] public IEnumerable<IItemInstance> CraftedIngredients => Ingredients;
    [IgnoreMember] public float Mass => ItemData.Data.Mass;

    public static CompoundCommodity CreateInstance(Guid data)
    {
        var item = Database.Get<CraftedItem>(data);
        if (item != null)
            return new CompoundCommodity
            {
                Data = data,
                Quality = Random.Range(.5f, 1),
                Ingredients = item.CraftingIngredients.SelectMany(ci =>
                    {
                        var ingredient = Database.Get(ci.Key);
                        return ingredient is ItemData
                            ? (IEnumerable<IItemInstance>) new[] {SimpleCommodity.CreateInstance(ci.Key, ci.Value)}
                            : Enumerable.Range(0, ci.Value).Select(i => CreateInstance(ci.Key));
                    })
                    .ToList(),
                DatabaseEntry = new DatabaseEntry
                {
                    ID = Guid.NewGuid(),
                    Name = item.Entry.Name
                }
            };
        
        Debug.LogError("Attempted to create Compound Commodity instance using missing or incorrect item id");
        return null;

    }
}
