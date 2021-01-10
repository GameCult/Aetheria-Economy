/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using MessagePack;
using Newtonsoft.Json;

[InspectableField, MessagePackObject, JsonObject(MemberSerialization.OptIn)]
public class ConstantWeaponData : WeaponData
{
    public override IBehavior CreateInstance(ItemManager context, Entity entity, EquippedItem item)
    {
        return new ConstantWeapon(context, this, entity, item);
    }
}

public class ConstantWeapon : IBehavior, IAlwaysUpdatedBehavior
{
    private ConstantWeaponData _data;
    
    private Entity Entity { get; }
    private EquippedItem Item { get; }
    private ItemManager Context { get; }
    
    public BehaviorData Data => _data;

    public event Action OnStartFiring;
    public event Action OnStopFiring;

    private bool _firing;
    private bool _stoppedFiring;

    public ConstantWeapon(ItemManager context, ConstantWeaponData c, Entity entity, EquippedItem item)
    {
        Context = context;
        _data = c;
        Entity = entity;
        Item = item;
    }

    public bool Execute(float delta)
    {
        if (!_firing)
        {
            _firing = true;
            OnStartFiring?.Invoke();
        }

        _stoppedFiring = false;
        return true;
    }

    public void Update(float delta)
    {
        // Update executes after AlwaysUpdate,
        // so if the stop flag didn't get disabled previous frame,
        // we know the weapon has stopped firing 
        if (_firing)
        {
            if (_stoppedFiring)
            {
                _firing = false;
                _stoppedFiring = false;
                OnStopFiring?.Invoke();
            }
            else
                _stoppedFiring = true;
        }
    }
}