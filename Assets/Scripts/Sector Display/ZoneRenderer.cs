﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;

public class ZoneRenderer : MonoBehaviour
{
    public Transform WormholePrefab;
    public float EntityFadeTime;
    public Transform EffectManagerParent;
    public Transform FogCameraParent;
    public GameSettings Settings;
    public Transform ZoneRoot;
    public Transform SectorBrushes;
    public MeshRenderer SectorBoundaryBrush;
    public CinemachineVirtualCamera SceneCamera;
    public Camera[] FogCameras;
    public Camera[] MinimapCameras;
    public Material FogMaterial;
    public float FogFarFadeFraction = .125f;
    public float FarPlaneDistanceMultiplier = 2;
    public InstancedMesh[] AsteroidMeshes;
    public int AsteroidSpritesheetWidth = 4;
    public int AsteroidSpritesheetHeight = 4;
    public LODHandler LODHandler;
    
    [Header("Tour")]
    public bool Tour;
    public float TourSwitchTime = 5f;
    public float TourFollowDistance = 30f;
    public float TourHeightOffset = 15;
    public float TourFollowOffsetDegrees;

    // public Mesh[] AsteroidMeshes;
    // public Material AsteroidMaterial;

    [Header("Prefabs")]
    public MeshFilter AsteroidBeltUI;
    public PlanetObject Planet;
    public GasGiantObject GasGiant;
    public SunObject Sun;
    
    [Header("Icons")]
    public Texture2D PlanetoidIcon;
    public Texture2D PlanetIcon;
    
    [HideInInspector]
    public Dictionary<Entity, EntityInstance> EntityInstances = new Dictionary<Entity, EntityInstance>();
    [HideInInspector]
    public Dictionary<Guid, PlanetObject> Planets = new Dictionary<Guid, PlanetObject>();
    
    private Dictionary<Guid, AsteroidBeltUI> _beltObjects = new Dictionary<Guid, AsteroidBeltUI>();
    private Dictionary<Guid, InstancedMesh[]> _beltMeshes = new Dictionary<Guid, InstancedMesh[]>();
    private Dictionary<Guid, Matrix4x4[][]> _beltMatrices = new Dictionary<Guid, Matrix4x4[][]>();
    private float _viewDistance;
    private float _maxDepth;

    private int _tourIndex = -1;
    private float _tourTimer;
    private List<(Transform, Transform)> _tourPlanets = new List<(Transform, Transform)>();
    private CinemachineTransposer _transposer;
    private PlanetObject _root;
    private bool _rootFound;
    private Entity _perspectiveEntity;
    private IDisposable[] _perspectiveSubscriptions = new IDisposable[2];

    public Dictionary<Wormhole, GameObject> WormholeInstances = new Dictionary<Wormhole, GameObject>();

    public Zone Zone { get; private set; }
    public ItemManager ItemManager { get; set; }

    public Entity PerspectiveEntity
    {
        get => _perspectiveEntity;
        set
        {
            _perspectiveEntity = value;
            _perspectiveSubscriptions[0]?.Dispose();
            _perspectiveSubscriptions[1]?.Dispose();
            if (value == null)
            {
                foreach (var e in EntityInstances.Values)
                    e.FadeOut(EntityFadeTime);
            }
            else
            {
                foreach(var entity in EntityInstances.Values)
                    entity.FadeOut(EntityFadeTime);
                foreach(var entity in value.VisibleEntities)
                    EntityInstances[entity].FadeIn(EntityFadeTime);
                EntityInstances[value].FadeIn(EntityFadeTime);
                _perspectiveSubscriptions[0] = value.VisibleEntities.ObserveAdd()
                    .Subscribe(add => EntityInstances[add.Value].FadeIn(EntityFadeTime));
                _perspectiveSubscriptions[1] = value.VisibleEntities.ObserveRemove()
                    .Where(removeEvent => EntityInstances.ContainsKey(removeEvent.Value))
                    .Subscribe(removeEvent => EntityInstances[removeEvent.Value].FadeOut(EntityFadeTime));
            }
        }
    }

    public float GameTime { get; set; }

    public float ViewDistance
    {
        set
        {
            _viewDistance = value;
            foreach (var camera in FogCameras)
                camera.orthographicSize = value;
            SceneCamera.m_Lens.FarClipPlane = value * FarPlaneDistanceMultiplier;
            FogMaterial.SetFloat("_DepthCeiling", value);
            FogMaterial.SetFloat("_DepthBlend", FogFarFadeFraction * value);
        }
    }

    public float MinimapDistance
    {
        set
        {
            foreach(var camera in MinimapCameras)
                camera.orthographicSize = value;
            // MinimapGravityQuad.transform.localScale = value * 2 * Vector3.one;
        }
    }

    void Start()
    {
        EntityInstance.EffectManagerParent = EffectManagerParent;
        ViewDistance = Settings.DefaultViewDistance;
        MinimapDistance = Settings.MinimapZoomLevels[Settings.DefaultMinimapZoom];
        
        _tourTimer = TourSwitchTime;
        _transposer = SceneCamera.GetCinemachineComponent<CinemachineTransposer>();
    }

    public void LoadZone(Zone zone)
    {
        ClearZone();
        _maxDepth = 0;
        Zone = zone;
        SectorBrushes.localScale = zone.Pack.Radius * 2 * Vector3.one;
        foreach(var p in zone.Planets.Values)
            LoadPlanet(p);
        
        foreach (var x in Planets)
        {
            if (!(zone.Planets[x.Key] is GasGiantData))
            {
                var parentOrbit = Zone.Orbits[Zone.Planets[x.Key].Orbit].Data.Parent;
                PlanetObject parent;
                if (parentOrbit == Guid.Empty)
                    parent = _root;
                else
                {
                    var parentPlanetData = Zone.Planets.Values.FirstOrDefault(p => p.Orbit == parentOrbit);
                    if(parentPlanetData == null)
                    {
                        parentOrbit = Zone.Orbits[parentOrbit].Data.Parent;
                        parentPlanetData = Zone.Planets.Values.FirstOrDefault(p => p.Orbit == parentOrbit);
                    }

                    if (parentPlanetData == null)
                        parent = _root;
                    else if (!Planets.TryGetValue(parentPlanetData.ID, out parent))
                    {
                        Debug.Log("WTF!");
                    }
                }
                //_tourPlanets.Add((x.Value.Body.transform, parent.Body.transform));
            }
        }
        
        foreach(var entity in zone.Entities)
            LoadEntity(entity);
        zone.Entities.ObserveAdd().Subscribe(e => LoadEntity(e.Value));
        zone.Entities.ObserveRemove().Subscribe(e => UnloadEntity(e.Value));
        
        if(zone.SectorZone != null)
        {
            foreach (var adjacentZone in zone.SectorZone.AdjacentZones)
            {
                var dir = normalize(adjacentZone.Position - zone.SectorZone.Position);
                AddWormhole(new Wormhole
                {
                    Target = adjacentZone,
                    Position = dir * Settings.DefaultZoneRadius * Settings.WormholeDistanceRatio
                });
            }
        }
    }

    public void AddWormhole(Wormhole wormhole)
    {
        var instance = Instantiate(WormholePrefab);
        instance.position = new Vector3(wormhole.Position.x, 0, wormhole.Position.y);
        WormholeInstances.Add(wormhole, instance.gameObject);
    }

    public void ClearZone()
    {
        if (Zone == null) return;
        foreach (var wormhole in WormholeInstances.Values) Destroy(wormhole);
        WormholeInstances.Clear();

        foreach (var entity in Zone.Entities)
        {
            UnloadEntity(entity);
        }
        
        if (Planets.Count > 0)
        {
            foreach (var planet in Planets.Values)
            {
                DestroyImmediate(planet.gameObject);
            }
            Planets.Clear();
            foreach (var beltObject in _beltObjects.Values)
            {
                Destroy(beltObject.Filter);
            }
            _beltObjects.Clear();
            _beltMeshes.Clear();
            _beltMatrices.Clear();
            _tourPlanets.Clear();
        }
    }

    void LoadEntity(Entity entity)
    {
        var hullData = ItemManager.GetData(entity.Hull) as HullData;
        EntityInstance instance;
        if (entity is Ship)
        {
            instance = Instantiate(UnityHelpers.LoadAsset<GameObject>(hullData.Prefab), ZoneRoot).GetComponent<ShipInstance>();
            if (instance == null)
            {
                ItemManager.Log($"Failed to instantiate {hullData.Name} ship with invalid prefab: no ShipInstance component!");
                return;
            }
        }
        else
        {
            instance = Instantiate(UnityHelpers.LoadAsset<GameObject>(hullData.Prefab), ZoneRoot).GetComponent<EntityInstance>();
            if (instance == null)
            {
                ItemManager.Log($"Failed to instantiate {hullData.Name} entity with invalid prefab: no EntityInstance component!");
                return;
            }
        }

        instance.SetEntity(this, entity);
        EntityInstances.Add(entity, instance);
    }

    public void UnloadEntity(Entity entity)
    {
        foreach (var item in entity.Equipment)
        {
            foreach (var behavior in item.Behaviors)
            {
                if(behavior is Weapon weapon)
                    weapon.ResetEvents();
            }
        }
        
        Destroy(EntityInstances[entity].gameObject);
        EntityInstances.Remove(entity);
    }

    void LoadPlanet(BodyData planetData)
    {
        if (planetData is AsteroidBeltData beltData)
        {
            var meshes = AsteroidMeshes.ToList();
            while(meshes.Count > Settings.AsteroidMeshCount)
                meshes.RemoveAt(Random.Range(0,meshes.Count));
            _beltMeshes[planetData.ID] = meshes.ToArray();
            _beltMatrices[planetData.ID] = new Matrix4x4[meshes.Count][];
            var count = beltData.Asteroids.Length / meshes.Count;
            var remainder = beltData.Asteroids.Length - count * meshes.Count;
            for (int i = 0; i < meshes.Count; i++)
            {
                _beltMatrices[planetData.ID][i] = new Matrix4x4[i<meshes.Count-1 ? count : count+remainder];
            }
            
            var beltObject = Instantiate(AsteroidBeltUI, ZoneRoot);
            var collider = beltObject.GetComponent<MeshCollider>();
            var belt = new AsteroidBeltUI(Zone, Zone.AsteroidBelts[beltData.ID], beltObject, collider, AsteroidSpritesheetWidth, AsteroidSpritesheetHeight, Settings.MinimapAsteroidSize);
            _beltObjects[beltData.ID] = belt;
        }
        else
        {
            PlanetObject planet;
            if (planetData is GasGiantData gasGiantData)
            {
                if (planetData is SunData sunData)
                {
                    planet = Instantiate(Sun, ZoneRoot);
                    var sun = (SunObject) planet;
                    sunData.LightColor.Subscribe(c => sun.Light.color = c.ToColor());
                    sunData.Mass.Subscribe(m => sun.Light.range = Settings.PlanetSettings.LightRadius.Evaluate(m));
                    sunData.FogTintColor.Subscribe(c => sun.FogTint.material.SetColor("_Color", c.ToColor()));
                    sunData.Mass.Subscribe(m => sun.FogTint.transform.localScale = Settings.PlanetSettings.FogTintRadius.Evaluate(m) * Vector3.one);
                }
                else planet = Instantiate(GasGiant, ZoneRoot);

                var gas = (GasGiantObject) planet;
                var gasGiant = Zone.PlanetInstances[planetData.ID] as GasGiant;
                gasGiantData.Colors.Subscribe(c => gas.Body.material.SetTexture("_ColorRamp", c.ToGradient(!(planetData is SunData)).ToTexture()));
                gasGiantData.AlbedoRotationSpeed.Subscribe(f => gas.SunMaterial.AlbedoRotationSpeed = f);
                gasGiantData.FirstOffsetRotationSpeed.Subscribe(f => gas.SunMaterial.FirstOffsetRotationSpeed = f);
                gasGiantData.SecondOffsetRotationSpeed.Subscribe(f => gas.SunMaterial.SecondOffsetRotationSpeed = f);
                gasGiantData.FirstOffsetDomainRotationSpeed.Subscribe(f => gas.SunMaterial.FirstOffsetDomainRotationSpeed = f);
                gasGiantData.SecondOffsetDomainRotationSpeed.Subscribe(f => gas.SunMaterial.SecondOffsetDomainRotationSpeed = f);
                gasGiant.GravityWavesRadius.Subscribe(f => gas.GravityWaves.transform.localScale = f * Vector3.one);
                gasGiant.GravityWavesDepth.Subscribe(f => gas.GravityWaves.material.SetFloat("_Depth", f));
                planetData.Mass.Subscribe(f => gas.GravityWaves.material.SetFloat("_Frequency", Settings.PlanetSettings.WaveFrequency.Evaluate(f)));
                    //gas.WaveScroll.Speed = Properties.GravitySettings.WaveSpeed.Evaluate(f);
            }
            else
            {
                planet = Instantiate(Planet, ZoneRoot);
                var possibleSettings = Settings.BodySettingsCollections
                    .Where(p => p.MinimumMass < planetData.Mass.Value)
                    .MaxBy(p => p.MinimumMass).BodySettings;
                planet.Generator.body = possibleSettings[Random.Range(0, possibleSettings.Length)];
                //Debug.Log($"Generating planet with {planetData.Mass} mass! Choosing {planet.Generator.body.name} settings!");
                //planet.Icon.material.mainTexture = planetData.Mass > Context.GlobalData.PlanetMass ? PlanetIcon : PlanetoidIcon;
            }

            var planetInstance = Zone.PlanetInstances[planetData.ID];
            planetInstance.BodyRadius.Subscribe(f =>
            {
                planet.Body.transform.localScale = f * Vector3.one;
            });
            planetInstance.GravityWellRadius.Subscribe(f => planet.GravityWell.transform.localScale = f * Vector3.one);
            planetInstance.GravityWellDepth.Subscribe(f =>
            {
                if (f > _maxDepth) _maxDepth = f;
                planet.GravityWell.material.SetFloat("_Depth", f);
                planet.Icon.transform.position = new Vector3(0, -f, 0);
            });
            planetInstance.BodyData.Mass.Subscribe(f => planet.Icon.transform.localScale = Settings.IconSize.Evaluate(f) * Vector3.one);
            

            Planets[planetData.ID] = planet;
            if (!_rootFound)
            {
                _rootFound = true;
                _root = planet;
            }
        }
        LODHandler.FindPlanets();
    }

    // private void Update()
    // {
    //     if (Tour)
    //     {
    //         _tourTimer -= UnityEngine.Time.deltaTime;
    //         if (_tourTimer < 0)
    //         {
    //             _tourTimer = TourSwitchTime;
    //             _tourIndex = (_tourIndex + 1) % _tourPlanets.Count;
    //             SceneCamera.Follow = _tourPlanets[_tourIndex].Item1;
    //             SceneCamera.LookAt = _tourPlanets[_tourIndex].Item2;
    //             if(_tourIndex==0) Debug.Log("Tour Complete!");
    //         }
    //         // if(_tourIndex>=0)
    //         // {
    //         //     var offset = (SceneCamera.Follow.position - SceneCamera.LookAt.position);
    //         //     offset.y = 0;
    //         //     offset = offset.normalized * TourFollowDistance;
    //         //     offset.y = TourHeightOffset;
    //         //     offset = Quaternion.AngleAxis(TourFollowOffsetDegrees, Vector3.up) * offset;
    //         //     _transposer.m_FollowOffset = offset;
    //         // }
    //     }
    // }

    void Update()
    {
        foreach(var belt in Zone.AsteroidBelts)
        {
            var meshes = _beltMeshes[belt.Key];
            var count = belt.Value.Positions.Length / meshes.Length;
            for (int i = 0; i < meshes.Length; i++)
            {
                for (int t = 0; t < _beltMatrices[belt.Key][i].Length; t++)
                {
                    var tx = t + i * count;
                    _beltMatrices[belt.Key][i][t] = Matrix4x4.TRS(belt.Value.Positions[tx],
                        Quaternion.Euler(
                            cos(belt.Value.Rotations[tx] + (float) i / meshes.Length) * 100,
                            sin(belt.Value.Rotations[tx] + (float) i / meshes.Length) * 100,
                            (float) tx / belt.Value.Positions.Length * 360),
                        Vector3.one * belt.Value.Scales[tx]);
                }

                Graphics.DrawMeshInstanced(meshes[i].Mesh, 0, meshes[i].Material, _beltMatrices[belt.Key][i]);
            }
            _beltObjects[belt.Key].Update(belt.Value.Positions);
        }
        
        foreach (var planet in Planets)
        {
            var planetInstance = Zone.PlanetInstances[planet.Key];
            var p = Zone.GetOrbitPosition(planetInstance.BodyData.Orbit);
            planet.Value.transform.position = new Vector3(p.x, 0, p.y);
            planet.Value.Body.transform.localPosition = new Vector3(0, Zone.GetHeight(p) + planetInstance.BodyRadius.Value * 2, 0);
            if(planet.Value is GasGiantObject gasGiantObject)
            {
                gasGiantObject.GravityWaves.material.SetFloat("_Phase", GameTime * ((GasGiant) Zone.PlanetInstances[planet.Key]).GravityWavesSpeed.Value);
                if(!(planet.Value is SunObject))
                {
                    var toParent = normalize(Zone.GetOrbitPosition(Zone.Orbits[planetInstance.BodyData.Orbit].Data.Parent) - p);
                    gasGiantObject.SunMaterial.LightingDirection = new Vector3(toParent.x, 0, toParent.y);
                }
            }
            else planet.Value.Body.transform.rotation *= Quaternion.AngleAxis(Settings.PlanetRotationSpeed, Vector3.up);
        }

        var fogPos = FogCameraParent.position;
        SectorBoundaryBrush.material.SetFloat("_Power", Settings.PlanetSettings.ZoneDepthExponent);
        SectorBoundaryBrush.material.SetFloat("_Depth", Settings.PlanetSettings.ZoneDepth + Settings.PlanetSettings.ZoneBoundaryFog);
        var startDepth = Zone.PowerPulse(Settings.MinimapZoneGravityRange, Settings.PlanetSettings.ZoneDepthExponent) * Settings.PlanetSettings.ZoneDepth;
        var depthRange = Settings.PlanetSettings.ZoneDepth - startDepth + _maxDepth;
        // MinimapGravityQuad.material.SetFloat("_StartDepth", startDepth);
        // MinimapGravityQuad.material.SetFloat("_DepthRange", depthRange);
        FogMaterial.SetFloat("_GridOffset", Settings.PlanetSettings.ZoneBoundaryFog);
        FogMaterial.SetVector("_GridTransform", new Vector4(fogPos.x,fogPos.z,_viewDistance*2));
        // var gravPos = MinimapGravityQuad.transform.position;
        // gravPos.y = -Settings.PlanetSettings.ZoneDepth - _maxDepth;
        // MinimapGravityQuad.transform.position = gravPos;
        // MinimapTintQuad.transform.position = gravPos - Vector3.up*10;
    }
}


[Serializable]
public class InstancedMesh
{
    public Mesh Mesh;
    public Material Material;
}

public class AsteroidBeltUI
{
    public MeshFilter Filter;
    private MeshCollider _collider;
    private Vector3[] _vertices;
    private Vector3[] _normals;
    private Vector2[] _uvs;
    private int[] _indices;
    private Guid _orbitParent;
    private Mesh _mesh;
    private float _size;
    private Zone _zone;
    private AsteroidBelt _belt;
    private float _scale;

    public AsteroidBeltUI(Zone zone, AsteroidBelt belt, MeshFilter meshFilter, MeshCollider collider, int spritesheetWidth, int spritesheetHeight, float scale)
    {
        _belt = belt;
        _zone = zone;
        Filter = meshFilter;
        _collider = collider;
        var orbit = zone.Orbits[belt.Data.Orbit];
        _orbitParent = orbit.Data.Parent;
        _vertices = new Vector3[_belt.Data.Asteroids.Length*4];
        _normals = new Vector3[_belt.Data.Asteroids.Length*4];
        _uvs = new Vector2[_belt.Data.Asteroids.Length*4];
        _indices = new int[_belt.Data.Asteroids.Length*6];
        _scale = scale;

        var maxDist = 0f;
        var spriteSize = float2(1f / spritesheetWidth, 1f / spritesheetHeight);
        // vertex order: bottom left, top left, top right, bottom right
        for (var i = 0; i < belt.Data.Asteroids.Length; i++)
        {
            if (belt.Data.Asteroids[i].Distance > maxDist)
                maxDist = belt.Data.Asteroids[i].Distance;
            var spriteX = Random.Range(0, spritesheetWidth);
            var spriteY = Random.Range(0, spritesheetHeight);
            
            _uvs[i * 4] = new Vector2(spriteX * spriteSize.x, spriteY * spriteSize.y);
            _uvs[i * 4 + 1] = new Vector2(spriteX * spriteSize.x, spriteY * spriteSize.y + spriteSize.y);
            _uvs[i * 4 + 2] = new Vector2(spriteX * spriteSize.x + spriteSize.x, spriteY * spriteSize.y + spriteSize.y);
            _uvs[i * 4 + 3] = new Vector2(spriteX * spriteSize.x + spriteSize.x, spriteY * spriteSize.y);
            
            _indices[i * 6] = i * 4;
            _indices[i * 6 + 1] = i * 4 + 1;
            _indices[i * 6 + 2] = i * 4 + 3;
            _indices[i * 6 + 3] = i * 4 + 3;
            _indices[i * 6 + 4] = i * 4 + 1;
            _indices[i * 6 + 5] = i * 4 + 2;
        }
        
        for (var i = 0; i < _normals.Length; i++)
        {
            _normals[i] = -Vector3.forward;
        }
        
        _mesh = new Mesh();
        _mesh.vertices = _vertices;
        _mesh.uv = _uvs;
        _mesh.triangles = _indices;
        _mesh.normals = _normals;
        _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * maxDist);
        _size = maxDist;

        Filter.mesh = _mesh;
        //_collider.sharedMesh = _mesh;
    }

    public void Update(float3[] positions)
    {
        var parentPosition = _zone.GetOrbitPosition(_orbitParent);
        for (var i = 0; i < _belt.Data.Asteroids.Length; i++)
        {
            var rotation = Quaternion.Euler(90, _belt.Rotations[i], 0);
            //var position = new Vector3(_belt.Transforms[i].x, _zone.GetHeight(parentPosition), _belt.Transforms[i].y);
            _vertices[i * 4] = rotation * new Vector3(-_belt.Scales[i] * _scale,-_belt.Scales[i] * _scale,0) + (Vector3) positions[i];
            _vertices[i * 4 + 1] = rotation * new Vector3(-_belt.Scales[i] * _scale,_belt.Scales[i] * _scale,0) + (Vector3) positions[i];
            _vertices[i * 4 + 2] = rotation * new Vector3(_belt.Scales[i] * _scale,_belt.Scales[i] * _scale,0) + (Vector3) positions[i];
            _vertices[i * 4 + 3] = rotation * new Vector3(_belt.Scales[i] * _scale,-_belt.Scales[i] * _scale,0) + (Vector3) positions[i];
        }

        _mesh.bounds = new Bounds(new Vector3(parentPosition.x, 0, parentPosition.y), Vector3.one * (_size * 2));
        _mesh.vertices = _vertices;
        //_collider.sharedMesh = _mesh;
    }
}