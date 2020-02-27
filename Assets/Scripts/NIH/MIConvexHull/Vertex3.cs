using UnityEngine;
using System.Collections;
using MIConvexHull;

public class Vertex3 : IVertex
{

	public double[] Position { get; set; }
	
	public double x { get { return Position[0]; } }
	public double y { get { return Position[1]; } }
	public double z { get { return Position[2]; } }
	
	public Vertex3(double x, double y, double z)
	{
		Position = new double[] { x, y, z };
	}
	
	public Vector3 ToVector3() {
		return new Vector3((float) Position[0], (float) Position[1], (float) Position[2]);
	}
}
