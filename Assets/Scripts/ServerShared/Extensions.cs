﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using LiteNetLib;
using MessagePack;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Random = Unity.Mathematics.Random;
using Unity.Tiny;
using float2 = Unity.Mathematics.float2;

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

    public static void Send<T>(this NetPeer peer, T message, DeliveryMethod method = DeliveryMethod.ReliableOrdered) where T : Message
    {
        peer.Send(MessagePackSerializer.Serialize(message as Message), method);
    }

    public static T[] WeightedRandomElements<T>(this IEnumerable<T> collection, ref Random random, Func<T, float> weightFunction, int count)
    {
        var weights = new Dictionary<T, float>(collection.Count());
        var totalWeight = 0f;
        foreach (var x in collection)
        {
            weights[x] = weightFunction(x);
            totalWeight += weights[x];
        }

        var elements = new T[count];
        for (int i = 0; i < count; i++)
        {
            var targetWeight = random.NextFloat(totalWeight);
            var accumWeight = 0f;
            foreach (var x in collection)
            {
                accumWeight += weights[x];
                if (accumWeight > targetWeight)
                {
                    elements[i] = x;
                    break;
                }
            }
        }

        return elements;
    }
    
    // Thanks, https://stackoverflow.com/a/48599119
    public static bool ByteArrayCompare(ReadOnlySpan<byte> a1, ReadOnlySpan<byte> a2)
    {
        return a1.SequenceEqual(a2);
    }

    public static bool ByteEquals(this byte[] a, byte[] b) => ByteArrayCompare(a, b);

    public static char Arrow(this ItemRotation rot)
    {
        switch (rot)
        {
            case ItemRotation.None:
                return '\u2191';
            case ItemRotation.Clockwise:
                return '\u2192';
            case ItemRotation.Reversed:
                return '\u2193';
            case ItemRotation.CounterClockwise:
                return '\u2190';
            default:
                throw new ArgumentOutOfRangeException(nameof(rot), rot, null);
        }
    }

    public static float2 Direction(this ItemRotation rotation)
    {
        switch (rotation)
        {
            case ItemRotation.None:
                return float2(0, 1);
            case ItemRotation.CounterClockwise:
                return float2(-1, 0);
            case ItemRotation.Reversed:
                return float2(0, -1);
            case ItemRotation.Clockwise:
                return float2(1, 0);
            default:
                throw new ArgumentOutOfRangeException(nameof(rotation), rotation, null);
        }
    }

    public static float2 Rotate(this float2 v, ItemRotation rotation)
    {
        switch (rotation)
        {
            case ItemRotation.None:
                return v;
            case ItemRotation.CounterClockwise:
                return float2(-v.y, v.x);
            case ItemRotation.Reversed:
                return float2(-v.x, -v.y);
            case ItemRotation.Clockwise:
                return float2(v.y, -v.x);
            default:
                throw new ArgumentOutOfRangeException(nameof(rotation), rotation, null);
        }
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
    
    public static string FormatTypeName(this string typeName)
    {
        return (typeName.EndsWith("Data")
            ? typeName.Substring(0, typeName.Length - 4)
            : typeName).SplitCamelCase();
    }
    
    public static string SignificantDigits(this float d, int digits=10)
    {
        int magnitude = d == 0.0f ? 0 : (int)Math.Floor(Math.Log10(Math.Abs(d))) + 1;
        digits -= magnitude;
        if (digits < 0)
            digits = 0;
        string fmt = "f" + digits;
        string strdec = d.ToString(fmt);
        return strdec.Contains(".") ? strdec.TrimEnd('0').TrimEnd('.') : strdec;
    }

    private static Random? _random;
    //private static Random Random => (Random) (_random ??= new Random((uint) (DateTime.Now.Ticks%uint.MaxValue)));
    // public static T RandomElement<T>(this IEnumerable<T> enumerable) => enumerable.ElementAt(Random.NextInt(0, enumerable.Count()));
    public static float NextPowerDistribution(this ref Random random, float min, float max, float exp, float randexp) =>
        pow((pow(max, exp + 1) - pow(min, exp + 1)) * pow(random.NextFloat(), randexp) + pow(min, exp + 1), 1 / (exp + 1));
    public static float NextUnbounded(this ref Random random) => 1 / (1 - random.NextFloat()) - 1;
    public static float NextUnbounded(this ref Random random, float bias, float power, float ceiling) => 1 / (1 - pow(min(random.NextFloat(), ceiling), 1 - pow(clamp(bias,0,.99f), 1 / power))) - 1;
	
    private static Dictionary<Type,Type[]> InterfaceClasses = new Dictionary<Type, Type[]>();
    public static Type[] GetAllInterfaceClasses(this Type type)
    {
        if (InterfaceClasses.ContainsKey(type))
            return InterfaceClasses[type];
        return InterfaceClasses[type] = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(ass => ass.GetTypes()).Where(t => t.IsClass && t.GetInterfaces().Contains(type)).ToArray();
    }
    
    private static Dictionary<Type,Type[]> ParentTypes = new Dictionary<Type, Type[]>();

    public static Type[] GetParentTypes(this Type type)
    {
        if (ParentTypes.ContainsKey(type))
            return ParentTypes[type];
        return ParentTypes[type] = type.GetParents().ToArray();
    }
    
    private static IEnumerable<Type> GetParents(this Type type)
    {
        // is there any base type?
        if (type == null)
        {
            yield break;
        }

        yield return type;

        // return all implemented or inherited interfaces
        foreach (var i in type.GetInterfaces())
        {
            yield return i;
        }

        // return all inherited types
        var currentBaseType = type.BaseType;
        while (currentBaseType != null)
        {
            yield return currentBaseType;
            currentBaseType= currentBaseType.BaseType;
        }
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
    
    public static bool IsNull<T, TU>(this KeyValuePair<T, TU> pair)
    {
        return pair.Equals(new KeyValuePair<T, TU>());
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
    
    public static float Angle(this float2 from, float2 to)
    {
        var num = sqrt(lengthsq(from) * lengthsq(to));
        return num < 1.00000000362749E-15 ? 0.0f : acos(clamp(dot(from, to) / num, -1f, 1f)) * 57.29578f;
    }
    
    // https://stackoverflow.com/a/3188835
    public static T MaxBy<T, U>(this IEnumerable<T> items, Func<T, U> selector)
    {
        if (!items.Any())
        {
            throw new InvalidOperationException("Empty input sequence");
        }

        var comparer = Comparer<U>.Default;
        T   maxItem  = items.First();
        U   maxValue = selector(maxItem);

        foreach (T item in items.Skip(1))
        {
            // Get the value of the item and compare it to the current max.
            U value = selector(item);
            if (comparer.Compare(value, maxValue) > 0)
            {
                maxValue = value;
                maxItem  = item;
            }
        }

        return maxItem;
    }
    
    public static T MinBy<T, U>(this IEnumerable<T> items, Func<T, U> selector)
    {
        if (!items.Any())
        {
            throw new InvalidOperationException("Empty input sequence");
        }

        var comparer = Comparer<U>.Default;
        T   maxItem  = items.First();
        U   maxValue = selector(maxItem);

        foreach (T item in items.Skip(1))
        {
            // Get the value of the item and compare it to the current max.
            U value = selector(item);
            if (comparer.Compare(value, maxValue) < 0)
            {
                maxValue = value;
                maxItem  = item;
            }
        }

        return maxItem;
    }
}

// https://github.com/Burtsev-Alexey/net-object-deep-copy
public static class ObjectExtensions
{
    private static readonly MethodInfo CloneMethod = typeof(Object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);

    public static bool IsPrimitive(this Type type)
    {
        if (type == typeof(String)) return true;
        return (type.IsValueType & type.IsPrimitive);
    }

    public static Object Copy(this Object originalObject)
    {
        return InternalCopy(originalObject, new Dictionary<Object, Object>(new ReferenceEqualityComparer()));
    }
    private static Object InternalCopy(Object originalObject, IDictionary<Object, Object> visited)
    {
        if (originalObject == null) return null;
        var typeToReflect = originalObject.GetType();
        if (IsPrimitive(typeToReflect)) return originalObject;
        if (visited.ContainsKey(originalObject)) return visited[originalObject];
        if (typeof(Delegate).IsAssignableFrom(typeToReflect)) return null;
        var cloneObject = CloneMethod.Invoke(originalObject, null);
        if (typeToReflect.IsArray)
        {
            var arrayType = typeToReflect.GetElementType();
            if (IsPrimitive(arrayType) == false)
            {
                Array clonedArray = (Array)cloneObject;
                clonedArray.ForEach((array, indices) => array.SetValue(InternalCopy(clonedArray.GetValue(indices), visited), indices));
            }

        }
        visited.Add(originalObject, cloneObject);
        CopyFields(originalObject, visited, cloneObject, typeToReflect);
        RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect);
        return cloneObject;
    }

    private static void RecursiveCopyBaseTypePrivateFields(object originalObject, IDictionary<object, object> visited, object cloneObject, Type typeToReflect)
    {
        if (typeToReflect.BaseType != null)
        {
            RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect.BaseType);
            CopyFields(originalObject, visited, cloneObject, typeToReflect.BaseType, BindingFlags.Instance | BindingFlags.NonPublic, info => info.IsPrivate);
        }
    }

    private static void CopyFields(object originalObject, IDictionary<object, object> visited, object cloneObject, Type typeToReflect, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy, Func<FieldInfo, bool> filter = null)
    {
        foreach (FieldInfo fieldInfo in typeToReflect.GetFields(bindingFlags))
        {
            if (filter != null && filter(fieldInfo) == false) continue;
            if (IsPrimitive(fieldInfo.FieldType)) continue;
            var originalFieldValue = fieldInfo.GetValue(originalObject);
            var clonedFieldValue = InternalCopy(originalFieldValue, visited);
            fieldInfo.SetValue(cloneObject, clonedFieldValue);
        }
    }
    public static T Copy<T>(this T original)
    {
        return (T)Copy((Object)original);
    }
}

public class ReferenceEqualityComparer : EqualityComparer<Object>
{
    public override bool Equals(object x, object y)
    {
        return ReferenceEquals(x, y);
    }
    public override int GetHashCode(object obj)
    {
        if (obj == null) return 0;
        return obj.GetHashCode();
    }
}
public static class ArrayExtensions
{
    public static void ForEach(this Array array, Action<Array, int[]> action)
    {
        if (array.LongLength == 0) return;
        ArrayTraverse walker = new ArrayTraverse(array);
        do action(array, walker.Position);
        while (walker.Step());
    }
}

internal class ArrayTraverse
{
    public int[] Position;
    private int[] maxLengths;

    public ArrayTraverse(Array array)
    {
        maxLengths = new int[array.Rank];
        for (int i = 0; i < array.Rank; ++i)
        {
            maxLengths[i] = array.GetLength(i) - 1;
        }
        Position = new int[array.Rank];
    }

    public bool Step()
    {
        for (int i = 0; i < Position.Length; ++i)
        {
            if (Position[i] < maxLengths[i])
            {
                Position[i]++;
                for (int j = 0; j < i; j++)
                {
                    Position[j] = 0;
                }
                return true;
            }
        }
        return false;
    }
}