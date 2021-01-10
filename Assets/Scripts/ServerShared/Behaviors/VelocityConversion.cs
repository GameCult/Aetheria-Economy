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
    [InspectableField, JsonProperty("traction"), Key(1), RuntimeInspectable]  
    public PerformanceStat Traction = new PerformanceStat();
    
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new VelocityConversion(context, this, entity, item);
    }
}

public class VelocityConversion : IBehavior
{
    public Entity Entity { get; }
    public EquippedItem Item { get; }
    public ItemManager Context { get; }

    public BehaviorData Data => _data;
    
    private VelocityConversionData _data;

    public VelocityConversion(ItemManager context, VelocityConversionData data, Entity entity, EquippedItem item)
    {
        Context = context;
        _data = data;
        Entity = entity;
        Item = item;
    }

    public void Initialize()
    {
    }

    public bool Execute(float delta)
    {
        return true;
    }

    public void Remove()
    {
    }
}