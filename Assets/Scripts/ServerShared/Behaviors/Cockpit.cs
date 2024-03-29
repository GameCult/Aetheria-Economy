/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn), RuntimeInspectable]
public class CockpitData : BehaviorData
{
    public override Behavior CreateInstance(EquippedItem item)
    {
        return new Cockpit(this, item);
    }
    public override Behavior CreateInstance(ConsumableItemEffect item)
    {
        return new Cockpit(this, item);
    }
}

public class Cockpit : Behavior
{
    private CockpitData _data;
    
    public Cockpit(CockpitData data, EquippedItem item) : base(data, item)
    {
        _data = data;
    }
    
    public Cockpit(CockpitData data, ConsumableItemEffect item) : base(data, item)
    {
        _data = data;
    }
}