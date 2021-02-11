using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public static class AetheriaMath
{
    //first-order intercept using absolute target position
    public static float3 FirstOrderIntercept
    (
        float3 shooterPosition,
        float3 shooterVelocity,
        float shotSpeed,
        float3 targetPosition,
        float3 targetVelocity
    )  {
        float3 targetRelativePosition = targetPosition - shooterPosition;
        float3 targetRelativeVelocity = targetVelocity - shooterVelocity;
        float t = FirstOrderInterceptTime
        (
            shotSpeed,
            targetRelativePosition,
            targetRelativeVelocity
        );
        return targetPosition + t*targetRelativeVelocity;
    }
    
    //first-order intercept using relative target position
    public static float FirstOrderInterceptTime
    (
        float shotSpeed,
        float3 targetRelativePosition,
        float3 targetRelativeVelocity
    ) {
        float velocitySquared = lengthsq(targetRelativeVelocity);
        if(velocitySquared < 0.001f)
            return 0f;
 
        float a = velocitySquared - shotSpeed*shotSpeed;
 
        //handle similar velocities
        if (abs(a) < 0.001f)
        {
            float t = -lengthsq(targetRelativePosition)/ (2f*dot(targetRelativeVelocity, targetRelativePosition));
            return max(t, 0f); //don't shoot back in time
        }
 
        float b = 2f*dot(targetRelativeVelocity, targetRelativePosition);
        float c = lengthsq(targetRelativePosition);
        float determinant = b*b - 4f*a*c;
 
        if (determinant > 0f) { //determinant > 0; two intercept paths (most common)
            float	t1 = (-b + sqrt(determinant))/(2f*a),
                    t2 = (-b - sqrt(determinant))/(2f*a);
            if (t1 > 0f) {
                if (t2 > 0f)
                    return min(t1, t2); //both are positive
                else
                    return t1; //only t1 is positive
            } else
                return max(t2, 0f); //don't shoot back in time
        } else if (determinant < 0f) //determinant < 0; no intercept path
            return 0f;
        else //determinant = 0; one intercept path, pretty much never happens
            return max(-b/(2f*a), 0f); //don't shoot back in time
    }
}
