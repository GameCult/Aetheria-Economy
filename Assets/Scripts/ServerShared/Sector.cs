using System;
using System.Collections.Generic;
using System.Linq;
using DataStructures.ViliWonka.Heap;
using Unity.Mathematics;
using JM.LinqFaster;
using static Unity.Mathematics.math;

public class Sector
{
    public List<SectorZone> Zones = new List<SectorZone>();
    public Dictionary<MegaCorporation, SectorZone> HomeZones = new Dictionary<MegaCorporation, SectorZone>();
    public Dictionary<MegaCorporation, SectorZone> BossZones = new Dictionary<MegaCorporation, SectorZone>();
    public SectorZone Entrance;
    public SectorZone Exit;

    private SectorZone[] _exitPath;

    public SectorZone[] ExitPath
    {
        get
        {
            return _exitPath ??= FindPath(Entrance, Exit);
        }
    }

    class DijkstraVertex
    {
        public float Cost;
        public DijkstraVertex Parent;
        public SectorZone Zone;
    }
    
    // public HashSet<SectorZone> ConnectedRegion(SectorZone v, SectorZone ignoreZone = null)
    // {
    //     var members = new HashSet<SectorZone> {v};
    //     while (true)
    //     {
    //         var lastCount = members.Count;
    //         // For each member, add all vertices that are connected to it but are not already a member
    //         // Also, if there is a zone being ignored, do not traverse across it
    //         foreach (var zone in members.ToArray())
    //             foreach(var adjacentZone in zone.AdjacentZones)
    //                 if (!members.Contains(adjacentZone) && !(zone == v && ignoreZone == adjacentZone))
    //                     members.Add(adjacentZone);
    //         // If we have stopped finding neighbors, stop traversing
    //         if (members.Count == lastCount)
    //             return members;
    //     }
    // }

    //public int TotalDistance(SectorZone v) => ConnectedRegionDistance(v).Sum(x => x.distance);
    
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
        SectorZone ignoreLink1,
        SectorZone ignoreLink2,
        int maxDistance = int.MaxValue)
    {
        return ConnectedRegion(v, x => x.Item1 != ignoreLink1 || x.Item2 != ignoreLink2, maxDistance);
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
        Predicate<(SectorZone,SectorZone)> linkFilter, 
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
    public MegaCorporation[] Megas;
    //public List<MegaCorporation> Megas;
    public MegaCorporation Owner;
}