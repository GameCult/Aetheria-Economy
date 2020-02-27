using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using MessagePack;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public static class Extensions
{
	public static void BroadcastMessageExt<T>(this GameObject go, string methodName, object value = null, SendMessageOptions options = SendMessageOptions.RequireReceiver)
	{
		var monoList = new List<T>();
		go.GetComponentsInChildren(true, monoList);
		foreach (var component in monoList)
		{
			try
			{
				Type type = component.GetType();

				MethodInfo method = type.GetMethod(methodName, BindingFlags.Instance |
				                                               BindingFlags.NonPublic |
				                                               BindingFlags.Public |
				                                               BindingFlags.Static);

				method.Invoke(component, new[] { value });
			}
			catch (Exception e)
			{
				//Re-create the Error thrown by the original SendMessage function
				if (options == SendMessageOptions.RequireReceiver)
					Debug.LogError("SendMessage " + methodName + " has no receiver!");

				//Debug.LogError(e.Message);
			}
		}
	}
	
	public static bool IsDefault<T>(this T value) where T : struct
	{
		bool isDefault = value.Equals(default(T));

		return isDefault;
	}

	public static Texture2D ToTexture(this Color c)
	{
		Texture2D result = new Texture2D(1, 1);
		result.SetPixels(new[]{c});
		result.Apply();

		return result;
	}

	public static Vector2 Flatland(this Vector3 v)
	{
		return new Vector2(v.x, v.z);
	}

	public static Vector3 Flatland(this Vector2 v)
	{
		return new Vector3(v.x, 0, v.y);
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
	
	public static T RandomElement<T>(this IEnumerable<T> enumerable) => enumerable.ElementAt(Random.Range(0, enumerable.Count()));

	public static float2 ToFloat2(this Vector2 v) => new float2(v.x, v.y);
	public static float3 ToFloat3(this Vector3 v) => new float3(v.x, v.y, v.z);
	public static Vector2 ToVector2(this float2 v) => new Vector2(v.x, v.y);
	public static Vector3 ToVector3(this float3 v) => new Vector3(v.x, v.y, v.z);

	public static IDatabaseEntry Get(this Guid entry) => Database.Get(entry);
	public static T Get<T>(this Guid entry) where T : class, IDatabaseEntry => Database.Get(entry) as T;
	public static Guid GetId(this IDatabaseEntry entry) => entry.Entry.ID;
	public static string ToJson(this IDatabaseEntry entry) => MessagePackSerializer.SerializeToJson(entry);
	public static byte[] Serialize(this IDatabaseEntry entry) => MessagePackSerializer.Serialize(entry);

	public static bool IsImplementationOf(this Type baseType, Type interfaceType)
	{
		return baseType.GetInterfaces().Any(interfaceType.Equals);
	}
	
}
