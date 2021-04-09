/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn), Order(1000), RuntimeInspectable]
public class WearData : BehaviorData
{
    [InspectableTemperature, JsonProperty("perSecond"), Key(1)]
    public bool PerSecond = true;
    
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new Wear(context, this, entity, item);
    }
}

public class Wear : IBehavior
{
    private WearData _data;
    private EquippableItemData _itemData;

    public Entity Entity { get; }
    public EquippedItem Item { get; }
    public ItemManager Context { get; }

    public BehaviorData Data => _data;

    public Wear(ItemManager context, WearData data, Entity entity, EquippedItem item)
    {
        Context = context;
        _data = data;
        Entity = entity;
        Item = item;
        _itemData = context.GetData(item.EquippableItem);
    }

    public bool Execute(float delta)
    {
        if (_data.PerSecond)
            Item.EquippableItem.Durability -= Item.Wear * delta;
        else Item.EquippableItem.Durability -= Item.Wear;
        return true;
    }
}