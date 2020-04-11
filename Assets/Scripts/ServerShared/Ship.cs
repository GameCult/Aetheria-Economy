using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MessagePack;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject]
public class Ship : Entity
{
    [Key("hull")] 
    public Gear Hull;
    [Key("hardpoints")] 
    public List<Hardpoint> Hardpoints = new List<Hardpoint>();
    
    // [Key("bindings")]   public Dictionary<KeyCode,Guid>    Bindings = new Dictionary<KeyCode,Guid>();
    //[IgnoreMember] public int HullHardpointCount;

    // [IgnoreMember] public Dictionary<Targetable, float> Contacts = new Dictionary<Targetable, float>();
    // [IgnoreMember] public Targetable Target;
    [IgnoreMember] public float2 Velocity;
    
    private HullData _hullData;

    
    public Ship(GameContext context, IEnumerable<Gear> items, IEnumerable<ItemInstance> cargo) : base(context, items, cargo)
    {
    }

    // public Gear GetEquipped(HardpointType type)
    // {
    //     return Hardpoints.FirstOrDefault(hp => hp.HardpointData.Type == type)?.Item;
    // }
    //
    // public T GetEquipped<T>() where T : EquippableItemData
    // {
    //     var data = Hardpoints.FirstOrDefault(hp => Context.GetData(hp.Item) is T);
    //     if (data != null)
    //         return Context.GetData(data.Item) as T;
    //     return null;
    // }
    //
    // public void Hydrate()
    // {
    //     GenerateBehaviors();
    //
    //     //HullHardpointCount = Hardpoints.Count(hp => hp.HardpointData.Type == HardpointType.Hull);
    // }
    //
    // private void GenerateBehaviors()
    // {
    // }

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

    public override void Update(float delta)
    {
        base.Update(delta);
        // foreach (var kvp in Contacts.ToArray())
        //     if (kvp.Key == null || Time.time - kvp.Value > GlobalData.Instance.TargetPersistenceDuration)
        //     {
        //         Contacts.Remove(kvp.Key);
        //         if (kvp.Key == Target)
        //             Target = null;
        //     }
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

