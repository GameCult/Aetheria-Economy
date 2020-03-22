using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MessagePack;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject]
public class Ship : DatabaseEntry
{
    [Key("hull")] 
    public Gear Hull;
    [Key("hardpoints")] 
    public List<Hardpoint> Hardpoints = new List<Hardpoint>();
    [Key("cargo")] 
    public List<ItemInstance> Cargo = new List<ItemInstance>();
    
    // [Key("bindings")]   public Dictionary<KeyCode,Guid>    Bindings = new Dictionary<KeyCode,Guid>();
    //[IgnoreMember] public int HullHardpointCount;
    [IgnoreMember] public Dictionary<Guid, IActivatedItemBehavior[]> ItemBindings;
    [IgnoreMember] public IItemBehavior[] ItemBehaviors;

    [IgnoreMember] public bool ForceThrust;
    // [IgnoreMember] public Dictionary<Targetable, float> Contacts = new Dictionary<Targetable, float>();
    [IgnoreMember] public float Temperature;
    [IgnoreMember] public float Charge;
    // [IgnoreMember] public Targetable Target;
    [IgnoreMember] public float2 Direction;
    [IgnoreMember] public Dictionary<object, float> VisibilitySources = new Dictionary<object, float>();
    
    private HullData _hullData;
    private bool _hydrated = false;

    [IgnoreMember] public float Mass { get; private set; }
    [IgnoreMember] public float SpecificHeat { get; private set; }

    //[IgnoreMember] public float Visibility => VisibilitySources.Values.Sum();

    public Gear GetEquipped(HardpointType type)
    {
        return Hardpoints.FirstOrDefault(hp => hp.HardpointData.Type == type)?.Item;
    }

    public T GetEquipped<T>() where T : EquippableItemData
    {
        var data = Hardpoints.FirstOrDefault(hp => Context.GetData(hp.Item) is T);
        if (data != null)
            return Context.GetData(data.Item) as T;
        return null;
    }

    public void AddHeat(float heat)
    {
        Temperature += heat / SpecificHeat;
    }

    public void Hydrate()
    {
        GenerateBehaviors();
        
        Mass = Hull.Mass + 
               Hardpoints.Sum(hp => hp.Item?.Mass ?? 0) + 
               Cargo.Sum(ii => ii.Mass);
        
        SpecificHeat = Hull.HeatCapacity +
               Hardpoints.Sum(hp => hp.Item?.HeatCapacity ?? 0) +
               Cargo.Sum(ii => ii.HeatCapacity);

        //HullHardpointCount = Hardpoints.Count(hp => hp.HardpointData.Type == HardpointType.Hull);
        _hydrated = true;
    }

    private void GenerateBehaviors()
    {
        ItemBehaviors = Hardpoints.Where(hp=>hp.Item!=null)
            .Where(hp=>hp.Item.ItemData.Behaviors?.Any()??false)
            .SelectMany(hp => hp.Item.ItemData.Behaviors
                .Select(bd => bd.CreateInstance(Context, this, hp.Item)))
            .OrderBy(b => b.GetType().GetCustomAttribute<UpdateOrderAttribute>()?.Order ?? 0).ToArray();
        
        ItemBindings = ItemBehaviors
            .Where(b => b is IActivatedItemBehavior)
            .GroupBy(b => b.Item.ID, (guid, behaviors) => new {guid, behaviors})
            .ToDictionary(g => g.guid, g => g.behaviors.Cast<IActivatedItemBehavior>().ToArray());
    }

    // public void GenerateBindings()
    // {
    //     Bindings = new Dictionary<KeyCode, Guid>();
    //     
    //     var bindables =
    //         ItemBindings.Select(id => new {id=id.Key, eq = id.Value.First().Hardpoint.Item.ItemData}).ToList();
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
        // foreach (var kvp in Contacts.ToArray())
        //     if (kvp.Key == null || Time.time - kvp.Value > GlobalData.Instance.TargetPersistenceDuration)
        //     {
        //         Contacts.Remove(kvp.Key);
        //         if (kvp.Key == Target)
        //             Target = null;
        //     }

        if (_hydrated)
        {
            var hull = Hull.ItemData;
            
            var rad = pow(Temperature, Context.GlobalData.HeatRadiationPower) * Context.GlobalData.HeatRadiationMultiplier;
            Temperature -= rad * delta;
            VisibilitySources[this] = rad;
            
            foreach(var behavior in ItemBehaviors)
                behavior.FixedUpdate(delta);

            foreach (var hardpoint in Hardpoints)
            {
                if (hardpoint.Item.ItemData.Performance(Temperature) < .01f)
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