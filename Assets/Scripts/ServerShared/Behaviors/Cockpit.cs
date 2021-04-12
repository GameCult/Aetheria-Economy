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
    public override IBehavior CreateInstance(EquippedItem item)
    {
        return new Cockpit(this, item);
    }
}

public class Cockpit : IBehavior
{
    private CockpitData _data;

    public EquippedItem Item { get; }

    public BehaviorData Data => _data;
    

    public Cockpit(CockpitData data, EquippedItem item)
    {
        _data = data;
        Item = item;
    }

    public bool Execute(float delta)
    {
        return true;
    }
}