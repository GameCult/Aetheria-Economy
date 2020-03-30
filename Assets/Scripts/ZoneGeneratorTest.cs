using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using Random = UnityEngine.Random;

public class ZoneGeneratorTest : MonoBehaviour
{
    public Galaxy Galaxy;
    public MeshRenderer ZoneBackground;
    public MeshRenderer ZoneBoundary;
    public ZoneObject ZoneObjectPrefab;
    public Texture2D PlanetSprite;
    public Texture2D GasGiantSprite;
    public Texture2D SunSprite;
    public float ZoneSizeScale = .01f;
    public float ZoneMassScale = .5f;
    public float ZoneMassPower = .33f;
    public float ZoneOrbitSpeed = 10;
    
    private DatabaseCache _cache;
    private Dictionary<Guid, ZoneObject> _zoneObjects = new Dictionary<Guid, ZoneObject>();
    private List<ZoneObject> _wormholes = new List<ZoneObject>();
    private Dictionary<Guid, float2> _orbitPositions = new Dictionary<Guid, float2>();
    private GameContext _context;
    private Material _boundaryMaterial;

    // Determine orbital position recursively, caching parent positions to avoid repeated calculations
    private float2 GetOrbitPosition(Guid orbit)
    {
        // Root orbit is fixed at center
        if(orbit==Guid.Empty)
            return Unity.Mathematics.float2.zero;
        
        if (!_orbitPositions.ContainsKey(orbit))
        {
            var orbitData = _cache.Get<OrbitData>(orbit);
            _orbitPositions[orbit] = GetOrbitPosition(orbitData.Parent) + (orbitData.Period < .01f ? Unity.Mathematics.float2.zero : 
                OrbitData.Evaluate(Time.time * ZoneOrbitSpeed / orbitData.Period + orbitData.Phase) *
                orbitData.Distance);
        }

        var position = _orbitPositions[orbit];
        if (float.IsNaN(position.x))
        {
            Debug.Log("Orbit position is NaN, something went very wrong!");
            return Unity.Mathematics.float2.zero;
        }
        return _orbitPositions[orbit];
    }
    
    void Start()
    {
        _cache = new DatabaseCache();
        _context = new GameContext(_cache);
        _boundaryMaterial = ZoneBoundary.material;
    }

    void Update()
    {
        _orbitPositions.Clear();
        foreach (var planet in _zoneObjects)
        {
            var planetData = _cache.Get<PlanetData>(planet.Key);
                
            planet.Value.transform.position = (Vector2) GetOrbitPosition(planetData.Orbit) * ZoneSizeScale;
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PopulateZone();
        }
    }
    
    void PopulateZone()
    {
        // Delete all the existing zone contents
        foreach (var zoneObject in _zoneObjects.Values) Destroy(zoneObject.gameObject);//zoneObject.GetComponent<Prototype>().ReturnToPool();
        _zoneObjects.Clear();
        foreach (var zoneObject in _wormholes) Destroy(zoneObject.gameObject); //zoneObject.GetComponent<Prototype>().ReturnToPool();
        _wormholes.Clear();

        float2 position;
        do
        {
            position = float2(Random.value, Random.value);
        } while (Galaxy.MapData.StarDensity.Evaluate(position, Galaxy.MapData.GlobalData) < .1f);
        
        var zone = new ZoneData{Context = _context, Name = Guid.NewGuid().ToString(), Position = position};
        OrbitData[] orbits;
        PlanetData[] planets;
        ZoneGenerator.GenerateZone(
            global: Galaxy.MapData.GlobalData, 
            zone: zone, 
            mass: Galaxy.MapData.ResourceDensities.First(m=>m.Name=="Mass"),
            radius: Galaxy.MapData.ResourceDensities.First(m=>m.Name=="Radius"),
            orbitData: out orbits, 
            planetData: out planets);
        _cache.AddAll(orbits);
        _cache.AddAll(planets);
        _cache.Add(zone);

        float zoneDepth = 0;
        foreach (var planet in planets)
        {
            var planetObject = Instantiate(ZoneObjectPrefab, transform);
            _zoneObjects[planet.ID] = planetObject;
            planetObject.Label.text = planet.Name;
            planetObject.GravityMesh.gameObject.SetActive(true);
            planetObject.GravityMesh.transform.localScale =
                Vector3.one * (pow(planet.Mass, Galaxy.MapData.GlobalData.GravityRadiusExponent) *
                               Galaxy.MapData.GlobalData.GravityRadiusMultiplier * 2 * ZoneSizeScale);
            var depth = pow(planet.Mass, ZoneMassPower) * ZoneMassScale;
            if (depth > zoneDepth)
                zoneDepth = depth;
            planetObject.GravityMesh.material.SetFloat("_Depth", depth);
            planetObject.Icon.material.SetTexture("_MainTex",
                planet.Mass > Galaxy.MapData.GlobalData.SunMass ? SunSprite :
                planet.Mass > Galaxy.MapData.GlobalData.GasGiantMass ? GasGiantSprite :
                PlanetSprite);
        }

        var radius = (zone.Radius * ZoneSizeScale * 2);
        ZoneBackground.material.SetFloat("_ClipDistance", radius);
        ZoneBackground.material.SetFloat("_HeightRange", zoneDepth + _boundaryMaterial.GetFloat("_Depth"));
        ZoneBoundary.transform.localScale = Vector3.one * radius;
    }
}
