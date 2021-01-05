using System.Collections;
using System.Collections.Generic;
using UniRx;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using UnityEngine;
using float2 = Unity.Mathematics.float2;

public class HullCollider : MonoBehaviour
{
    public Subject<HullHitEventArgs> Hit = new Subject<HullHitEventArgs>();
    
    public Entity Entity { get; set; }
    
    public void SendHit(float damage, DamageType damageType, Entity source, RaycastHit hit)
    {
        Hit.OnNext(new HullHitEventArgs
        {
            Damage = damage,
            DamageType = damageType,
            Source = source,
            Hit = hit
        });
    }
    
    public class HullHitEventArgs
    {
        public float Damage;
        public DamageType DamageType;
        public Entity Source;
        public RaycastHit Hit;
    }
}