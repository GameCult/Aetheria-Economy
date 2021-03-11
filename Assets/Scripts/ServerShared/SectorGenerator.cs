using System;
using System.Collections.Generic;
using System.Linq;
using DataStructures.ViliWonka.Heap;
using MIConvexHull;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public static class SectorGenerator
{
    public static Sector GenerateSector(SectorGenerationSettings settings, MarkovNameGenerator nameGenerator, int zoneCount, float linkDensity)
    {
        var outputSamples = WeightedSampleElimination.GeneratePoints(zoneCount,
            settings.CloudDensity,
            v => (.2f - lengthsq(v - float2(.5f))) * 4);
        var sector = new Sector();
        foreach (var v in outputSamples) sector.Zones.Add(new SectorZone {Position = v, Name = nameGenerator.NextName});
        
        // Create a delaunay triangulation to connect adjacent sectors
        var triangulation = DelaunayTriangulation<Vertex2<SectorZone>, Cell2<SectorZone>>
            .Create(sector.Zones.Select(z => new Vertex2<SectorZone>(z.Position, z)).ToList(), 1e-5f);
        var links = new List<(SectorZone, SectorZone)>();
        foreach(var cell in triangulation.Cells)
        {
            links.Add((cell.Vertices[0].StoredObject, cell.Vertices[1].StoredObject));
            links.Add((cell.Vertices[1].StoredObject, cell.Vertices[2].StoredObject));
            links.Add((cell.Vertices[0].StoredObject, cell.Vertices[2].StoredObject));
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
        while (heap.Count > linkDensity * links.Count)
        {
            var link = heap.PopObj();
            if(sector.ConnectedRegion(link.Item1, link.Item2).Contains(link.Item2))
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

        return sector;
    }
}