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

using static Unity.Mathematics.math;

public static class UnityExtensions
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

				Debug.LogError(e.Message);
			}
		}
	}

	public static Texture2D ToTexture(this Color c)
	{
		Texture2D result = new Texture2D(1, 1);
		result.SetPixels(new[]{c});
		result.Apply();

		return result;
	}
}
