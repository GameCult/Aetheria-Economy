using System;
using System.Collections.Generic;
using System.Linq;
using DataStructures.ViliWonka.Heap;
using MIConvexHull;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Random = Unity.Mathematics.Random;

public static class SectorGenerator
{
    public static Sector GenerateSector(SectorGenerationSettings settings, MegaCorporation[] megas, ref Random random, float minLineSeparation = .01f)
    {
        var outputSamples = WeightedSampleElimination.GeneratePoints(settings.ZoneCount,
            settings.CloudDensity,
            v => (.2f - lengthsq(v - float2(.5f))) * 4);
        var sector = new Sector();
        foreach (var v in outputSamples) sector.Zones.Add(new SectorZone {Position = v});

        #region Link Placement

        // Create a delaunay triangulation to connect adjacent sectors
        var triangulation = DelaunayTriangulation<Vertex2<SectorZone>, Cell2<SectorZone>>
            .Create(sector.Zones.Select(z => new Vertex2<SectorZone>(z.Position, z)).ToList(), 1e-5f);
        var links = new HashSet<(SectorZone, SectorZone)>();
        foreach(var cell in triangulation.Cells)
        {
            if(!links.Contains((cell.Vertices[0].StoredObject, cell.Vertices[1].StoredObject)) &&
               !links.Contains((cell.Vertices[1].StoredObject, cell.Vertices[0].StoredObject)))
                links.Add((cell.Vertices[0].StoredObject, cell.Vertices[1].StoredObject));
            if(!links.Contains((cell.Vertices[1].StoredObject, cell.Vertices[2].StoredObject)) &&
               !links.Contains((cell.Vertices[2].StoredObject, cell.Vertices[1].StoredObject)))
                links.Add((cell.Vertices[1].StoredObject, cell.Vertices[2].StoredObject));
            if(!links.Contains((cell.Vertices[0].StoredObject, cell.Vertices[2].StoredObject)) &&
               !links.Contains((cell.Vertices[2].StoredObject, cell.Vertices[0].StoredObject)))
                links.Add((cell.Vertices[0].StoredObject, cell.Vertices[2].StoredObject));
        }

        foreach (var link in links.ToArray())
        {
            foreach (var zone in sector.Zones)
            {
                if(zone != link.Item1 && zone != link.Item2)
                    if (AetheriaMath.FindDistanceToSegment(zone.Position, link.Item1.Position, link.Item2.Position, out _) < minLineSeparation)
                        links.Remove(link);
            }
        }

        foreach (var link in links)
        {
            link.Item1.AdjacentZones.Add(link.Item2);
            link.Item2.AdjacentZones.Add(link.Item1);
        }
        
        float LinkWeight((SectorZone, SectorZone) link)
        {
            return 1 / saturate(settings.CloudDensity((link.Item1.Position + link.Item2.Position) / 2)) *
                   lengthsq(link.Item1.Position - link.Item2.Position) *
                   (link.Item1.AdjacentZones.Count - 1) * (link.Item2.AdjacentZones.Count - 1);
        }
        
        var heap = new MaxHeap<(SectorZone, SectorZone)>(links.Count);
        foreach(var link in links) heap.PushObj(link, LinkWeight(link));
        while (heap.Count > settings.LinkDensity * links.Count)
        {
            var link = heap.PopObj();
            if(sector.ConnectedRegion(link.Item1, link.Item1, link.Item2).Contains(link.Item2))
            {
                link.Item1.AdjacentZones.Remove(link.Item2);
                link.Item2.AdjacentZones.Remove(link.Item1);
                foreach (var secondary in link.Item1.AdjacentZones)
                {
                    heap.SetValue((link.Item1, secondary), LinkWeight((link.Item1, secondary)));
                    heap.SetValue((secondary, link.Item1), LinkWeight((secondary, link.Item1)));
                }

                foreach (var secondary in link.Item2.AdjacentZones)
                {
                    heap.SetValue((link.Item2, secondary), LinkWeight((link.Item2, secondary)));
                    heap.SetValue((secondary, link.Item2), LinkWeight((secondary, link.Item2)));
                }
            }
        }

        #endregion

        // Cache distance set and calculate isolation for every zone (used extensively for placing stuff)
        foreach (var zone in sector.Zones)
        {
            zone.Distance = sector.ConnectedRegionDistance(zone);
            zone.Isolation = zone.Distance.Sum(x => x.Value);
        }

        // Exit is the most isolated zone (highest total distance to all other zones
        sector.Exit = sector.Zones.MaxBy(z => z.Isolation);
        
        // Entrance is the zone furthest from the exit
        sector.Entrance = sector.Zones.MaxBy(z => sector.Exit.Distance[z]);

        // Store exit path in hash set for querying
        var exitPathSet = new HashSet<SectorZone>(sector.ExitPath);
        
        // Find all zones on the exit path where removing that zone would disconnect the entrance from the exit
        // Disregard "corridor" zones with only two adjacent zones
        var chokePoints = sector.ExitPath
            .Where(z => z.AdjacentZones.Count > 2 && !sector.ConnectedRegion(sector.Entrance, z).Contains(sector.Exit));

        // Choose some megas to have bosses placed based on whether a boss hull is assigned
        var bossMegas = megas
            .Where(m => m.BossHull != Guid.Empty)
            .Take(settings.BossCount)
            .ToArray();

        // Place boss zones along the critical path as far apart from each other as possible
        foreach (var mega in bossMegas)
        {
            sector.BossZones[mega] = chokePoints.MaxBy(z =>
                sector.Exit.Distance[z] * sector.Entrance.Distance[z] * 
                sector.BossZones.Values.Aggregate(1, (i, os) => i * os.Distance[z]));
        }

        // Place boss mega headquarters such that their sphere of influence encompasses their boss zone
        // While occupying as much territory as possible
        foreach (var mega in bossMegas)
        {
            sector.HomeZones[mega] = sector
                .ConnectedRegion(sector.BossZones[mega], mega.InfluenceDistance)
                .MaxBy(z =>
                    sector.ConnectedRegion(z, mega.InfluenceDistance).Count *
                    sector.HomeZones.Values.Aggregate(1f, (i, os) => i * sqrt(os.Distance[z])));
        }
        
        // Place remaining headquarters away from existing megas while also maximizing territory
        foreach (var mega in megas.Where(m=>!bossMegas.Contains(m)))
        {
            sector.HomeZones[mega] = sector.Zones.MaxBy(z =>
                pow(sector.ConnectedRegion(z, mega.InfluenceDistance).Count, sector.HomeZones.Count) *
                sector.Exit.Distance[z] * sector.Entrance.Distance[z] * 
                sector.HomeZones.Values.Aggregate(1f, (i, os) => i * sqrt(os.Distance[z])) * 
                sector.BossZones.Values.Aggregate(1f, (i, os) => i * sqrt(os.Distance[z])));
        }

        // Assign faction presence
        foreach (var zone in sector.Zones)
        {
            // All megas for are present in zones within their sphere of influence
            zone.Megas = megas
                .Where(m => zone.Distance[sector.HomeZones[m]] <= m.InfluenceDistance)
                .ToArray();
            
            // Owner of a zone is the one with the nearest headquarters
            var nearestMega = megas.MinBy(m => zone.Distance[sector.HomeZones[m]]);
            if (zone.Distance[sector.HomeZones[nearestMega]] <= nearestMega.InfluenceDistance)
                zone.Owner = nearestMega;
        }

        // Generate zone name using the owner's name generator, otherwise assign catalogue ID
        foreach (var zone in sector.Zones) 
            zone.Name = zone.Owner?.NameGenerator.NextName ?? $"EAC-{random.NextInt(99999).ToString()}";

        return sector;
    }
}