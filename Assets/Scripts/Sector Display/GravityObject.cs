/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

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
