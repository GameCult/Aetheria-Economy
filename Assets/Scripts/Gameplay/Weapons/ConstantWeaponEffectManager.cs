using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ConstantWeaponEffectManager : MonoBehaviour
{
    public abstract void Start(WeaponData data, EquippedItem item, EntityInstance source, EntityInstance target);
    public abstract void Stop(WeaponData data, EquippedItem item, EntityInstance source, EntityInstance target);
}
