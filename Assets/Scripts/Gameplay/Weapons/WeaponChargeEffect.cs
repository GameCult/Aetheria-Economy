using UnityEngine;

public abstract class WeaponChargeEffect : MonoBehaviour
{
    public ChargedWeapon Weapon { get; set; }
    
    public abstract void StopCharging();
    public abstract void Charged();
    public abstract void Failed();
}