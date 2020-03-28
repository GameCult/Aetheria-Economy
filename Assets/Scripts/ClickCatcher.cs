using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ClickCatcher : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
	public bool EnableMouse => PointerIsInside && !_catching;
	public bool PointerIsInside { get; private set; }
	
	public PointerEnterEvent OnEnter = new PointerEnterEvent();
	public PointerExitEvent OnExit = new PointerExitEvent();

	private bool _catching;
	private UnityEvent _catchClick;

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

	public UnityEvent Catch()
	{
		_catching = true;
		_catchClick = new UnityEvent();
		return _catchClick;
	}

	public void Release()
	{
		_catching = false;
	}
	
	public void OnPointerEnter(PointerEventData eventData)
	{
		//Debug.Log("Pointer Entered");
		OnEnter.Invoke();
		PointerIsInside = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		//Debug.Log("Pointer Exited");
		OnExit.Invoke();
		PointerIsInside = false;
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (!_catching) return;
		
		_catchClick.Invoke();
		_catching = false;
	}
	
	[Serializable]
	public class PointerEnterEvent : UnityEvent { }
	[Serializable]
	public class PointerExitEvent : UnityEvent { }
}
