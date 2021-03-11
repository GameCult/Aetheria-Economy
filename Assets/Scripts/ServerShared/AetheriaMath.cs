using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public static class AetheriaMath
{
    public const float Deg2Rad = 0.01745329f;
    public const float Rad2Deg = 57.29578f;
    
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
    
    // http://csharphelper.com/blog/2016/09/find-the-shortest-distance-between-a-point-and-a-line-segment-in-c/
    // Calculate the distance between
    // point pt and the segment p1 --> p2.
    public static float FindDistanceToSegment(
        float2 pt, float2 p1, float2 p2, out float2 closest)
    {
        float dx = p2.x - p1.x;
        float dy = p2.y - p1.y;
        if (dx == 0 && dy == 0)
        {
            // It's a point not a line segment.
            closest = p1;
            dx = pt.x - p1.x;
            dy = pt.y - p1.y;
            return sqrt(dx * dx + dy * dy);
        }

        // Calculate the t that minimizes the distance.
        float t = ((pt.x - p1.x) * dx + (pt.y - p1.y) * dy) /
                  (dx * dx + dy * dy);

        // See if this represents one of the segment's
        // end points or a point in the middle.
        if (t < 0)
        {
            closest = float2(p1.x, p1.y);
            dx = pt.x - p1.x;
            dy = pt.y - p1.y;
        }
        else if (t > 1)
        {
            closest = float2(p2.x, p2.y);
            dx = pt.x - p2.x;
            dy = pt.y - p2.y;
        }
        else
        {
            closest = float2(p1.x + t * dx, p1.y + t * dy);
            dx = pt.x - closest.x;
            dy = pt.y - closest.y;
        }

        return sqrt(dx * dx + dy * dy);
    }
}
