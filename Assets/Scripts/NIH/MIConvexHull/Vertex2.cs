using System;
using UnityEngine;
using System.Collections;
using MIConvexHull;

public class Vertex2 : IVertex
{

	public double[] Position => new[] {(double) StoredPosition.x, StoredPosition.y};

	public Vector2 StoredPosition;
	
	public float x { get { return StoredPosition.x; } }
	public float y { get { return StoredPosition.y; } }
	
	public Vertex2(float x, float y)
	{
		StoredPosition = new Vector2(x, y);
	}

	public Vertex2(Vector2 pos)
	{
		StoredPosition = pos;
	}

	public Vector2 ToVector2() {
		return StoredPosition;
	}
	
	public Vector3 ToVector3() {
		return StoredPosition;
	}

	public double Distance(Vertex2 v)
	{
		return Math.Sqrt((v.x - x) * (v.x - x) + (v.y - y) * (v.y - y));
	}
}
