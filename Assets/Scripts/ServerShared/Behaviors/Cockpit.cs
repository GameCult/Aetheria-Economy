/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, JsonObject(MemberSerialization.OptIn), RuntimeInspectable]
public class CockpitData : BehaviorData
{
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new Cockpit(context, this, entity, item);
    }
}

public class Cockpit : IBehavior
{
    private CockpitData _data;

    public Entity Entity { get; }
    public EquippedItem Item { get; }
    public ItemManager Context { get; }

    public BehaviorData Data => _data;
    
    public float Heatstroke { get; private set; }

    public Cockpit(ItemManager context, CockpitData data, Entity entity, EquippedItem item)
    {
        Context = context;
        _data = data;
        Entity = entity;
        Item = item;
        Heatstroke = 0;
    }

    public bool Execute(float delta)
    {
        if (Item.Temperature > Context.GameplaySettings.HeatstrokeTemperature)
        {
            Heatstroke = saturate(
                Heatstroke +
                pow(Item.Temperature - Context.GameplaySettings.HeatstrokeTemperature, Context.GameplaySettings.HeatstrokeExponent) * 
                Context.GameplaySettings.HeatstrokeMultiplier * delta);
        }
        else
        {
            Heatstroke = saturate(Heatstroke - Context.GameplaySettings.HeatstrokeRecoverySpeed * delta);
        }
        return Heatstroke < Context.GameplaySettings.HeatstrokeControlLimit;
    }
}