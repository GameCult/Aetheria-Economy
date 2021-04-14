/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, JsonObject(MemberSerialization.OptIn), RuntimeInspectable]
public class VelocityConversionData : BehaviorData
{
    [Inspectable, JsonProperty("traction"), Key(1), RuntimeInspectable]  
    public PerformanceStat Traction = new PerformanceStat();
    
    public override IBehavior CreateInstance(EquippedItem item)
    {
        return new VelocityConversion(this, item);
    }
}

public class VelocityConversion : IBehavior
{
    public EquippedItem Item { get; }

    public BehaviorData Data => _data;
    
    private VelocityConversionData _data;

    public VelocityConversion(VelocityConversionData data, EquippedItem item)
    {
        _data = data;
        Item = item;
    }

    public bool Execute(float dt)
    {
        return true;
    }
}