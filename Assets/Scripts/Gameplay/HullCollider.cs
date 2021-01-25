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
    
    public void SendHit(float damage, float penetration, float spread, DamageType damageType, Entity source, RaycastHit hit, Vector3 direction)
    {
        Hit.OnNext(new HullHitEventArgs
        {
            Damage = damage,
            Penetration = penetration,
            Spread = spread,
            DamageType = damageType,
            Source = source,
            Hit = hit,
            Direction = direction
        });
    }
    
    public class HullHitEventArgs
    {
        public float Damage;
        public float Penetration;
        public float Spread;
        public DamageType DamageType;
        public Entity Source;
        public RaycastHit Hit;
        public float3 Direction;
    }
}