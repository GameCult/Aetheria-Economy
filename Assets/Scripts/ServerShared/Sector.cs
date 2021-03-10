using System.Collections.Generic;
using Unity.Mathematics;

public class Sector
{
    public List<SectorZone> Zones = new List<SectorZone>();
}

public class SectorZone
{
    public string Name;
    public float2 Position;
    public List<SectorZone> AdjacentZones = new List<SectorZone>();
}