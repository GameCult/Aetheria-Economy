/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveScroll : MonoBehaviour
{
    public float Speed = 1;

    private Material _material;
    private float _phase;

    void Start ()
	{
	    _material = GetComponent<MeshRenderer>().material;
	}
	
	void Update () {
        _material.SetFloat("_Phase", _phase=Time.time*Speed);
    }
}
