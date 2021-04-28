using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using Random = Unity.Mathematics.Random;

public abstract class ZoneSelector
{
    public abstract SectorZone SelectZone(List<SectorZone> candidates);
}

public abstract class OrderedZoneSelector : ZoneSelector
{
    public bool Flip = false;
    
    protected abstract Func<SectorZone, IComparable> Comparison { get; }

    public override SectorZone SelectZone(List<SectorZone> candidates)
    {
        var sorted = candidates.OrderBy(Comparison);
        return Flip ? sorted.Last() : sorted.First();
    }
}

public class DistanceSelector : OrderedZoneSelector
{
    private SectorZone Target { get; }
    
    
    public DistanceSelector(string[] args, IZoneResolver zoneResolver)
    {
        Target = zoneResolver.ResolveZone(args[0].Trim());
    }

    protected override Func<SectorZone, IComparable> Comparison => zone => Target?.Distance[zone] ?? 0;
}

public class RandomSelector : ZoneSelector
{
    private Random Random { get; }
    
    public RandomSelector(ref Random random)
    {
        Random = random;
    }
    
    public override SectorZone SelectZone(List<SectorZone> candidates)
    {
        return candidates[Random.NextInt(candidates.Count)];
    }
}