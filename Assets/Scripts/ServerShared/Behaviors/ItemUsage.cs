/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;

[Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn), Order(-5), RuntimeInspectable]
public class ItemUsageData : BehaviorData
{
    [InspectableDatabaseLink(typeof(SimpleCommodityData)), JsonProperty("item"), Key(1), RuntimeInspectable]  
    public Guid Item;
    
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new ItemUsage(context, this, entity, item);
    }
}

public class ItemUsage : IBehavior
{
    private ItemUsageData _data;

    private Entity Entity { get; }
    private EquippedItem Item { get; }
    private ItemManager Context { get; }

    public BehaviorData Data => _data;

    public ItemUsage(ItemManager context, ItemUsageData data, Entity entity, EquippedItem item)
    {
        _data = data;
        Entity = entity;
        Item = item;
        Context = context;
    }

    public bool Execute(float delta)
    {
        var cargo = Entity.FindItemInCargo(_data.Item);
        if (cargo == null) return false;

        var item = cargo.ItemsOfType[_data.Item][0];
        if (item is SimpleCommodity simpleCommodity)
            cargo.Remove(simpleCommodity, 1);
        if (item is CraftedItemInstance craftedItemInstance)
            cargo.Remove(craftedItemInstance);
        
        return true;
    }
}