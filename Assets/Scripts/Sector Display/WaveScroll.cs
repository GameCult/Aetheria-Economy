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
        _material.SetFloat("_Phase", _phase+=Time.deltaTime*Speed);
    }
}
