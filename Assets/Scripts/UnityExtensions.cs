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
	public static T FindInParents<T>(this GameObject go) where T : Component
	{
		if (go == null) return null;
		var comp = go.GetComponent<T>();

		if (comp != null)
			return comp;

		Transform t = go.transform.parent;
		while (t != null && comp == null)
		{
			comp = t.gameObject.GetComponent<T>();
			t = t.parent;
		}
		return comp;
	}

	public static bool ContainsWorldPoint(this RectTransform rectTransform, Vector3 point)
	{
		Vector2 localMousePosition = rectTransform.InverseTransformPoint(point);
		return rectTransform.rect.Contains(localMousePosition);
	}
	
	public static void BroadcastAll(string fun, System.Object msg) {
		GameObject[] gos = (GameObject[])GameObject.FindObjectsOfType(typeof(GameObject));
		foreach (GameObject go in gos) {
			if (go && go.transform.parent == null) {
				go.gameObject.BroadcastMessageExt<MonoBehaviour>(fun, msg, SendMessageOptions.DontRequireReceiver);
			}
		}
	}
	
	public static void BroadcastMessageExt<T>(this GameObject go, string methodName, object value = null, SendMessageOptions options = SendMessageOptions.RequireReceiver)
	{
		var monoList = new List<T>();
		go.GetComponentsInChildren(true, monoList);
		//monoList.Add();
		foreach (var component in monoList)
		{
			// try
			// {
				Type type = component.GetType();

				MethodInfo method = type.GetMethod(methodName, BindingFlags.Instance |
				                                               BindingFlags.NonPublic |
				                                               BindingFlags.Public |
				                                               BindingFlags.Static);

				if(method!=null)
					method.Invoke(component, new[] { value });
			// }
			// catch (Exception e)
			// {
			// 	//Re-create the Error thrown by the original SendMessage function
			// 	if (options == SendMessageOptions.RequireReceiver)
			// 		Debug.LogError("SendMessage " + methodName + " has no receiver!");
			//
			// 	Debug.LogError(e.Message);
			// }
		}
	}

	public static Texture2D ToTexture(this Color c)
	{
		Texture2D result = new Texture2D(1, 1);
		result.SetPixels(new[]{c});
		result.Apply();

		return result;
	}

	private const int GradSteps = 32;
	public static Texture2D ToTexture(this Gradient g)
	{
		var tex = new Texture2D(GradSteps, 1) {wrapMode = TextureWrapMode.Clamp};
		for (int x = 0; x < GradSteps; x++)
		{
			tex.SetPixel(x, 0, g.Evaluate( (float)x / GradSteps));
		}
		tex.Apply();
		return tex;
	}

	public static Gradient ToGradient(this float4[] keys, bool sharp = false)
	{
		var grad = new Gradient();
		grad.mode = sharp ? GradientMode.Fixed : GradientMode.Blend;
		grad.alphaKeys = new[] {new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1)};
		grad.colorKeys = keys.Select(k => new GradientColorKey(k.xyz, k.w)).ToArray();
		return grad;
	}
	
	public static Vector3 Flatland(this Vector2 v) => new Vector3(v.x,0,v.y);
	public static Vector2 Flatland(this Vector3 v) => new Vector2(v.x,v.z);
}
