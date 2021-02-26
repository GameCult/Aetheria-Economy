using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using UnityEngine;

public class ShieldManager : MonoBehaviour
{
    public Prototype ShieldPrototype;
    public float CollisionHitDuration = 3;
    
    public Entity Entity { get; set; }

    private void OnCollisionEnter(Collision other)
    {
        var otherShield = other.collider.GetComponent<ShieldManager>();
        if (!otherShield)
        {
            Debug.Log("Shield collision occurred, but other collider isn't a shield!");
            return;
        }
        var contact = other.GetContact(0);
        var normal = normalize(float2(contact.normal.x, contact.normal.z));
        var tangent = normal.Rotate(ItemRotation.CounterClockwise);
        var v1n = dot(normal, Entity.Velocity);
        var v1t = dot(tangent, Entity.Velocity);
        var v2n = dot(normal, otherShield.Entity.Velocity);
        //var v2t = dot(tangent, otherShield.Entity.Velocity);

        var v1np = PostCollisionVelocity(v1n, Entity.Mass, v2n, otherShield.Entity.Mass);
        Entity.Velocity = tangent * v1t + normal * v1np;
        if(Entity.ShieldEnabled) ShowHit(contact.point, CollisionHitDuration);
    }

    private float PostCollisionVelocity(float v1, float m1, float v2, float m2)
    {
        return (v1 * (m1 - m2) + m2 * v2) / (m1 + m2);
    }

    public void ShowHit(Vector3 point, float duration)
    {
        var shield = ShieldPrototype.Instantiate<ShieldAnimation>();
        shield.Direction = shield.transform.InverseTransformPoint(point);
        shield.Duration = duration;
    }
}
