using System;
using System.Collections.Generic;
using System.Linq;
using DataStructures.ViliWonka.Heap;
using MIConvexHull;
using JM.LinqFaster;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Random = Unity.Mathematics.Random;

public class Sector
{
    public Dictionary<MegaCorporation, SectorZone> HomeZones = new Dictionary<MegaCorporation, SectorZone>();
    public Dictionary<MegaCorporation, SectorZone> BossZones = new Dictionary<MegaCorporation, SectorZone>();
    
    public SectorZone[] Zones { get; }
    public SectorZone Entrance { get; }
    public SectorZone Exit { get; }

    private SectorZone[] _exitPath;

    public SectorZone[] ExitPath
    {
        get
        {
            return _exitPath ??= FindPath(Entrance, Exit);
        }
    }

    public Sector(DatabaseCache database, SavedGame savedGame)
    {
        var factions = savedGame.Factions.Select(database.Get<MegaCorporation>).ToArray();
        Zones = savedGame.Zones.Select(zone =>
        {
            return new SectorZone
            {
                Name = zone.Name,
                Position = zone.Position
            };
        }).ToArray();
        for (var i = 0; i < Zones.Length; i++)
        {
            Zones[i].AdjacentZones = savedGame.Zones[i].AdjacentZones.Select(azi => Zones[azi]).ToList();
            Zones[i].Factions = savedGame.Zones[i].Factions.Select(mi => factions[mi]).ToArray();
            Zones[i].Owner = factions[savedGame.Zones[i].Owner];
        }

        HomeZones = savedGame.HomeZones.ToDictionary(
            x => factions[x.Key], 
            x => Zones[x.Value]);

        BossZones = savedGame.BossZones.ToDictionary(
            x => factions[x.Key], 
            x => Zones[x.Value]);

        Entrance = Zones[savedGame.Entrance];
        Exit = Zones[savedGame.Exit];

        CalculateDistanceMatrix();
    }

    public Sector(SectorGenerationSettings settings, MegaCorporation[] factions, ref Random random, float minLineSeparation = .01f)
    {
        var outputSamples = WeightedSampleElimination.GeneratePoints(settings.ZoneCount,
            settings.CloudDensity,
            v => (.2f - lengthsq(v - float2(.5f))) * 4);
        Zones = outputSamples.Select(v => new SectorZone {Position = v}).ToArray();

        #region Link Placement

        // Create a delaunay triangulation to connect adjacent sectors
        var triangulation = DelaunayTriangulation<Vertex2<SectorZone>, Cell2<SectorZone>>
            .Create(Zones.Select(z => new Vertex2<SectorZone>(z.Position, z)).ToList(), 1e-5f);
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
            foreach (var zone in Zones)
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
            if(ConnectedRegion(link.Item1, link.Item1, link.Item2).Contains(link.Item2))
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

        CalculateDistanceMatrix();
        
        // Entrance is the most isolated zone (highest total distance to all other zones
        Entrance = Zones.MaxBy(z => z.Isolation);

        // Exit is the zone furthest from the exit
        Exit = Zones.MaxBy(z => Exit.Distance[z]);
        
        // Find all zones on the exit path where removing that zone would disconnect the entrance from the exit
        // Disregard "corridor" zones with only two adjacent zones
        var chokePoints = ExitPath
            .Where(z => z.AdjacentZones.Count > 2 && !ConnectedRegion(Entrance, z).Contains(Exit));

        // Choose some megas to have bosses placed based on whether a boss hull is assigned
        var bossMegas = factions
            .Where(m => m.BossHull != Guid.Empty)
            .Take(settings.BossCount)
            .ToArray();

        // Place boss zones along the critical path as far apart from each other as possible
        foreach (var mega in bossMegas)
        {
            BossZones[mega] = chokePoints.MaxBy(z =>
                Exit.Distance[z] * Entrance.Distance[z] * 
                BossZones.Values.Aggregate(1, (i, os) => i * os.Distance[z]));
        }

        // Place boss mega headquarters such that their sphere of influence encompasses their boss zone
        // While occupying as much territory as possible
        foreach (var mega in bossMegas)
        {
            HomeZones[mega] = ConnectedRegion(BossZones[mega], mega.InfluenceDistance)
                .MaxBy(z =>
                    ConnectedRegion(z, mega.InfluenceDistance).Count *
                    HomeZones.Values.Aggregate(1f, (i, os) => i * sqrt(os.Distance[z])));
        }
        
        // Place remaining headquarters away from existing megas while also maximizing territory
        foreach (var mega in factions.Where(m=>!bossMegas.Contains(m)))
        {
            HomeZones[mega] = Zones.MaxBy(z =>
                pow(ConnectedRegion(z, mega.InfluenceDistance).Count, HomeZones.Count) *
                Exit.Distance[z] * Entrance.Distance[z] * 
                HomeZones.Values.Aggregate(1f, (i, os) => i * sqrt(os.Distance[z])) * 
                BossZones.Values.Aggregate(1f, (i, os) => i * sqrt(os.Distance[z])));
        }

        // Assign faction presence
        foreach (var zone in Zones)
        {
            // Factions are present in all zones within their sphere of influence
            zone.Factions = factions
                .Where(m => zone.Distance[HomeZones[m]] <= m.InfluenceDistance)
                .ToArray();
            
            // Owner of a zone is the faction with the nearest headquarters
            var nearestFaction = factions.MinBy(m => zone.Distance[HomeZones[m]]);
            if (zone.Distance[HomeZones[nearestFaction]] <= nearestFaction.InfluenceDistance)
                zone.Owner = nearestFaction;
        }

        // Generate zone name using the owner's name generator, otherwise assign catalogue ID
        foreach (var zone in Zones) 
            zone.Name = zone.Owner?.NameGenerator.NextName ?? $"EAC-{random.NextInt(99999).ToString()}";
    }

    // Cache distance matrix and calculate isolation for every zone (used extensively for placing stuff)
    private void CalculateDistanceMatrix()
    {
        foreach (var zone in Zones)
        {
            zone.Distance = ConnectedRegionDistance(zone);
            zone.Isolation = zone.Distance.Sum(x => x.Value);
        }
    }

    class DijkstraVertex
    {
        public float Cost;
        public DijkstraVertex Parent;
        public SectorZone Zone;
    }
    
    public Dictionary<SectorZone, int> ConnectedRegionDistance(SectorZone v)
    {
        var members = new Dictionary<SectorZone, int> {{v, 0}};
        int cost = 1;
        while (true)
        {
            var lastCount = members.Count;
            // For each member, add all vertices that are connected to it but are not already a member
            // Also, if there is a zone being ignored, do not traverse across it
            foreach (var member in members.Keys.ToArray())
            foreach (var adjacentZone in member.AdjacentZones)
            {
                if(!members.ContainsKey(adjacentZone))
                    members.Add(adjacentZone, cost);
            }
            // If we have stopped finding neighbors, stop traversing
            if (members.Count == lastCount)
                return members;
            cost++;
        }
    }

    public HashSet<SectorZone> ConnectedRegion(
        SectorZone v,
        SectorZone ignoreLinkSource,
        SectorZone ignoreLinkTarget,
        int maxDistance = int.MaxValue)
    {
        return ConnectedRegion(v, x => x.Item1 != ignoreLinkSource || x.Item2 != ignoreLinkTarget, maxDistance);
    }

    public HashSet<SectorZone> ConnectedRegion(
        SectorZone v,
        SectorZone ignoreZone,
        int maxDistance = int.MaxValue)
    {
        return ConnectedRegion(v, x => x.Item2 != ignoreZone, maxDistance);
    }

    public HashSet<SectorZone> ConnectedRegion(
        SectorZone v,
        int maxDistance = int.MaxValue)
    {
        return ConnectedRegion(v, x => true, maxDistance);
    }
    
    public HashSet<SectorZone> ConnectedRegion(
        SectorZone v, 
        Predicate<(SectorZone source, SectorZone target)> linkFilter, 
        int maxDistance = int.MaxValue)
    {
        var visited = new HashSet<SectorZone> {v};
        int cost = 1;
        while (true)
        {
            var lastCount = visited.Count;
            // For each member, add all nodes that are connected to it but have not been visited
            // Also, if there is a zone being ignored, do not traverse across it
            foreach (var zone in visited.ToArray())
            foreach (var adjacentZone in zone.AdjacentZones)
            {
                if(!visited.Contains(adjacentZone) && linkFilter((zone, adjacentZone)))
                    visited.Add(adjacentZone);
            }
            // If we have stopped finding neighbors, stop traversing
            if (visited.Count == lastCount || cost == maxDistance)
                return visited;
            cost++;
        }
    }
    
    public SectorZone[] FindPath(SectorZone source, SectorZone target, bool bestFirst = false)
    {
        MinHeap<DijkstraVertex> unsearchedNodes = new MinHeap<DijkstraVertex>();
        unsearchedNodes.PushObj(new DijkstraVertex{Zone = source}, 0);
        //SortedList<float,DijkstraVertex> members = new SortedList<float,DijkstraVertex>{{0, new DijkstraVertex{Zone = source}}};
        var searched = new HashSet<SectorZone>();
        while (true)
        {
            if(unsearchedNodes.Count == 0) return null;  // No nodes left unsearched
            var s = unsearchedNodes.PopObj(); // Lowest cost unsearched node
            if (s.Zone == target) // We found the path
            {
                Stack<DijkstraVertex> path = new Stack<DijkstraVertex>(); // Since we start at the end, use a LIFO collection
                path.Push(s);
                while(path.Peek().Parent!=null) // Keep pushing until we reach the start, which has no parent
                    path.Push(path.Peek().Parent);
                return path.Select(dv => dv.Zone).ToArray();
            }
            // For each adjacent star (filter already visited stars unless heuristic is in use)
            IEnumerable<SectorZone> zonesToSearch = s.Zone.AdjacentZones;
            if (!bestFirst) zonesToSearch = zonesToSearch.Where(z => !searched.Contains(z));
            foreach (var dijkstraStar in zonesToSearch
                    // Cost is parent cost plus distance squared
                    .Select(zone => new DijkstraVertex {Parent = s, Zone = zone, Cost = s.Cost + lengthsq(s.Zone.Position - zone.Position)}))
                // Add new member to list, sorted by cost plus optional heuristic
                unsearchedNodes.PushObj(dijkstraStar, bestFirst ? dijkstraStar.Cost + lengthsq(dijkstraStar.Zone.Position - target.Position) : dijkstraStar.Cost);
            searched.Add(s.Zone);
        }
    }
}

public class SectorZone
{
    public string Name;
    public float2 Position;
    public List<SectorZone> AdjacentZones = new List<SectorZone>();
    public int Isolation;
    public Dictionary<SectorZone, int> Distance;
    public MegaCorporation[] Factions;
    public MegaCorporation Owner;
    public Zone Contents;
}