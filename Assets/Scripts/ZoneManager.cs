using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MessagePack;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;
// TODO: USE THIS EVERYWHERE
using static Unity.Mathematics.math;

public class ZoneManager
{
//	public float BinaryFloor = 100;
//	public float BinaryProbability = .1f;
//	public float MassPowerMinimum = 1.35f;
//	public float MassPowerMaximum = 1.75f;
//	public float DistancePowerMinimum = 1.35f;
//	public float DistancePowerMaximum = 1.75f;
//	public float Radius = 500;
//	public float Mass = 100000;
//	public float MassFloor = 1;
//	public float SunMass = 10000;
//	public float GasGiantMass = 2000;
//	public float RadiusPower = 1.75f;

	public static Planet[] GenerateEntities(ZoneData data)
	{
		Random.InitState(data.Entry.Name.GetHashCode());
		

		Planet.MassFloor = data.MassFloor;
		Planet.RadiusPower = data.RadiusPower;
		
		var root = new Planet
		{
			Mass = data.Mass, 
			ChildDistanceMaximum = data.Radius,
			ChildDistanceMinimum = Planet.Radius(data.Mass)
		};
		
		root.ExpandRosette(6);
		
		root.ExpandSolar(4, .6f, .8f, 1.25f, 1.75f);

		var averageChildMass = root.Children.Sum(p => p.Mass) / root.Children.Count;
		foreach(var p in root.Children.Where(c=>c.Mass>data.GasGiantMass))
		{
			var m = p.Mass / averageChildMass;
			p.ExpandSolar((int) (m * 5), 1.5f, 2.5f, 1 + m * .05f, 1.05f + m * .15f);
		}

//		foreach (var p in root.Children.Concat(root.Children.SelectMany(c => c.Children))
//			.Where(p => p.Mass > BinaryFloor && Random.value < BinaryProbability))
//			p.ExpandBinary();


		return root.AllPlanets().ToArray();
	}
}

public class Planet
{
	public static float MassFloor;
	public static float RadiusPower;
	
	public float Distance;
	public float Phase;
	public float Mass;
	public float Period;
	public float ChildDistanceMinimum;
	public float ChildDistanceMaximum;
	public bool Empty = false;
	public List<Planet> Children = new List<Planet>();
	public Planet Parent;
	public Orbit Instance;
	
	public static float Radius(float mass)
	{
		return pow(mass, 1 / RadiusPower);
	}

	public IEnumerable<Planet> AllPlanets()
	{
		return new[]{this}.Concat(Children.SelectMany(c=>c.AllPlanets()));
	}

	public void ExpandRosette(int vertices)
	{
		//Empty = true;
		vertices = vertices / 2 * 2;
		// Masses in a rosette alternate, so every sequential pair has the same shared mass
		var sharedMass = Mass / (vertices / 2);
		var proportion = Random.Range(.5f,.9f);
		var dist = (ChildDistanceMinimum + ChildDistanceMaximum) / 2;
		var p0 = new float2(0, dist);
		var p1 = Orbit.Evaluate(1.0f / vertices) * dist;
		var totalChildDist = distance(p0, p1) - Radius(sharedMass * proportion) - Radius(sharedMass * (1 - proportion));
		var p0ChildDist = totalChildDist * proportion;
		var p1ChildDist = totalChildDist * (1 - proportion);
		for (int i = 0; i < vertices; i++)
		{
			var child = new Planet
			{
				Parent = this,
				Mass = sharedMass * (i % 2 == 0 ? proportion : 1 - proportion), // Masses alternate
				Distance = dist,
				Phase = (float) i / vertices,
				ChildDistanceMaximum = (i % 2 == 0 ? p0ChildDist : p1ChildDist)
			};
			child.ChildDistanceMinimum = Radius(child.Mass) * 2;
			child.Period = child.Distance * child.Distance / 100;
			Children.Add(child);
		}

		ChildDistanceMinimum = dist + p0ChildDist;
	}

	public void ExpandSolar(int count, float massPowMin, float massPowMax, float distPowMin, float distPowMax)
	{
		if (count == 0 || ChildDistanceMaximum < ChildDistanceMinimum)
			return;
		var masses = new float[count];
		var distances = new float[count];
		float massTotal = distances[0] = masses[0] = 1;
		for (var i = 1; i < count; i++)
		{
			massTotal += masses[i] = masses[i - 1] * Random.Range(massPowMin, massPowMax);
			distances[i] = distances[i - 1] * Random.Range(distPowMin, distPowMax);
		}

		for (var i = 0; i < masses.Length; i++)
			masses[i] *= Random.Range(.1f, 1f);

		for (var i = 0; i < count; i++)
		{
			Func<int,float> mass = d => masses[d] / massTotal * Mass;
			Func<int,float> realDist = d => lerp(ChildDistanceMinimum, ChildDistanceMaximum, 
				(distances[d] - distances[0]) / (distances[count - 1] - distances[0]) * .9f) + Radius(mass(d));
			if (mass(i) > MassFloor)
			{
				var child = new Planet
				{
					Parent = this,
					Mass = mass(i),
					Distance = realDist(i),
					Phase = Random.value
				};
				child.Period = child.Distance * child.Distance / 100;
				child.ChildDistanceMinimum = Radius(child.Mass) * 2;
				child.ChildDistanceMaximum = min(i > 0 ? child.Distance - realDist(i - 1) : child.Distance - ChildDistanceMinimum,
										 i < count - 1 ? realDist(i + 1) - child.Distance : float.PositiveInfinity);
				if (float.IsNaN(child.Distance)) {
					Debug.Log($"Planet created with NaN distance, something went very wrong!");
					return;
				}
				if (child.ChildDistanceMaximum > child.ChildDistanceMinimum)
					Children.Add(child);
			}
		}
	}

	public void ExpandBinary()
	{
		Empty = true;
		var proportion = Random.Range(.5f,.99f);
		var dist = Random.Range(.1f * ChildDistanceMaximum, .5f * ChildDistanceMaximum);
		var big = new Planet
		{
			Mass = Mass * proportion,
			Distance = dist * (1 - proportion)
		};
		big.ChildDistanceMinimum = Radius(big.Mass);
		big.Period = dist * dist / 100;
		big.ChildDistanceMaximum = dist * proportion;
		Children.Add(big);
		
		var little = new Planet
		{
			Parent = this,
			Mass = Mass * (1 - proportion),
			Distance = dist * proportion
		};
		little.ChildDistanceMinimum = Radius(little.Mass);
		little.Period = dist * dist / 100;
		little.ChildDistanceMaximum = dist * (1 - proportion);
		little.Phase = .5f;
		Children.Add(little);

		ChildDistanceMaximum = little.Distance + little.ChildDistanceMaximum;
	}

	public void Expand(ExpansionType type)
	{
		switch (type)
		{
			case ExpansionType.Rosette:
				// Choose an even number of vertices between 4 and 12 inclusive
				ExpandRosette((Random.Range(2, 7) * Random.Range(2, 7)) / 2 * 2);
				break;
			case ExpansionType.Solar:
				ExpandSolar(Random.Range(1, 6) + Random.Range(1, 6), 1.2f, 2f, 1.2f, 2f);
				break;
			case ExpansionType.Binary:
				ExpandBinary();
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(type), type, null);
		}
	}
}

public enum ExpansionType
{
	Rosette,
	Solar,
	Binary
}
