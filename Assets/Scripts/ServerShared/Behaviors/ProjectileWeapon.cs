/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using MessagePack;
using Newtonsoft.Json;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class ProjectileWeaponData : InstantWeaponData
{
    [InspectableField, JsonProperty("spread"), Key(16), RuntimeInspectable]  
    public PerformanceStat Spread = new PerformanceStat();

    [InspectableField, JsonProperty("bulletInherit"), Key(17)]  
    public float Inherit;

    [InspectableField, JsonProperty("bulletVelocity"), Key(18), RuntimeInspectable]  
    public PerformanceStat Velocity = new PerformanceStat();

    [InspectableField, JsonProperty("auto"), Key(19)]  
    public bool Auto;
    
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return Auto ? new AutoWeapon(context, this, entity, item) : new InstantWeapon(context, this, entity, item);
    }
}