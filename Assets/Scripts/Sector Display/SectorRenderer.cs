using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

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
    public ActionGameManager Manager;
    public float FogFarFadeFraction = .125f;
    public float FarPlaneDistanceMultiplier = 2;
    
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
        LoadZone(Manager.CurrentZone);
        ViewDistance = Properties.DefaultViewDistance;
        MinimapDistance = Properties.DefaultMinimapDistance;
        Manager.ZoneChanged += (_, args) => LoadZone(args.NewZone);
    }

    void LoadZone(Zone zone)
    {
        _zone = zone;
        SectorBrushes.localScale = zone.Data.Radius * 2 * Vector3.one;
        if (_planets.Count > 0)
        {
            foreach (var planet in _planets.Values)
            {
                Destroy(planet.gameObject);
            }
            _planets.Clear();
        }
        foreach(var p in zone.Planets.Values)
            LoadPlanet(p);
    }

    void LoadPlanet(PlanetData planetData)
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
                    sun.Light.color = sunData.LightColor;
                    sun.Light.range = Properties.LightRadius.Evaluate(sunData.Mass);
                    sun.FogTint.material.SetColor("_Color", sunData.FogTintColor);
                    sun.FogTint.transform.localScale = Properties.FogTintRadius.Evaluate(sunData.Mass) * Vector3.one;
                }
                else planet = Instantiate(GasGiant, SectorRoot);

                var gas = (GasGiantObject) planet;
                gas.GradientMapper.ApplyGradient(gasGiantData.Colors);
                gas.SunMaterial.AlbedoRotationSpeed = gasGiantData.AlbedoRotationSpeed;
                gas.SunMaterial.FirstOffsetRotationSpeed = gasGiantData.AlbedoRotationSpeed;
                gas.SunMaterial.SecondOffsetRotationSpeed = gasGiantData.AlbedoRotationSpeed;
                gas.SunMaterial.FirstOffsetDomainRotationSpeed = gasGiantData.AlbedoRotationSpeed;
                gas.SunMaterial.SecondOffsetDomainRotationSpeed = gasGiantData.AlbedoRotationSpeed;
                gas.GravityWaves.material.SetFloat("_Depth", Properties.WaveDepth.Evaluate(planetData.Mass));
                gas.GravityWaves.material.SetFloat("_Frequency", Properties.WaveFrequency.Evaluate(planetData.Mass));
                gas.WaveScroll.Speed = Properties.WaveSpeed.Evaluate(planetData.Mass);
                gas.GravityWaves.transform.localScale = Properties.WaveRadius.Evaluate(planetData.Mass) * Vector3.one;
            }
            else
            {
                planet = Instantiate(Planet, SectorRoot);
                planet.Icon.material.mainTexture = planetData.Mass > Manager.Context.GlobalData.PlanetMass ? PlanetIcon : PlanetoidIcon;
            }
            
            var diameter = Properties.BodyDiameter.Evaluate(planetData.Mass);
            planet.Body.transform.localScale = diameter * Vector3.one;
            planet.GravityWell.transform.localScale = Properties.GravityRadius.Evaluate(planetData.Mass) * Vector3.one;
            planet.GravityWell.material.SetFloat("_Depth", Properties.GravityDepth.Evaluate(planetData.Mass));
            planet.GridSnap.Offset = diameter * 2;
            
            _planets.Add(planetData.ID, planet.transform);
        }
    }

    void LateUpdate()
    {
        foreach (var planet in _planets)
        {
            planet.Value.position = ((Vector2) _zone.GetOrbitPosition(_zone.Planets[planet.Key].Orbit)).Flatland();
        }

        var pos = FogCameraParent.position;
        FogMaterial.SetVector("_GridTransform", new Vector4(pos.x,pos.z,_viewDistance*2));
    }
}
