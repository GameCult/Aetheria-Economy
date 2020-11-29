using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using UniRx;
using UnityEngine;
using static Unity.Mathematics.math;
using Unity.Mathematics;

public class SectorRenderer : MonoBehaviour
{
    public Transform FogCameraParent;
    public SectorDisplayProperties Properties;
    public Transform SectorRoot;
    public Transform SectorBrushes;
    public CinemachineVirtualCamera SceneCamera;
    public Camera[] FogCameras;
    public Camera[] MinimapCameras;
    public Material FogMaterial;
    public float FogFarFadeFraction = .125f;
    public float FarPlaneDistanceMultiplier = 2;
    public Mesh[] AsteroidMeshes;
    public Material AsteroidMaterial;
    
    [Header("Prefabs")]
    public GameObject Belt;
    public PlanetObject Planet;
    public GasGiantObject GasGiant;
    public SunObject Sun;
    
    [Header("Icons")]
    public Texture2D PlanetoidIcon;
    public Texture2D PlanetIcon;
    
    private Dictionary<Guid, Transform> _planets = new Dictionary<Guid, Transform>();
    private Zone _zone;
    private float _viewDistance;
    
    public GameContext Context { get; set; }

    public float ViewDistance
    {
        set
        {
            _viewDistance = value;
            foreach (var camera in FogCameras)
                camera.orthographicSize = value;
            SceneCamera.m_Lens.FarClipPlane = value * FarPlaneDistanceMultiplier;
            FogMaterial.SetFloat("_DepthCeiling", value - FogFarFadeFraction * value);
            FogMaterial.SetFloat("_DepthBlend", FogFarFadeFraction * value);
        }
    }

    public float MinimapDistance
    {
        set
        {
            foreach(var camera in MinimapCameras)
                camera.orthographicSize = value * 2;
        }
    }

    void Start()
    {
        ViewDistance = Properties.DefaultViewDistance;
        MinimapDistance = Properties.DefaultMinimapDistance;
    }

    public void LoadZone(Zone zone)
    {
        _zone = zone;
        _zone.GravitySettings = Properties.GravitySettings;
        SectorBrushes.localScale = zone.Data.Radius * 2 * Vector3.one;
        _zone.ZoneDepth = 50;
        _zone.ZoneDepthExponent = .25f;
        _zone.ZoneDepthRadius = zone.Data.Radius * 2;
        ClearZone();
        foreach(var p in zone.Planets.Values)
            LoadPlanet(p);
    }

    public void ClearZone()
    {
        if (_planets.Count > 0)
        {
            foreach (var planet in _planets.Values)
            {
                DestroyImmediate(planet.gameObject);
            }
            _planets.Clear();
        }
    }

    void LoadPlanet(BodyData planetData)
    {
        if (planetData is AsteroidBeltData beltData)
        {
            
        }
        else
        {
            PlanetObject planet;
            if (planetData is GasGiantData gasGiantData)
            {
                if (planetData is SunData sunData)
                {
                    planet = Instantiate(Sun, SectorRoot);
                    var sun = (SunObject) planet;
                    sunData.LightColor.Subscribe(c => sun.Light.color = c);
                    sunData.Mass.Subscribe(m => sun.Light.range = Properties.LightRadius.Evaluate(m));
                    sunData.FogTintColor.Subscribe(c => sun.FogTint.material.SetColor("_Color", c));
                    sunData.Mass.Subscribe(m => sun.FogTint.transform.localScale = Properties.FogTintRadius.Evaluate(m) * Vector3.one);
                }
                else planet = Instantiate(GasGiant, SectorRoot);

                var gas = (GasGiantObject) planet;
                gasGiantData.Colors.Subscribe(c => gas.GradientMapper.ApplyGradient(c.ToGradient()));
                gasGiantData.AlbedoRotationSpeed.Subscribe(f => gas.SunMaterial.AlbedoRotationSpeed = f);
                gasGiantData.FirstOffsetRotationSpeed.Subscribe(f => gas.SunMaterial.FirstOffsetRotationSpeed = f);
                gasGiantData.SecondOffsetRotationSpeed.Subscribe(f => gas.SunMaterial.SecondOffsetRotationSpeed = f);
                gasGiantData.FirstOffsetDomainRotationSpeed.Subscribe(f => gas.SunMaterial.FirstOffsetDomainRotationSpeed = f);
                gasGiantData.SecondOffsetDomainRotationSpeed.Subscribe(f => gas.SunMaterial.SecondOffsetDomainRotationSpeed = f);
                planetData.Mass.CombineLatest(planetData.GravityRadiusMultiplier,
                        (mass, radius) => Properties.GravitySettings.WaveRadius.Evaluate(mass) * radius)
                    .Subscribe(f => gas.GravityWaves.transform.localScale = f * Vector3.one);
                planetData.Mass.CombineLatest(planetData.GravityDepthMultiplier,
                        (mass, depth) => Properties.GravitySettings.WaveDepth.Evaluate(mass) * depth)
                    .Subscribe(f => gas.GravityWaves.material.SetFloat("_Depth", f));
                planetData.Mass.Subscribe(f =>
                {
                    gas.GravityWaves.material.SetFloat("_Frequency", Properties.GravitySettings.WaveFrequency.Evaluate(f));
                    //gas.WaveScroll.Speed = Properties.GravitySettings.WaveSpeed.Evaluate(f);
                });
            }
            else
            {
                planet = Instantiate(Planet, SectorRoot);
                //planet.Icon.material.mainTexture = planetData.Mass > Context.GlobalData.PlanetMass ? PlanetIcon : PlanetoidIcon;
            }

            planetData.Mass.Subscribe(mass =>
            {
                var diameter = Properties.BodyDiameter.Evaluate(mass);
                planet.Body.transform.localScale = diameter * Vector3.one;
                //planet.GridSnap.Offset = diameter * 2;
            });
            
            planetData.Mass.CombineLatest(planetData.GravityRadiusMultiplier,
                    (mass, radius) => Properties.GravitySettings.GravityRadius.Evaluate(mass) * radius)
                .Subscribe(f => planet.GravityWell.transform.localScale = f * Vector3.one);
            planetData.Mass.CombineLatest(planetData.GravityDepthMultiplier,
                    (mass, depth) => Properties.GravitySettings.GravityDepth.Evaluate(mass) * depth)
                .Subscribe(f => planet.GravityWell.material.SetFloat("_Depth", f));
            
            _planets.Add(planetData.ID, planet.transform);
        }
    }

    private void Update()
    {
        foreach(var belt in _zone.AsteroidBelts.Values)
        {
            for (int i = 0; i < AsteroidMeshes.Length; i++)
            {
                var matrices = belt.Transforms
                    .Skip((int) (((float)i / AsteroidMeshes.Length) * belt.Transforms.Length))
                    .Take((int) (1f / AsteroidMeshes.Length * belt.Transforms.Length))
                    .Select(v => Matrix4x4.TRS(
                        new Vector3(v.x, _zone.GetHeight(v.xy) + 5, v.y),
                        Quaternion.Euler(cos(v.z + (float) i / AsteroidMeshes.Length) * 100, sin(v.z + (float) i / AsteroidMeshes.Length) * 100, 0),
                        Vector3.one * (v.w)))
                    .ToArray();
                Graphics.DrawMeshInstanced(AsteroidMeshes[i], 0, AsteroidMaterial, matrices);
            }
        }
    }

    void LateUpdate()
    {
        foreach (var planet in _planets)
        {
            var p = _zone.GetOrbitPosition(_zone.Planets[planet.Key].Orbit);
            planet.Value.position = new Vector3(p.x, _zone.GetHeight(p) + 10, p.y);
        }

        var pos = FogCameraParent.position;
        FogMaterial.SetVector("_GridTransform", new Vector4(pos.x,pos.z,_viewDistance*2));
        
    }
}
