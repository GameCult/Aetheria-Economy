using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataStructures.ViliWonka.Heap;
using MIConvexHull;
using JM.LinqFaster;
using UniRx;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Random = Unity.Mathematics.Random;

public class Sector
{
    public Dictionary<Faction, SectorZone> HomeZones = new Dictionary<Faction, SectorZone>();
    public Dictionary<Faction, SectorZone> BossZones = new Dictionary<Faction, SectorZone>();
    public HashSet<SectorZone> DiscoveredZones = new HashSet<SectorZone>();
    
    public SectorBackgroundSettings Background { get; }
    public NameGeneratorSettings NameGeneratorSettings { get; }
    public Faction[] Factions { get; }
    public SectorZone[] Zones { get; }
    public SectorZone Entrance { get; }
    public SectorZone Exit { get; }

    private HashSet<Guid> _containedFactions;
    private SectorZone[] _exitPath;
    private Dictionary<Faction, MarkovNameGenerator> _nameGenerators = new Dictionary<Faction, MarkovNameGenerator>();

    public SectorZone[] ExitPath
    {
        get
        {
            if(Entrance!=null && Exit != null)
                return _exitPath ??= FindPath(Entrance, Exit);
            return null;
        }
    }

    public Sector(CultCache cultCache, SavedGame savedGame)
    {
        Background = savedGame.Background;
        Factions = savedGame.Factions.Select(cultCache.Get<Faction>).ToArray();
        Zones = savedGame.Zones.Select(zone =>
        {
            return new SectorZone
            {
                Name = zone.Name,
                Position = zone.Position,
                PackedContents = zone.Contents
            };
        }).ToArray();
        foreach (var i in savedGame.DiscoveredZones) DiscoveredZones.Add(Zones[i]);
        for (var i = 0; i < Zones.Length; i++)
        {
            Zones[i].AdjacentZones = savedGame.Zones[i].AdjacentZones.Select(azi => Zones[azi]).ToList();
            Zones[i].Factions = savedGame.Zones[i].Factions.Select(mi => Factions[mi]).ToArray();
            Zones[i].Owner = savedGame.Zones[i].Owner < 0 ? null : Factions[savedGame.Zones[i].Owner];
        }

        HomeZones = savedGame.HomeZones.ToDictionary(
            x => Factions[x.Key], 
            x => Zones[x.Value]);

        BossZones = savedGame.BossZones.ToDictionary(
            x => Factions[x.Key], 
            x => Zones[x.Value]);

        Entrance = Zones[savedGame.Entrance];
        if(savedGame.Exit != -1)
            Exit = Zones[savedGame.Exit];

        CalculateDistanceMatrix();
    }

    public Sector(
        SectorGenerationSettings settings, 
        SectorBackgroundSettings background, 
        NameGeneratorSettings nameGeneratorSettings, 
        CultCache cache, 
        Action<string> progressCallback = null,
        uint seed = 0)
    {
        Background = background;
        var factions = cache.GetAll<Faction>();
        var random = new Random(seed == 0 ? (uint) (DateTime.Now.Ticks % uint.MaxValue) : seed);
        Factions = factions.OrderBy(x => random.NextFloat()).Take(settings.MegaCount).ToArray();

        Zones = GenerateZones(settings.ZoneCount, ref random, progressCallback);

        GenerateLinks(settings.LinkDensity, progressCallback);

        CalculateDistanceMatrix(progressCallback);

        // Exit is the most isolated zone (highest total distance to all other zones)
        Exit = Zones.MaxBy(z => z.Isolation);
        
        // Entrance is the zone furthest from the exit
        Entrance = Zones.MaxBy(z => Exit.Distance[z]);
        
        DiscoveredZones.Add(Entrance);
        foreach(var z in Entrance.AdjacentZones) DiscoveredZones.Add(z);
        
        PlaceFactionsMain(settings.BossCount, progressCallback);

        CalculateFactionInfluence(progressCallback);

        GenerateNames(cache, nameGeneratorSettings, ref random, progressCallback);

        progressCallback?.Invoke("Done!");
        if(progressCallback!=null) Thread.Sleep(500); // Inserting Delay to make it seem like it's doing more work lmao
    }

    public Sector(
        TutorialGenerationSettings settings,
        SectorBackgroundSettings background,
        NameGeneratorSettings nameGeneratorSettings,
        CultCache cache,
        Action<string> progressCallback = null,
        uint seed = 0)
    {
        Faction ResolveFaction(string name) => cache.GetAll<Faction>().FirstOrDefault(f => f.Name.StartsWith(name, StringComparison.InvariantCultureIgnoreCase));

        Background = background;
        var random = new Random(seed == 0 ? (uint) (DateTime.Now.Ticks % uint.MaxValue) : seed);
        
        var factions = new List<Faction>();

        var protagonistFaction = ResolveFaction(settings.ProtagonistFaction);
        factions.Add(protagonistFaction);
        
        var antagonistFaction = ResolveFaction(settings.AntagonistFaction);
        factions.Add(antagonistFaction);
        
        var bufferFaction = ResolveFaction(settings.BufferFaction);
        factions.Add(bufferFaction);
        
        var questFaction = ResolveFaction(settings.QuestFaction);
        factions.Add(questFaction);
        
        var neutralFactions = settings.NeutralFactions
            .Select(ResolveFaction)
            .ToArray();
        factions.AddRange(neutralFactions);
        
        Factions = factions.ToArray();
        foreach (var faction in Factions) faction.InfluenceDistance = (faction.InfluenceDistance + 1) / 2;

        Zones = GenerateZones(settings.ZoneCount, ref random, progressCallback);

        GenerateLinks(settings.LinkDensity, progressCallback);

        CalculateDistanceMatrix(progressCallback);

        HomeZones[protagonistFaction] = Zones
            .MaxBy(z => ConnectedRegion(z, protagonistFaction.InfluenceDistance).Count);

        HomeZones[antagonistFaction] = Zones
            .MaxBy(z => ConnectedRegion(z, antagonistFaction.InfluenceDistance).Count * z.Distance[HomeZones[protagonistFaction]]);

        HomeZones[protagonistFaction] = Zones
            .MaxBy(z => ConnectedRegion(z, protagonistFaction.InfluenceDistance).Count * sqrt(z.Distance[HomeZones[antagonistFaction]]));
        
        // var antagonistRegion = ConnectedRegion(HomeZones[antagonistFaction], antagonistFaction.InfluenceDistance);
        // var protagonistRegion = ConnectedRegion(HomeZones[protagonistFaction], protagonistFaction.InfluenceDistance);

        // Place the buffer faction in a zone where it has equal distance to the pro/antagonist HQs and where it can control the most territory
        var bufferDistance = Zones.Min(z => abs(z.Distance[HomeZones[antagonistFaction]] - z.Distance[HomeZones[protagonistFaction]]));
        var potentialBufferZones = Zones
            .Where(z => abs(z.Distance[HomeZones[antagonistFaction]] - z.Distance[HomeZones[protagonistFaction]]) == bufferDistance);
        HomeZones[bufferFaction] = potentialBufferZones.MaxBy(z => ConnectedRegion(z, bufferFaction.InfluenceDistance).Count);
        
        // Place neutral headquarters away from existing factions while also maximizing territory
        foreach (var faction in neutralFactions)
        {
            HomeZones[faction] = Zones.MaxBy(z =>
                ConnectedRegion(z, faction.InfluenceDistance).Count *
                HomeZones.Values.Aggregate(1f, (i, os) => i * sqrt(os.Distance[z])));
        }
        
        CalculateFactionInfluence(progressCallback);

        var potentialQuestZones = Zones
            .Where(z => z.Factions.Contains(antagonistFaction) && z.Factions.Contains(bufferFaction));
        HomeZones[questFaction] = potentialQuestZones
            .MaxBy(z=>z.Distance[HomeZones[antagonistFaction]] * ConnectedRegion(z, questFaction.InfluenceDistance).Count);
        
        CalculateFactionInfluence(progressCallback);

        Entrance = Zones.Where(z => z.Owner == null).MinBy(z => z.Distance[HomeZones[protagonistFaction]]);
        
        DiscoveredZones.Add(Entrance);
        foreach(var z in Entrance.AdjacentZones) DiscoveredZones.Add(z);

        CalculateFactionInfluence(progressCallback);

        GenerateNames(cache, nameGeneratorSettings, ref random, progressCallback);

        progressCallback?.Invoke("Done!");
        if(progressCallback!=null) Thread.Sleep(500); // Inserting Delay to make it seem like it's doing more work lmao
    }

    private SectorZone[] GenerateZones(int zoneCount, ref Random random, Action<string> progressCallback = null)
    {
        var outputSamples = WeightedSampleElimination.GeneratePoints(zoneCount,
            ref random,
            Background.CloudDensity,
            v => (.2f - lengthsq(v - float2(.5f))) * 4,
            progressCallback);
        return outputSamples.Select(v => new SectorZone {Position = v}).ToArray();
    }

    private void PlaceFactionsMain(int bossCount, Action<string> progressCallback = null)
    {
        progressCallback?.Invoke("Finding Chokepoints");
        if(progressCallback!=null) Thread.Sleep(500); // Inserting Delay to make it seem like it's doing more work lmao
        
        // Find all zones on the exit path where removing that zone would disconnect the entrance from the exit
        // Disregard "corridor" zones with only two adjacent zones
        var chokePoints = ExitPath
            .Where(z => z.AdjacentZones.Count > 2 && !ConnectedRegion(Entrance, z).Contains(Exit));

        progressCallback?.Invoke("Placing Factions");
        if (progressCallback != null) Thread.Sleep(500); // Inserting Delay to make it seem like it's doing more work lmao

        // Choose some megas to have bosses placed based on whether a boss hull is assigned
        var bossMegas = Factions
            .Where(m => m.BossHull != Guid.Empty)
            .Take(bossCount)
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
        foreach (var mega in Factions.Where(m => !bossMegas.Contains(m)))
        {
            HomeZones[mega] = Zones.MaxBy(z =>
                pow(ConnectedRegion(z, mega.InfluenceDistance).Count, HomeZones.Count) *
                Exit.Distance[z] * Entrance.Distance[z] *
                HomeZones.Values.Aggregate(1f, (i, os) => i * sqrt(os.Distance[z])) *
                BossZones.Values.Aggregate(1f, (i, os) => i * sqrt(os.Distance[z])));
        }
    }

    private void CalculateFactionInfluence(Action<string> progressCallback = null)
    {
        progressCallback?.Invoke("Calculating Faction Influence");
        if (progressCallback != null) Thread.Sleep(500); // Inserting Delay to make it seem like it's doing more work lmao

        // Assign faction presence
        foreach (var zone in Zones)
        {
            // Factions are present in all zones within their sphere of influence
            zone.Factions = Factions
                .Where(f => HomeZones.ContainsKey(f))
                .Where(f => zone.Distance[HomeZones[f]] <= f.InfluenceDistance)
                .ToArray();

            // Owner of a zone is the faction with the nearest headquarters
            var nearestFaction = Factions
                .Where(f => HomeZones.ContainsKey(f))
                .MinBy(f => (float)zone.Distance[HomeZones[f]]);
            if (zone.Distance[HomeZones[nearestFaction]] <= nearestFaction.InfluenceDistance)
                zone.Owner = nearestFaction;
        }
    }

    private void GenerateNames(CultCache cache,
        NameGeneratorSettings nameGeneratorSettings,
        ref Random random,
        Action<string> progressCallback = null)
    {
        for (var i = 0; i < Factions.Length; i++)
        {
            progressCallback?.Invoke($"Feeding Markov Chains: {i + 1} / {Factions.Length}");
            //if(progressCallback!=null) Thread.Sleep(250); // Inserting Delay to make it seem like it's doing more work lmao
            var faction = Factions[i];
            _nameGenerators[faction] = new MarkovNameGenerator(ref random, cache.Get<NameFile>(faction.GeonameFile).Names, nameGeneratorSettings);
        }

        // Generate zone name using the owner's name generator, otherwise assign catalogue ID
        foreach (var zone in Zones)
        {
            if (zone.Owner != null)
            {
                zone.Name = _nameGenerators[zone.Owner].NextName.Trim();
            }
            else
            {
                zone.Name = $"EAC-{random.NextInt(9999).ToString()}";
            }
        }
    }

    private void GenerateLinks(float linkDensity, Action<string> progressCallback = null)
    {
        progressCallback?.Invoke("Triangulating Zone Positions");
        if (progressCallback != null) Thread.Sleep(500); // Inserting Delay to make it seem like it's doing more work lmao

        // Create a delaunay triangulation to connect adjacent sectors
        var triangulation = DelaunayTriangulation<Vertex2<SectorZone>, Cell2<SectorZone>>
            .Create(Zones.Select(z => new Vertex2<SectorZone>(z.Position, z)).ToList(), 1e-7f);
        var links = new HashSet<(SectorZone, SectorZone)>();
        foreach (var cell in triangulation.Cells)
        {
            if (!links.Contains((cell.Vertices[0].StoredObject, cell.Vertices[1].StoredObject)) &&
                !links.Contains((cell.Vertices[1].StoredObject, cell.Vertices[0].StoredObject)))
                links.Add((cell.Vertices[0].StoredObject, cell.Vertices[1].StoredObject));
            if (!links.Contains((cell.Vertices[1].StoredObject, cell.Vertices[2].StoredObject)) &&
                !links.Contains((cell.Vertices[2].StoredObject, cell.Vertices[1].StoredObject)))
                links.Add((cell.Vertices[1].StoredObject, cell.Vertices[2].StoredObject));
            if (!links.Contains((cell.Vertices[0].StoredObject, cell.Vertices[2].StoredObject)) &&
                !links.Contains((cell.Vertices[2].StoredObject, cell.Vertices[0].StoredObject)))
                links.Add((cell.Vertices[0].StoredObject, cell.Vertices[2].StoredObject));
        }

        progressCallback?.Invoke("Eliminating Zone Links");
        if (progressCallback != null) Thread.Sleep(500); // Inserting Delay to make it seem like it's doing more work lmao
        // foreach (var link in links.ToArray())
        // {
        //     foreach (var zone in Zones)
        //     {
        //         if(zone != link.Item1 && zone != link.Item2)
        //             if (AetheriaMath.FindDistanceToSegment(zone.Position, link.Item1.Position, link.Item2.Position, out _) < minLineSeparation)
        //                 links.Remove(link);
        //     }
        // }

        foreach (var link in links)
        {
            link.Item1.AdjacentZones.Add(link.Item2);
            link.Item2.AdjacentZones.Add(link.Item1);
        }

        float LinkWeight((SectorZone, SectorZone) link)
        {
            return 1 / saturate(Background.CloudDensity((link.Item1.Position + link.Item2.Position) / 2)) *
                   lengthsq(link.Item1.Position - link.Item2.Position) *
                   (link.Item1.AdjacentZones.Count - 1) * (link.Item2.AdjacentZones.Count - 1);
        }

        var heap = new MaxHeap<(SectorZone, SectorZone)>(links.Count);
        foreach (var link in links) heap.PushObj(link, LinkWeight(link));
        while (heap.Count > linkDensity * links.Count)
        {
            var link = heap.PopObj();
            if (ConnectedRegion(link.Item1, link.Item1, link.Item2).Contains(link.Item2))
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
    }

    // Cache distance matrix and calculate isolation for every zone (used extensively for placing stuff)
    private void CalculateDistanceMatrix(Action<string> progressCallback = null)
    {
        progressCallback?.Invoke("Calculating Distance Matrix");
        if(progressCallback!=null) Thread.Sleep(500); // Inserting Delay to make it seem like it's doing more work lmao
        foreach (var zone in Zones)
        {
            zone.Distance = ConnectedRegionDistance(zone);
            zone.Isolation = zone.Distance.Sum(x => x.Value);
        }
    }

    public bool ContainsFaction(Guid factionID)
    {
        _containedFactions ??= new HashSet<Guid>(Factions.Select(f => f.ID));
        return _containedFactions.Contains(factionID);
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
    public Faction[] Factions;
    public Faction Owner;
    public Zone Contents;
    public ZonePack PackedContents;
}