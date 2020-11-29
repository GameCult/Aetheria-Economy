using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityObject : MonoBehaviour
{
	[HideInInspector]
	public Material GravityMaterial;

	[HideInInspector]
	public string Shader;

	void Start () {
		Gravity.Instance.GravityObjects.Add(this);
		GravityMaterial = GetComponent<MeshRenderer>().material;
		Shader = GravityMaterial.shader.name;
	}
}
