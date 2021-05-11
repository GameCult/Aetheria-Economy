/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ClickCatcher : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, /*IPointerUpHandler, IPointerDownHandler,*/ IPointerClickHandler
{
	//public bool EnableMouse => PointerIsInside && !_catching;
	public bool PointerIsInside { get; private set; }
	public float DragDistance = 25f;
	
	public Subject<PointerEventData> OnEnter = new Subject<PointerEventData>();
	public Subject<PointerEventData> OnExit = new Subject<PointerEventData>();
	public Subject<PointerEventData> OnClick = new Subject<PointerEventData>();

	// private float downTime;

	public static ClickCatcher Background
	{
		get
		{
			if (_background == null)
				_background = GameObject.FindGameObjectWithTag("UIBackground").GetComponent<ClickCatcher>();
			return _background;
		}
	}

	private static ClickCatcher _background;
	
	public void OnPointerEnter(PointerEventData eventData)
	{
		//Debug.Log("Pointer Entered");
		OnEnter.OnNext(eventData);
		PointerIsInside = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		//Debug.Log("Pointer Exited");
		OnExit.OnNext(eventData);
		PointerIsInside = false;
	}

	// public void OnPointerUp(PointerEventData eventData)
	// {
	// 	if(Time.time - downTime < ClickTime)
	// 		OnClick?.Invoke(eventData);
	// }
	//
	// public void OnPointerDown(PointerEventData eventData)
	// {
	// 	downTime = Time.time;
	// }

	public void OnPointerClick(PointerEventData eventData)
	{
		if((eventData.pressPosition - eventData.position).sqrMagnitude < DragDistance)
			OnClick.OnNext(eventData);
	}
}
