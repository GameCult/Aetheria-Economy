using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MessagePack;
using Unity.Mathematics;
//using UnityEngine;
// TODO: USE THIS EVERYWHERE
using static Unity.Mathematics.math;
using Random = Unity.Mathematics.Random;

public class ZoneGenerator
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

	public static void GenerateZone(GlobalData global, ZoneData zone, GalaxyMapLayerData mass, GalaxyMapLayerData radius, out OrbitData[] orbitData, out PlanetData[] planetData)
	{
		var zoneRadius = lerp(global.MinimumZoneRadius, global.MaximumZoneRadius, radius.Evaluate(zone.Position, global));
		zone.Radius = zoneRadius;
		
		var zoneMass = lerp(global.MinimumZoneMass, global.MaximumZoneMass, mass.Evaluate(zone.Position, global));
		
		//Debug.Log($"Generating zone at position {zone.Position} with radius {zoneRadius} and mass {zoneMass}");
		
		var planets = GenerateEntities(global, zone, zoneMass, zoneRadius);
        
        // Create collections to map between zone generator output and database entries
        var orbitMap = new Dictionary<Planet, OrbitData>();
        var orbitInverseMap = new Dictionary<OrbitData, Planet>();
        
        // Create orbit database entries
        orbitData = planets.Select(planet =>
        {
            var data = new OrbitData()
            {
                ID = Guid.NewGuid(),
                Distance = planet.Distance,
                Period = planet.Period,
                Phase = planet.Phase,
                Zone = zone.ID
            };
            orbitMap[planet] = data;
            orbitInverseMap[data] = planet;
            return data;
        }).ToArray();

        // Link OrbitData parents to database GUIDs
        foreach (var data in orbitData)
            data.Parent = orbitInverseMap[data].Parent != null
                ? orbitMap[orbitInverseMap[data].Parent].ID
                : Guid.Empty;
        
        planetData = planets.Where(p=>!p.Empty).Select(planet =>
        {
            var planetDatum = new PlanetData
            {
                Mass = planet.Mass,
                ID = Guid.NewGuid(),
                Orbit = orbitMap[planet].ID,
                Zone = zone.ID,
                Belt = planet.Belt
            };
            planetDatum.Name = planetDatum.ID.ToString().Substring(0, 8);
            return planetDatum;
        }).ToArray();

        zone.Planets = planetData.Select(pd => pd.ID).ToList();
        zone.Orbits = orbitData.Select(od => od.ID).ToList();
	}

	public static Planet[] GenerateEntities(GlobalData global, ZoneData data, float mass, float radius)
	{
		var random = new Random();
		random.InitState(unchecked((uint)data.Name.GetHashCode()));
		
		var root = new Planet
		{
			Random = random,
			Global = global,
			Mass = mass, 
			ChildDistanceMaximum = radius * .75f,
			ChildDistanceMinimum = global.PlanetRadius(mass)
		};

		// There is some chance of generating a rosette or binary system
		// Probabilities which are fixed for the entire galaxy are in GlobalData, contained in the GameContext
		var rosette = random.NextFloat() < global.RosetteProbability;
		
		if (rosette)
		{
			// Create a rosette with a number of vertices between 2 and 9 inclusive
			root.ExpandRosette((int)(random.NextFloat(1, 3) * random.NextFloat(2, 3)));
		
			// Create a small number of less massive "captured" planets orbiting past the rosette
			root.ExpandSolar(
				count: random.NextInt(1,5), 
				massMulMin: .6f, 
				massMulMax: .8f, 
				distMulMin: 1.25f, 
				distMulMax: 1.75f, 
				jupiterJump: 1,
				massFraction: .1f);

			var averageChildMass = root.Children.Sum(p => p.Mass) / root.Children.Count;
			foreach(var p in root.Children.Where(c=>c.Mass>global.GasGiantMass))
			{
				var m = p.Mass / averageChildMass;
				// Give each child in the rosette its own mini solar system
				p.ExpandSolar(
					count: (int) (m * 5), 
					massMulMin: 0.75f, 
					massMulMax: 2.5f, 
					distMulMin: 1 + m * .25f,
					distMulMax: 1.05f + m * .5f,
					jupiterJump: random.NextFloat() * random.NextFloat() * 10 + 1,
					massFraction: .5f
				);
			}
		}
		else
		{
			// Create a regular old boring solar system
			root.ExpandSolar(
				count: random.NextInt(5, 15), 
				massMulMin: 0.5f, 
				massMulMax: 2.0f, 
				distMulMin: 1.1f,
				distMulMax: 1.25f,
				jupiterJump: random.NextFloat() * random.NextFloat() * 10 + 1,
				massFraction: .25f
			);
		}

		// Get all children that are above the satellite creation mass floor and not rosette members
		var satelliteCandidates = rosette
			? root.AllPlanets().Where(p => p != root && p.Parent != root && p.Mass > global.SatelliteCreationMassFloor)
			: root.AllPlanets().Where(p => p != root && p.Mass > global.SatelliteCreationMassFloor);

		var binaries = new List<Planet>();
		foreach (var planet in satelliteCandidates)
		{
			// There's a chance of generating satellites for each qualified planet
			if (random.NextFloat() < global.SatelliteCreationProbability)
			{
				// Sometimes the satellite is so massive that it forms a binary system (like Earth!)
				if (random.NextFloat() < global.BinaryCreationProbability)
				{
					planet.ExpandRosette(2);
					binaries.AddRange(planet.Children);
				}
				// Otherwise, terrestrial planets get a couple satellites while gas giants get many
				else planet.ExpandSolar(
					count: planet.Mass < global.GasGiantMass ? random.NextInt(1,3) : random.NextInt(4,10), 
					massMulMin: .75f, 
					massMulMax: 1.5f, 
					distMulMin: 1.05f, 
					distMulMax: 1.25f, 
					jupiterJump: 1,
					massFraction: .15f); // Planetary satellites are not nearly as massive as planets themselves
			}
		}

		// Get all children that are below the belt creation mass floor and not rosette members, also exclude binaries
		var beltCandidates = rosette
			? root.AllPlanets().Where(p => p != root && p.Parent != root && p.Mass < global.BeltMassCeiling && !binaries.Contains(p))
			: root.AllPlanets().Where(p => p != root && p.Mass < global.BeltMassCeiling && !binaries.Contains(p));
		foreach(var planet in beltCandidates.Reverse())
			if (random.NextFloat() < global.BeltProbability && !planet.Parent.Children.Any(p=>p.Belt))
				planet.Belt = true;

		return root.AllPlanets().ToArray();
	}
}

// All orbiting bodies are referred to here as "Planets"
// This goes back to the original etymology for the word planet, which is traveler,
// Because they were objects we observed to move in the sky instead of remaining fixed like the stars
public class Planet
{
	public Random Random;
	public GlobalData Global;
	public float Distance;
	public float Phase;
	public float Mass;
	public float Period;
	public float ChildDistanceMinimum;
	public float ChildDistanceMaximum;
	public bool Empty = false;
	public bool Belt = false;
	public List<Planet> Children = new List<Planet>(); // Planets orbiting this one are referred to as children
	public Planet Parent;

	// Recursively gather all planets in the hierarchy
	public IEnumerable<Planet> AllPlanets()
	{
		return new[]{this}.Concat(Children.SelectMany(c=>c.AllPlanets()));
	}

	// Create children that fill a single orbit, equally spaced
	// https://en.wikipedia.org/wiki/Klemperer_rosette
	public void ExpandRosette(int vertices)
	{
		//Debug.Log("Expanding Rosette");
		
		// Rosette children replace the parent, parent orbital node is left empty
		Empty = true;
		
		// Masses in a rosette alternate, so every sequential pair has the same shared mass
		var sharedMass = Mass / vertices * 2;
		
		// Proportion of the masses of sequential pairs is random, but only for even vertex counts
		var proportion = vertices % 2 == 0 ? Random.NextFloat(.5f,.95f) : .5f;
		
		// Place children at a fixed distance in the center of the range
		var dist = (ChildDistanceMinimum + ChildDistanceMaximum) / 2;
		
		// Position of first child
		var p0 = new float2(0, dist);
		
		// Position of second child
		var p1 = OrbitData.Evaluate(1.0f / vertices) * dist;
		
		// Maximum child distance is half the distance to the neighbor minus the neighbor's radius
		var p0ChildDist = (distance(p0, p1) * proportion - Global.PlanetRadius(sharedMass * (1 - proportion))) * .75f;
		var p1ChildDist = (distance(p0, p1) * (1 - proportion) - Global.PlanetRadius(sharedMass * proportion)) * .75f;
		
		for (int i = 0; i < vertices; i++)
		{
			var child = new Planet
			{
				Random = Random,
				Global = Global,
				Parent = this,
				Mass = sharedMass * (i % 2 == 0 ? proportion : 1 - proportion), // Masses alternate
				Distance = dist,
				Phase = (float) i / vertices,
				ChildDistanceMaximum = (i % 2 == 0 ? p0ChildDist : p1ChildDist)
			};
			child.ChildDistanceMinimum = Global.PlanetRadius(child.Mass) * 2;
			child.Period = pow(child.Distance, Global.OrbitPeriodExponent) * Global.OrbitPeriodMultiplier;
			Children.Add(child);
		}

		ChildDistanceMinimum = dist + p0ChildDist;
	}

	// Create children that mimic the distribution of planetary masses in the solar system
	public void ExpandSolar(int count, float massMulMin, float massMulMax, float distMulMin, float distMulMax, float jupiterJump, float massFraction)
	{
		//Debug.Log("Expanding Solar");
		// Expansion is impossible when child space is full
		if (count == 0 || ChildDistanceMaximum < ChildDistanceMinimum)
			return;
		
		var masses = new float[count];
		var distances = new float[count];
		
		// Accumulate total mass 
		// Initialize first mass and distance, actual number doesn't matter since total mass will be divided proportionally
		float massTotal = distances[0] = masses[0] = 1;
		
		// Masses and distances multiply from one planet to the next
		// Mass typically increases exponentially as you go further out
		// jupiterJump is an additional mass multiplier applied after half of the planets
		for (var i = 1; i < count; i++)
		{
			massTotal += masses[i] = masses[i - 1] * Random.NextFloat(massMulMin, massMulMax) * (count/2==i ? jupiterJump : 1);
			distances[i] = distances[i - 1] * Random.NextFloat(distMulMin, distMulMax);
		}

		// Add some randomness to child masses
		for (var i = 0; i < masses.Length; i++)
			masses[i] *= Random.NextFloat(.1f, 1f);

		// Normalize the masses of the children
		for (var i = 0; i < count; i++)
			masses[i] = masses[i] / massTotal * Mass * massFraction;

		// Map child distances to range between minimum and maximum
		if (count > 1)
		{
			var oldDistances = (float[]) distances.Clone();
			for (var i = 0; i < count; i++)
				distances[i] = lerp(ChildDistanceMinimum, ChildDistanceMaximum,
					(oldDistances[i] - oldDistances[0]) / (oldDistances[count - 1] - oldDistances[0])) + Global.PlanetRadius(masses[i]);
		}
		
		for (var i = 0; i < count; i++)
		{
			// Only instantiate children above the mass floor
			if (masses[i] > Global.MassFloor)
			{
				var child = new Planet
				{
					Random = Random,
					Global = Global,
					Parent = this,
					Mass = masses[i],
					Distance = distances[i],
					Phase = Random.NextFloat()
				};
				child.Period = pow(child.Distance, Global.OrbitPeriodExponent) * Global.OrbitPeriodMultiplier;
				child.ChildDistanceMinimum = Global.PlanetRadius(child.Mass) * 2;
				// Maximum child distance of child is the smallest distance to either of its neighbors
				child.ChildDistanceMaximum = min(i == 0 ? child.Distance - ChildDistanceMinimum : child.Distance - distances[i - 1],
										 i < count - 1 ? distances[i + 1] - child.Distance : float.PositiveInfinity);
				if (float.IsNaN(child.Distance))
					throw new NotFiniteNumberException($"Planet created with NaN distance, something went very wrong!");
				if (child.ChildDistanceMaximum > child.ChildDistanceMinimum)
					Children.Add(child);
			}
		}
	}
}