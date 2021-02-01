/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinimapCameraTrack : MonoBehaviour
{
	public Transform Camera;
	public bool Modulo;
	public bool Flip;
	public float Offset;
	private RectTransform _rect;

	void Start ()
	{
		_rect = GetComponent<RectTransform>();
	}
	
	void Update ()
	{
		var dir = Camera.forward.Flatland().normalized;
		var deg = -Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
		if (Flip)
			deg *= -1;
		if (Modulo)
			deg = Mathf.Repeat(deg + 15, 30) - 15;
		_rect.rotation = Quaternion.Euler(0,0,deg+Offset);
	}
}
