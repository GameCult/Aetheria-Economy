using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LiteNetLib;
using MessagePack;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Random = Unity.Mathematics.Random;

public static class Extensions
{
    //public static IDatabaseEntry Get(this Guid entry) => Database.Get(entry);
    //public static T Get<T>(this Guid entry) where T : class, IDatabaseEntry => Database.Get(entry) as T;
    public static string ToJson(this DatabaseEntry entry) => MessagePackSerializer.SerializeToJson(entry);
    public static byte[] Serialize(this DatabaseEntry entry) => MessagePackSerializer.Serialize(entry);

    public static bool IsImplementationOf(this Type baseType, Type interfaceType)
    {
        return baseType.GetInterfaces().Any(interfaceType.Equals);
    }

    private static readonly Dictionary<CraftedItemInstance, float> Quality = new Dictionary<CraftedItemInstance, float>();
    public static float CompoundQuality(this CraftedItemInstance item)
    {
        if (Quality.ContainsKey(item)) return Quality[item];
		
        var quality = item.Quality;
			
        var craftedIngredients = item.Ingredients.Where(i => i is CraftedItemInstance).ToArray();
        if (craftedIngredients.Length > 0)
        {
            quality *= craftedIngredients.Cast<CraftedItemInstance>().Average(CompoundQuality);
        }

        Quality[item] = quality;

        return Quality[item];
    }

    public static void Send<T>(this NetPeer peer, T message, DeliveryMethod method = DeliveryMethod.ReliableOrdered) where T : Message
    {
        peer.Send(MessagePackSerializer.Serialize(message as Message), method);
    }

    public static float Performance(this EquippableItemData itemData, float temperature)
    {
        return saturate(itemData.HeatPerformanceCurve.Evaluate(saturate(
            (temperature - itemData.MinimumTemperature) /
            (itemData.MaximumTemperature - itemData.MinimumTemperature))));
    }
	
    public static string SplitCamelCase( this string str )
    {
        return Regex.Replace( 
            Regex.Replace( 
                str, 
                @"(\P{Ll})(\P{Ll}\p{Ll})", 
                "$1 $2" 
            ), 
            @"(\p{Ll})(\P{Ll})", 
            "$1 $2" 
        );
    }

    private static Random? _random;
    private static Random Random => (Random) (_random ?? (_random = new Random(1337)));
    public static T RandomElement<T>(this IEnumerable<T> enumerable) => enumerable.ElementAt(Random.NextInt(0, enumerable.Count()));
	
    private static Dictionary<Type,Type[]> InterfaceClasses = new Dictionary<Type, Type[]>();
    public static Type[] GetAllInterfaceClasses(this Type type)
    {
        if (InterfaceClasses.ContainsKey(type))
            return InterfaceClasses[type];
        return InterfaceClasses[type] = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(ass => ass.GetTypes()).Where(t => t.IsClass && t.GetInterfaces().Contains(type)).ToArray();
    }
	
    private static Dictionary<Type,Type[]> ChildClasses = new Dictionary<Type, Type[]>();
    public static Type[] GetAllChildClasses(this Type type)
    {
        if (ChildClasses.ContainsKey(type))
            return ChildClasses[type];
        return ChildClasses[type] = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(ass => ass.GetTypes()).Where(type.IsAssignableFrom).ToArray();
    }
	
    public static bool IsDefault<T>(this T value) where T : struct
    {
        bool isDefault = value.Equals(default(T));

        return isDefault;
    }

    private static Dictionary<float4[], int2> _cachedIndices = new Dictionary<float4[], int2>();
    public static float Evaluate(this float4[] curve, float time)
    {
        // Clamp time
        time = math.clamp(time, curve[0].x, curve[curve.Length - 1].x);
        
        FindSurroundingKeyframes(time, curve); 
        return HermiteInterpolate(time, curve[_cachedIndices[curve].x], curve[_cachedIndices[curve].y]);
    }

    static void FindSurroundingKeyframes(float time, float4[] curve)
    {
        // Check that time is within cached keyframe time
        if (_cachedIndices.ContainsKey(curve) && 
            _cachedIndices[curve].x != _cachedIndices[curve].y && 
            time >= curve[_cachedIndices[curve].x].x && 
            time <= curve[_cachedIndices[curve].y].x)
        {
            return;
        }

            
        // Fall back to using dichotomic search.
        var length = curve.Length;
        int half;
        int middle;
        int first = 0;

        while (length > 0)
        {
            half = length >> 1;
            middle = first + half;

            if (time < curve[middle].x)
            {
                length = half;
            }
            else
            {
                first = middle + 1;
                length = length - half - 1;
            }
        }

        // If not within range, we pick the last element twice.
        var indices = int2(first - 1, math.min(curve.Length - 1, first));
        _cachedIndices[curve] = indices;
    }

    static float HermiteInterpolate(float time, in float4 leftKeyframe, in float4 rightKeyframe)
    {
        // Handle stepped curve.
        if (math.isinf(leftKeyframe.w) || math.isinf(rightKeyframe.z))
        {
            return leftKeyframe.y;
        }

        float dx = rightKeyframe.x - leftKeyframe.x;
        float m0;
        float m1;
        float t;
        if (dx != 0.0f)
        {
            t = (time - leftKeyframe.x) / dx;
            m0 = leftKeyframe.w * dx;
            m1 = rightKeyframe.z * dx;
        }
        else
        {
            t = 0.0f;
            m0 = 0;
            m1 = 0;
        }

        return HermiteInterpolate(t, leftKeyframe.y, m0, m1, rightKeyframe.y);
    }

    static float HermiteInterpolate(float t, float p0, float m0, float m1, float p1)
    {
        // Unrolled the equations to avoid precision issue.
        // (2 * t^3 -3 * t^2 +1) * p0 + (t^3 - 2 * t^2 + t) * m0 + (-2 * t^3 + 3 * t^2) * p1 + (t^3 - t^2) * m1

        var a = 2.0f * p0 + m0 - 2.0f * p1 + m1;
        var b = -3.0f * p0 - 2.0f * m0 + 3.0f * p1 - m1;
        var c = m0;
        var d = p0;

        return t * (t * (a * t + b) + c) + d;
    }
}
