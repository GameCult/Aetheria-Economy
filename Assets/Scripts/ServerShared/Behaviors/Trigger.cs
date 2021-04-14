/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;

[Inspectable, MessagePackObject, JsonObject(MemberSerialization.OptIn), Order(-20)]
public class TriggerData : BehaviorData
{
    public override IBehavior CreateInstance(EquippedItem item)
    {
        return new Trigger(this, item);
    }
}

public class Trigger : IBehavior, IActivatedBehavior
{
    private TriggerData _data;

    public EquippedItem Item { get; }

    public BehaviorData Data => _data;

    public bool _pulled;

    public Trigger(TriggerData data, EquippedItem item)
    {
        _data = data;
        Item = item;
    }

    public bool Execute(float dt)
    {
        if (_pulled)
        {
            _pulled = false;
            return true;
        }

        return false;
    }

    public void Activate()
    {
        _pulled = true;
    }

    public void Deactivate()
    {
    }
}