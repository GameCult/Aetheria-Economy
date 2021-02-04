/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using UniRx;
using UnityEngine;
using static Unity.Mathematics.math;
using Unity.Mathematics;
using UnityEngine.Serialization;
using float2 = Unity.Mathematics.float2;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class SectorRenderer : MonoBehaviour
{
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
    private Dictionary<InstantWeaponData, InstantWeaponEffectManager> _instantWeaponManagers = new Dictionary<InstantWeaponData, InstantWeaponEffectManager>();
    private Dictionary<ConstantWeaponData, ConstantWeaponEffectManager> _constantWeaponManagers = new Dictionary<ConstantWeaponData, ConstantWeaponEffectManager>();
    private Zone _zone;
    private float _viewDistance;
    private float _maxDepth;

    private int _tourIndex = -1;
    private float _tourTimer;
    private List<(Transform, Transform)> _tourPlanets = new List<(Transform, Transform)>();
    private CinemachineTransposer _transposer;
    private PlanetObject _root;
    private bool _rootFound;
    
    public ItemManager ItemManager { get; set; }

    public float Time { get; set; }

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
        ViewDistance = Settings.DefaultViewDistance;
        MinimapDistance = Settings.MinimapZoomLevels[Settings.DefaultMinimapZoom];
        
        _tourTimer = TourSwitchTime;
        _transposer = SceneCamera.GetCinemachineComponent<CinemachineTransposer>();
    }

    public void LoadZone(Zone zone)
    {
        _maxDepth = 0;
        _zone = zone;
        SectorBrushes.localScale = zone.Data.Radius * 2 * Vector3.one;
        ClearZone();
        foreach(var p in zone.Planets.Values)
            LoadPlanet(p);
        
        foreach (var x in Planets)
        {
            if (!(zone.Planets[x.Key] is GasGiantData))
            {
                var parentOrbit = _zone.Orbits[_zone.Planets[x.Key].Orbit].Data.Parent;
                PlanetObject parent;
                if (parentOrbit == Guid.Empty)
                    parent = _root;
                else
                {
                    var parentPlanetData = _zone.Planets.Values.FirstOrDefault(p => p.Orbit == parentOrbit);
                    if(parentPlanetData == null)
                    {
                        parentOrbit = _zone.Orbits[parentOrbit].Data.Parent;
                        parentPlanetData = _zone.Planets.Values.FirstOrDefault(p => p.Orbit == parentOrbit);
                    }

                    if (parentPlanetData == null)
                        parent = _root;
                    else if (!Planets.TryGetValue(parentPlanetData.ID, out parent))
                    {
                        Debug.Log("WTF!");
                    }
                }
                _tourPlanets.Add((x.Value.Body.transform, parent.Body.transform));
            }
        }
        
        foreach(var entity in zone.Entities)
            LoadEntity(entity);
        zone.Entities.ObserveAdd().Subscribe(e => LoadEntity(e.Value));
        zone.Entities.ObserveRemove().Subscribe(e => UnloadEntity(e.Value));
    }

    public void ClearZone()
    {
        if (Planets.Count > 0)
        {
            foreach (var planet in Planets.Values)
            {
                DestroyImmediate(planet.gameObject);
            }
            Planets.Clear();
            _beltMeshes.Clear();
            _beltMatrices.Clear();
            _tourPlanets.Clear();
        }
    }

    void LoadEntity(Entity entity)
    {
        var hullData = ItemManager.GetData(entity.Hull) as HullData;
        EntityInstance instance;
        if (entity is Ship ship)
        {
            instance = new ShipInstance();
            instance.Transform = Instantiate(UnityHelpers.LoadAsset<GameObject>(hullData.Prefab), ZoneRoot).transform;
            instance.Prefab = instance.Transform.GetComponent<EntityPrefab>();
            var shipInstance = (ShipInstance) instance;
            shipInstance.Particles = ship.GetBehaviors<Thruster>()
                .Select<Thruster, (Thruster effect, ParticleSystem system, float baseEmission)>(x =>
                {
                    var effectData = (ThrusterData) x.Data;
                    var particles = Instantiate(UnityHelpers.LoadAsset<ParticleSystem>(effectData.ParticlesPrefab), shipInstance.Transform, false);
                    var particlesShape = particles.shape;
                    particlesShape.meshRenderer = shipInstance.Prefab.ThrusterHardpoints
                        .FirstOrDefault(t => t.name == x.Entity.Hardpoints[x.Item.Position.x, x.Item.Position.y].Transform)
                        ?.Emitter;
                    return (x, particles, particles.emission.rateOverTimeMultiplier);
                })
                .ToArray();
        }
        else
        {
            instance = new EntityInstance();
            instance.Transform = Instantiate(UnityHelpers.LoadAsset<GameObject>(hullData.Prefab), ZoneRoot).transform;
            instance.Prefab = instance.Transform.GetComponent<EntityPrefab>();
        }

        instance.Entity = entity;
        foreach (var hullCollider in instance.Prefab.HullColliders) hullCollider.Entity = entity;

        foreach (var item in entity.Equipment)
        {
            foreach (var behavior in item.Behaviors)
            {
                if (behavior is InstantWeapon instantWeapon)
                {
                    var data = (InstantWeaponData) instantWeapon.Data;
                    if (!_instantWeaponManagers.ContainsKey(data))
                    {
                        var managerPrefab = UnityHelpers.LoadAsset<InstantWeaponEffectManager>(data.EffectPrefab);
                        if(managerPrefab)
                        {
                            _instantWeaponManagers.Add(data, Instantiate(managerPrefab, EffectManagerParent));
                        }
                        else Debug.LogError($"No InstantWeaponEffectManager prefab found at path {data.EffectPrefab}");
                    }

                    instantWeapon.OnFire += () => 
                        _instantWeaponManagers[data].Fire(instantWeapon, item, instance, entity.Target.Value != null && EntityInstances.ContainsKey(entity.Target.Value) ? EntityInstances[entity.Target.Value] : null);

                    if (behavior is ChargedWeapon chargedWeapon)
                    {
                        var chargeManager = _instantWeaponManagers[data].GetComponent<ChargeEffectManager>();
                        if (chargeManager)
                        {
                            chargedWeapon.OnStartCharging += () => chargeManager.StartCharging(chargedWeapon, item, instance);
                            chargedWeapon.OnStopCharging += () => chargeManager.StopCharging(chargedWeapon);
                            chargedWeapon.OnCharged += () => chargeManager.Charged(chargedWeapon);
                            chargedWeapon.OnFailed += () => chargeManager.Failed(chargedWeapon);
                        }
                    }
                }

                if (behavior is ConstantWeapon constantWeapon)
                {
                    var data = (ConstantWeaponData) constantWeapon.Data;
                    if (!_constantWeaponManagers.ContainsKey(data))
                    {
                        var managerPrefab = UnityHelpers.LoadAsset<ConstantWeaponEffectManager>(data.EffectPrefab);
                        if(managerPrefab)
                        {
                            _constantWeaponManagers.Add(data, Instantiate(managerPrefab, EffectManagerParent));
                        }
                        else Debug.LogError($"No ConstantWeaponEffectManager prefab found at path {data.EffectPrefab}");
                    }

                    constantWeapon.OnStartFiring += () => 
                        _constantWeaponManagers[data].StartFiring(data, item, instance, entity.Target.Value != null ? EntityInstances[entity.Target.Value] : null);
                    constantWeapon.OnStopFiring += () => 
                        _constantWeaponManagers[data].StopFiring(item);
                }
            }
        }
        instance.RadiatorMeshes = new Dictionary<HardpointData, MeshRenderer>();
        instance.Barrels = new Dictionary<HardpointData, Transform[]>();
        instance.BarrelIndices = new Dictionary<HardpointData, int>();
        foreach (var hp in hullData.Hardpoints)
        {
            if (hp.Type == HardpointType.Radiator)
            {
                var mesh = instance.Prefab.RadiatorHardpoints.FirstOrDefault(x => x.name == hp.Transform);
                if (mesh)
                {
                    instance.RadiatorMeshes.Add(hp, mesh.Mesh);
                }
            }
            if(hp.Type == HardpointType.Ballistic || hp.Type == HardpointType.Energy || hp.Type == HardpointType.Launcher)
            {
                var whp = instance.Prefab.WeaponHardpoints.FirstOrDefault(x => x.name == hp.Transform);
                if (whp)
                {
                    instance.Barrels.Add(hp, whp.FiringPoint);
                    instance.BarrelIndices.Add(hp, 0);
                }
            }
        }

        void DamageSchematic(float damage, Shape hitShape)
        {
            foreach (var v in hitShape.Coordinates)
                hitShape[v] = hitShape[v] && hullData.Shape[v];

            float hullDamage = 0;
            var damagePerCell = damage / hitShape.Coordinates.Length;
            foreach (var v in hitShape.Coordinates)
            {
                var d = damagePerCell;
                
                // Subtract surface damage from armor, passing on the remainder to the item and then to the hull
                var prev = entity.Armor[v.x, v.y];
                entity.Armor[v.x, v.y] = max(prev - d, 0);
                entity.ArmorDamage.OnNext((v, d));
                d = max(d - prev, 0);

                if (d > 0.1f)
                {
                    var item = entity.GearOccupancy[v.x, v.y];
                    if (item != null)
                    {
                        prev = item.EquippableItem.Durability;
                        item.EquippableItem.Durability = max(prev - d, 0);
                        entity.ItemDamage.OnNext((item, d));
                        d = max(d - prev, 0);
                    }
                }

                hullDamage += d;
            }

            if(hullDamage > .1f)
            {
                entity.Hull.Durability -= hullDamage;
                entity.HullDamage.OnNext(hullDamage);
            }
        }

        foreach (var collider in instance.Prefab.HullColliders)
        {
            collider.Splash.Subscribe(splash =>
            {
                var hitShape = new Shape(hullData.Shape.Width, hullData.Shape.Height);
                foreach (var v in hullData.Shape.Coordinates)
                {
                    var localHitDirection = instance.Prefab.transform.InverseTransformDirection(splash.Direction);
                    var direction = normalize(float2(localHitDirection.x, localHitDirection.z));
                    var cellDot = dot(normalize(v - hullData.Shape.CenterOfMass), direction);
                    if (cellDot < 0) hitShape[v] = true;
                }
                DamageSchematic(splash.Damage, hitShape);
            });
            
            collider.Hit.Subscribe(hit =>
            {
                var hardpointIndex = (int) hit.Hit.textureCoord.x - 1;
                
                var hitShape = new Shape(hullData.Shape.Width, hullData.Shape.Height);

                // U coordinate between 0-1 indicates a hit that didn't land directly on a hardpoint
                // Find the 2D position of the hit scaled to the schematic
                float2 hitPos = float2.zero;
                if (hardpointIndex < 0)
                {
                    hitPos = float2(hit.Hit.textureCoord.x * hullData.Shape.Width, hit.Hit.textureCoord.y * hullData.Shape.Height);
                    // Search all schematic border cells for the cell which is closest to the hit position
                    var hitCell = int2(-1);
                    var distance = float.MaxValue;
                    foreach (var v in hullData.Shape.Coordinates)
                    {
                        var cellDist = lengthsq(hitPos - v);
                        if (cellDist < distance)
                        {
                            distance = cellDist;
                            hitCell = v;
                        }
                    }

                    hitShape[hitCell] = true;
                }
                else
                {
                    // Collider UV coordinates starting with 1 correspond to hardpoint index
                    var hardpoint = hullData.Hardpoints[hardpointIndex];
                    
                    // Obtain the hull coordinates of all cells occupied by the hardpoint
                    var hardpointCells = hullData.Shape.Inset(hardpoint.Shape, hardpoint.Position);
                    hitPos = hardpointCells.CenterOfMass;
                    foreach (var v in hardpointCells.Coordinates)
                        hitShape[v] = true;
                }
                
                for (int i = 0; i < hit.Spread - 1; i++)
                {
                    hitShape.Expand();
                }

                if (hit.Penetration > .5f)
                {
                    // Find the local 2D vector corresponding to the direction of the incoming hit
                    var localHitDirection = instance.Prefab.transform.InverseTransformDirection(hit.Direction);
                    var penetrationVector = normalize(float2(localHitDirection.x, localHitDirection.z));

                    // March a ray through the ship from the hit position
                    var penetrationPoint = hitPos;
                    var penetrationDistance = 0;
                    while (penetrationDistance < hit.Penetration)
                    {
                        penetrationPoint += penetrationVector * .5f;
                        hitShape[int2(penetrationPoint)] = true;
                    }
                }
                
                DamageSchematic(hit.Damage, hitShape);
            });
        }

        instance.LookAtPoint = new GameObject($"{entity.Name} Look Point").transform;
        
        foreach (var articulationPoint in instance.Prefab.ArticulationPoints)
        {
            articulationPoint.Target = instance.LookAtPoint;
        }
        EntityInstances.Add(entity, instance);
    }

    void UnloadEntity(Entity entity)
    {
        if (EntityInstances[entity].Prefab.DestroyEffect != null)
        {
            var t = Instantiate(EntityInstances[entity].Prefab.DestroyEffect).transform;
            t.position = EntityInstances[entity].Prefab.transform.position;
        }

        foreach (var item in entity.Equipment)
        {
            foreach (var behavior in item.Behaviors)
            {
                if(behavior is Weapon weapon)
                    weapon.ResetEvents();
            }
        }
        
        Destroy(EntityInstances[entity].Transform.gameObject);
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
            var belt = new AsteroidBeltUI(_zone, _zone.AsteroidBelts[beltData.ID], beltObject, collider, AsteroidSpritesheetWidth, AsteroidSpritesheetHeight, Settings.MinimapAsteroidSize);
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
                var gasGiant = _zone.PlanetInstances[planetData.ID] as GasGiant;
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

            var planetInstance = _zone.PlanetInstances[planetData.ID];
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
        foreach(var belt in _zone.AsteroidBelts)
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
            var planetInstance = _zone.PlanetInstances[planet.Key];
            var p = _zone.GetOrbitPosition(planetInstance.BodyData.Orbit);
            planet.Value.transform.position = new Vector3(p.x, 0, p.y);
            planet.Value.Body.transform.localPosition = new Vector3(0, _zone.GetHeight(p) + planetInstance.BodyRadius.Value * 2, 0);
            if(planet.Value is GasGiantObject gasGiantObject)
            {
                gasGiantObject.GravityWaves.material.SetFloat("_Phase", Time * ((GasGiant) _zone.PlanetInstances[planet.Key]).GravityWavesSpeed.Value);
                if(!(planet.Value is SunObject))
                {
                    var toParent = normalize(_zone.GetOrbitPosition(_zone.Orbits[planetInstance.BodyData.Orbit].Data.Parent) - p);
                    gasGiantObject.SunMaterial.LightingDirection = new Vector3(toParent.x, 0, toParent.y);
                }
            }
            else planet.Value.Body.transform.rotation *= Quaternion.AngleAxis(Settings.PlanetRotationSpeed, Vector3.up);
        }

        var hitList = new List<Entity>();
        foreach (var entity in EntityInstances)
        {
            if(entity.Key.Hull.Durability <= 0)
            {
                hitList.Add(entity.Key);
                continue;
            }
            if(entity.Value is ShipInstance shipInstance)
            {
                foreach (var (effect, system, baseEmission) in shipInstance.Particles)
                {
                    var emissionModule = system.emission;
                    var item = effect.Item.EquippableItem;
                    var data = shipInstance.Entity.ItemManager.GetData(item);
                    emissionModule.rateOverTimeMultiplier = baseEmission * effect.Axis * (item.Durability / data.Durability);
                }

                entity.Value.Transform.rotation = ((Ship) entity.Key).Rotation;
            }

            foreach (var x in entity.Value.RadiatorMeshes)
            {
                var temp = 0f;
                foreach (var v in x.Key.Shape.Coordinates)
                {
                    var v2 = v + x.Key.Position;
                    temp += entity.Key.Temperature[v2.x, v2.y];
                }
                temp /= x.Key.Shape.Coordinates.Length;
                x.Value.material.SetFloat("_Emission", Settings.GameplaySettings.TemperatureEmissionCurve.Evaluate(temp));
            }

            foreach (var x in entity.Value.Barrels)
            {
                entity.Key.HardpointTransforms[x.Key] = (x.Value[0].position, x.Value[0].forward);
            }

            if (entity.Key.Target.Value != null && !EntityInstances.ContainsKey(entity.Key.Target.Value))
                entity.Key.Target.Value = null;
            entity.Value.LookAtPoint.position = entity.Value.Transform.position + (Vector3) entity.Key.LookDirection * (entity.Key.Target.Value != null
                ? max((EntityInstances[entity.Key.Target.Value].Transform.position - entity.Value.Transform.position).magnitude,Settings.GameplaySettings.ConvergenceMinimumDistance) : 10000);
            entity.Value.Transform.position = entity.Key.Position;
        }
        foreach(var e in hitList)
        {
            _zone.Entities.Remove(e);
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

public class EntityInstance
{
    public Entity Entity;
    public Transform Transform;
    public EntityPrefab Prefab;
    public Dictionary<HardpointData, Transform[]> Barrels;
    public Dictionary<HardpointData, int> BarrelIndices;
    public Dictionary<HardpointData, MeshRenderer> RadiatorMeshes;
    public Transform LookAtPoint;
    public Transform GetBarrel(HardpointData hardpoint)
    {
        if (Barrels.ContainsKey(hardpoint))
        {
            var barrel = Barrels[hardpoint][BarrelIndices[hardpoint]];
            BarrelIndices[hardpoint] = (BarrelIndices[hardpoint] + 1) % Barrels[hardpoint].Length;
            return barrel;
        }

        return Transform;
    }
}

public class ShipInstance : EntityInstance
{
    public (Thruster effect, ParticleSystem system, float baseEmission)[] Particles;
}

[Serializable]
public class InstancedMesh
{
    public Mesh Mesh;
    public Material Material;
}

public class AsteroidBeltUI
{
    private MeshFilter _filter;
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
        _filter = meshFilter;
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

        _filter.mesh = _mesh;
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