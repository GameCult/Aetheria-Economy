using System;
using System.Linq;
using JsonKnownTypes;
using MessagePack;
using Newtonsoft.Json;
// TODO: USE THIS EVERYWHERE
using Unity.Mathematics;
using static Unity.Mathematics.math;

public interface INamedEntry
{
    string EntryName { get; set; }
}

[InspectableField, 
 MessagePackObject, 
 Union(0, typeof(SimpleCommodityData)), 
 Union(1, typeof(CompoundCommodityData)),
 Union(2, typeof(GearData)), 
 Union(3, typeof(HullData)), 
 Union(4, typeof(SimpleCommodity)), 
 Union(5, typeof(CompoundCommodity)), 
 Union(6, typeof(Gear)),
 Union(7, typeof(BlueprintData)),
 Union(8, typeof(GalaxyMapLayerData)),
 Union(9, typeof(GlobalData)), 
 Union(10, typeof(ZoneData)), 
 Union(11, typeof(PlayerData)), 
 Union(12, typeof(Corporation)),
 Union(13, typeof(MegaCorporation)),
 Union(14, typeof(OrbitalEntity)), 
 Union(15, typeof(OrbitData)), 
 Union(16, typeof(BodyData)),
 Union(17, typeof(PersonalityAttribute)),
 Union(18, typeof(AgentTask)),
 Union(19, typeof(LoadoutData)),
 Union(20, typeof(Ship)), 
 Union(21, typeof(Mining)), 
 Union(22, typeof(StationTowing)), 
 Union(23, typeof(Survey)), 
 Union(24, typeof(HaulingTask)), 
 Union(25, typeof(AsteroidBeltData)), 
 Union(26, typeof(GasGiantData)), 
 Union(27, typeof(SunData)), 
 Union(28, typeof(PlanetData)), 
 JsonObject(MemberSerialization.OptIn), JsonConverter(typeof(JsonKnownTypesConverter<DatabaseEntry>))]
//[Union(21, typeof(ContractData))]
//[Union(22, typeof(Station))]
public abstract class DatabaseEntry
{
    [JsonProperty("id"), Key(0)]
    public Guid ID = Guid.NewGuid();
    [IgnoreMember] public GameContext Context { get; set; }
}