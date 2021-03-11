using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using JM.LinqFaster;
using static Unity.Mathematics.math;

public class Sector
{
    public List<SectorZone> Zones = new List<SectorZone>();
    
    class DijkstraVertex
    {
        public float Cost;
        public DijkstraVertex Parent;
        public SectorZone Zone;
    }
    
    public List<SectorZone> ConnectedRegion(SectorZone v, SectorZone ignoreZone = null)
    {
        List<SectorZone> members = new List<SectorZone>();
        members.Add(v);
        while (true)
        {
            int lastCount = members.Count;
            // For each member, add all vertices that are connected to it but are not already a member
            // Also, if there is a zone being ignored, do not traverse across it
            foreach (SectorZone m in members.ToArray())
                members.AddRange(m.AdjacentZones
                    .Where(z => !members.Contains(z) && !(m == v && ignoreZone == z)));
            // If we have stopped finding neighbors, stop traversing
            if (members.Count == lastCount)
                return members;
        }
    }
    
    public List<SectorZone> FindPath(SectorZone source, SectorZone target, bool bestFirst = false)
    {
        SortedList<float,DijkstraVertex> members = new SortedList<float,DijkstraVertex>{{0, new DijkstraVertex{Zone = source}}};
        List<DijkstraVertex> searched = new List<DijkstraVertex>();
        while (true)
        {
            var s = members.FirstOrDefault(m => !searched.Contains(m.Value)).Value; // Lowest cost unsearched node
            if (s == null) return null; // No vertices left unsearched
            if (s.Zone == target) // We found the path
            {
                Stack<DijkstraVertex> path = new Stack<DijkstraVertex>(); // Since we start at the end, use a LIFO collection
                path.Push(s);
                while(path.Peek().Parent!=null) // Keep pushing until we reach the start, which has no parent
                    path.Push(path.Peek().Parent);
                return path.Select(dv => dv.Zone).ToList();
            }
            // For each adjacent star (filter already visited stars unless heuristic is in use)
            foreach (var dijkstraStar in s.Zone.AdjacentZones
                    .WhereSelectF(zone => !bestFirst || members.All(m => m.Value.Zone != zone),
                    // Cost is parent cost plus distance squared
                    zone => new DijkstraVertex {Parent = s, Zone = zone, Cost = s.Cost + lengthsq(s.Zone.Position - zone.Position)}))
                // Add new member to list, sorted by cost plus optional heuristic
                members.Add(bestFirst ? dijkstraStar.Cost + lengthsq(dijkstraStar.Zone.Position - target.Position) : dijkstraStar.Cost, dijkstraStar);
            searched.Add(s);
        }
    }
}

public class SectorZone
{
    public string Name;
    public float2 Position;
    public List<SectorZone> AdjacentZones = new List<SectorZone>();
}