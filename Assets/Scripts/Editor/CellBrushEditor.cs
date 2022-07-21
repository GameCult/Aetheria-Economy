using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
 
public class CellBrushEditor : MaterialEditor
{
	private static readonly string[] Keywords = new[] {"A", "B", "C", "D", "E", "F", "G", "H"};

	public override void OnInspectorGUI ()
	{
		// Draw the default inspector.
		base.OnInspectorGUI ();
 
		// If we are not visible, return.
		if (!isVisible)
			return;
 
		// Get the current keywords from the material
		Material targetMat = target as Material;
		string[] keyWords = targetMat.shaderKeywords;
 
		int selected = keyWords.Any()?Array.IndexOf(Keywords,keyWords.First()):0;
		EditorGUI.BeginChangeCheck();
		selected = EditorGUILayout.Popup("Variant", selected, Keywords);
		
		// If something has changed, update the material.
		if (EditorGUI.EndChangeCheck())
		{
			targetMat.shaderKeywords = new []{Keywords[selected]};
			EditorUtility.SetDirty (targetMat);
		}
	}
}