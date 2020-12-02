using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinimapCameraTrack : MonoBehaviour
{
	public Transform Camera;
	public bool Modulo;
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
		if (Modulo)
			deg = Mathf.Repeat(deg + 15, 30) - 15;
		_rect.rotation = Quaternion.Euler(0,0,deg+Offset);
	}
}
