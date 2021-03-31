using System;
using System.Collections;
using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn), RuntimeInspectable]
public class AutoWeaponData : InstantWeaponData
{
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new AutoWeapon(context, this, entity, item);
    }
}

public class AutoWeapon : InstantWeapon
{
    
    public AutoWeapon(ItemManager context, InstantWeaponData data, Entity entity, EquippedItem item) : base(context, data, entity, item)
    {
    }

    public override bool Execute(float delta)
    {
        if(_firing && _burstRemaining == 0 && _cooldown < 0)
            Trigger();
        return base.Execute(delta);
    }
}

