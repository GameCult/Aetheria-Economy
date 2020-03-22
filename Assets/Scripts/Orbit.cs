using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
// TODO: USE THIS EVERYWHERE
using static Unity.Mathematics.math;

public class Orbit : MonoBehaviour
{
    public Transform Target;
    public float Period;
    public bool ApplyOrbitRotation = false;
	public float Phase;
	public float Distance;
	public bool CalculatePosition;

    //private float _period;

	// Use this for initialization
	void Start ()
	{
	    if (Target == null)
	    {
	        enabled = false;
	        return;
	    }

	    var diff = Target.position - transform.position;

	    Distance = !CalculatePosition ? Distance : diff.magnitude;

	    Phase = !CalculatePosition ? Phase : Mathf.Atan2(-diff.x, -diff.z);

	    if (Mathf.Approximately(Period,0))
        {
            Period = diff.sqrMagnitude / 100;
        }
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (Target == null)
		{
			enabled = false;
			Debug.Log("Orbit script updating without target. HOW DOES THIS HAPPEN?!");
			return;
		}
		
		var time = Time.time;//(GameManager.Instance!=null ? GameManager.Instance.ServerTime : Time.time);

//		var p = _phase + time / (Period / (Mathf.PI * 2));
		
		var pos = OrbitInstance.Evaluate(Phase + time / Period);

        transform.position = new Vector3(Target.position.x + Distance * pos.x,
											transform.position.y,
	                                     Target.position.z + Distance * pos.y);
        var rot = transform.localRotation.eulerAngles;

        if(ApplyOrbitRotation)
            transform.rotation = Quaternion.Euler(rot.x,(rot.y*Mathf.Deg2Rad-Time.fixedDeltaTime * Period) * Mathf.Rad2Deg,rot.z);
	}

    void Parent(Transform parent)
    {
        Target = parent;
    }
}
