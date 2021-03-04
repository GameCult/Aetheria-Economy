/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject, JsonObject(MemberSerialization.OptIn), Order(1000), RuntimeInspectable]
public class WearData : BehaviorData
{
    [TemperatureInspectable, JsonProperty("perSecond"), Key(1)]
    public bool PerSecond;
    
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
        var wear = Context.Evaluate(_itemData.WearDamage, Item);
        if (_data.PerSecond) wear *= delta;
        Item.EquippableItem.Durability -= wear;
        return true;
    }
}