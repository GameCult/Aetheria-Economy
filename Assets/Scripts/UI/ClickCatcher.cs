using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ClickCatcher : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
	//public bool EnableMouse => PointerIsInside && !_catching;
	public bool PointerIsInside { get; private set; }
	
	public event Action<PointerEventData> OnEnter;
	public event Action<PointerEventData> OnExit;
	public event Action<PointerEventData> OnClick;

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
		OnEnter?.Invoke(eventData);
		PointerIsInside = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		//Debug.Log("Pointer Exited");
		OnExit?.Invoke(eventData);
		PointerIsInside = false;
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		OnClick?.Invoke(eventData);
	}
}
