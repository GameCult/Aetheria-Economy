/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class LauncherData : InstantWeaponData
{
    [InspectableAnimationCurve, JsonProperty("guidance"), Key(9)]  
    public float4[] GuidanceCurve;

    [InspectableAnimationCurve, JsonProperty("thrustCurve"), Key(10)]  
    public float4[] ThrustCurve;

    [InspectableAnimationCurve, JsonProperty("liftCurve"), Key(11)]  
    public float4[] LiftCurve;

    [InspectableField, JsonProperty("thrust"), Key(12)]  
    public PerformanceStat Thrust = new PerformanceStat();

    [InspectableField, JsonProperty("frequency"), Key(13)]  
    public float DodgeFrequency;

    [InspectableField, JsonProperty("launchSpeed"), Key(14)]  
    public PerformanceStat LaunchSpeed = new PerformanceStat();

    [InspectableField, JsonProperty("missileSpeed"), Key(15), RuntimeInspectable]  
    public PerformanceStat Velocity = new PerformanceStat();

    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new InstantWeapon(context, this, entity, item);
    }
}