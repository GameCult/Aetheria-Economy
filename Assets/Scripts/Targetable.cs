using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Unity.Mathematics.math;

public class Targetable : MonoBehaviour
{
    public float Points = 100;
    public float Visibility;
    public Transform ReplaceWithPrefab;
    public Ship Ship;

    public IsFriendlyFoeStatus DefaultIsFriendlyFoeStatus = IsFriendlyFoeStatus.None;

    public readonly Dictionary<Targetable, IsFriendlyFoeStatus> IFF = new Dictionary<Targetable, IsFriendlyFoeStatus>();
    private Vector3 _lastPosition;

    public IsFriendlyFoeStatus IsFriendlyFoe(Targetable target)
    {
        if (!IFF.ContainsKey(target))
            IFF[target] = DefaultIsFriendlyFoeStatus;

        return IFF[target];
    }

    void Start()
    {
    }

    void Health(float health)
    {
        Points = health;
    }

    public void Damage(float points)
    {
        if (Ship != null)
        {
            var shield = Ship.GetEquipped(HardpointType.Shield);
            if (shield != null)
            {
                var shieldData = shield.ItemData as ShieldData;
                var shieldedDamage = points * saturate(Ship.Context.Evaluate(shieldData.Shielding, shield, Ship));
                Ship.AddHeat(shieldedDamage / Ship.Context.Evaluate(shieldData.Efficiency, shield, Ship));
                Ship.Hull.Durability -= points - shieldedDamage;
            }
            else Ship.Hull.Durability -= points;
        }
        else
        {
            Points -= points;

            if (Points > 0) return;
        
            KillLocal();
        }
        /*
        var ent = GameManager.Instance.Entities.FirstOrDefault(kv => kv.Value == transform).Key;
        if(ent>0)
            GameManager.Instance.Send("Destroy", ent);*/

    }

    public void KillLocal()
    {
        //GameManager.Instance.Kill(this);
//        if (GetComponent<PlayerShip>())
//        {
//            GameManager.Instance.KillPlayer();
//        }
        
        Destroy(gameObject);

        if (ReplaceWithPrefab != null)
            Instantiate(ReplaceWithPrefab, transform.position, transform.rotation);
    }

    private void LateUpdate()
    {
        _lastPosition = transform.position;
    }

    void kill(string[] args)
    {
        if(args[0]==gameObject.name)
            KillLocal();
    }
}


public enum IsFriendlyFoeStatus
{
    None,
    Friendly,
    Hostile,
    Mayday,
    Body,
    Wormhole
}