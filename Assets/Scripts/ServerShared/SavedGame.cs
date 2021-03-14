using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[MessagePackObject]
public class SavedGame
{
    [Key(0)]
    public SavedZone[] Zones;
    
    [Key(1)]
    public Guid[] Factions;
    
    [Key(2)]
    public Dictionary<int, int> HomeZones;
    
    [Key(3)]
    public Dictionary<int, int> BossZones;
    
    [Key(4)]
    public int Entrance;
    
    [Key(5)]
    public int Exit;
    
    [Key(6)]
    public int CurrentZone;
    
    [Key(7)]
    public int CurrentZoneEntity;
    
    public SavedGame() { }

    public SavedGame(Sector sector)
    {
        Factions = sector.HomeZones.Keys.Select(f => f.ID).ToArray();
        
        HomeZones = sector.HomeZones.ToDictionary(
            x => Array.IndexOf(Factions, x.Key.ID),
            x => Array.IndexOf(sector.Zones, x.Value));
        BossZones = sector.BossZones.ToDictionary(
            x => Array.IndexOf(Factions, x.Key.ID),
            x => Array.IndexOf(sector.Zones, x.Value));
        
        Zones = sector.Zones.Select(zone=> new SavedZone
        {
            Name = zone.Name,
            Position = zone.Position,
            AdjacentZones = zone.AdjacentZones.Select(az=> Array.IndexOf(sector.Zones, az)).ToArray(),
            Factions = zone.Factions.Select(f=> Array.IndexOf(Factions, f.ID)).ToArray(),
            Contents = zone.Contents.Pack(),
            Owner = Array.IndexOf(Factions, zone.Owner.ID)
        }).ToArray();
        
        Entrance = Array.IndexOf(sector.Zones, sector.Entrance);
        Exit = Array.IndexOf(sector.Zones, sector.Exit);
    }
}

[MessagePackObject]
public class SavedZone
{
    [Key(0)]
    public string Name;
    
    [Key(1)]
    public float2 Position;
    
    [Key(2)]
    public int[] AdjacentZones;
    
    [Key(3)]
    public int[] Factions;
    
    [Key(4)]
    public int Owner;
    
    [Key(5)]
    public ZonePack Contents;
}