/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;

[Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn), Order(-25)]
public class SwitchData : BehaviorData
{
    public override IBehavior CreateInstance(EquippedItem item)
    {
        return new Switch(this, item);
    }
}

public class Switch : IBehavior
{
    private SwitchData _data;

    private EquippedItem Item { get; }

    public BehaviorData Data => _data;

    public bool Activated { get; set; }

    public Switch(SwitchData data, EquippedItem item)
    {
        _data = data;
        Item = item;
    }

    public bool Execute(float delta)
    {
        return Activated;
    }
}