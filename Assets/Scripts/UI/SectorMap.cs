using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using static Unity.Mathematics.noise;
using Random = UnityEngine.Random;
using float2 = Unity.Mathematics.float2;

public class SectorMap : MonoBehaviour
{
    public Camera InfluenceCamera;
    public Prototype InfluenceRendererPrototype;
    public MeshRenderer SectorRenderer;
    public SectorGenerationSettings Settings;
    public Prototype ZonePrototype;
    public Prototype LinkPrototype;
    public Prototype IconPrototype;
    public Material ZonePrimaryMaterial;
    public Material ZoneSecondaryMaterial;
    public Material ZoneLinkMaterial;
    public float MegaPrimaryBoost = .75f;
    public float MegaSecondaryBoost = 2f;
    public float MegaLinkBoost = 2f;
    public Texture2D EntranceIcon;
    public Texture2D ExitIcon;
    public Texture2D BossIcon;
    public Texture2D HomeIcon;
    public Prototype LegendPrototype;
    public float IconDistance = 1;
    public int ZoneCount = 64;
    public float LinkDensity = .5f;
    public float LabelDistance = .4f;

    private Dictionary<MegaCorporation, (RenderTexture influence, Material primaryMaterial, Material secondaryMaterial, Material linkMaterial)> _factionMaterials =
        new Dictionary<MegaCorporation, (RenderTexture influence, Material primaryMaterial, Material secondaryMaterial, Material linkMaterial)>();
    
    void Start()
    {
        var filePath = new DirectoryInfo(Application.dataPath).Parent.CreateSubdirectory("GameData");
        var cache = new DatabaseCache();
        cache.Load(Path.Combine(filePath.FullName, "AetherDB.msgpack"));

        var megas = cache.GetAll<MegaCorporation>();

        var random = new Unity.Mathematics.Random((uint) (DateTime.Now.Ticks % uint.MaxValue));
        var sectorMegas = megas.OrderBy(x => Random.value).Take(Settings.MegaCount).ToArray();
        foreach (var mega in sectorMegas)
        {
            var primary = Instantiate(ZonePrimaryMaterial);
            primary.SetColor("_TintColor", (mega.PrimaryColor * MegaPrimaryBoost).ToColor());
            var secondary = Instantiate(ZoneSecondaryMaterial);
            secondary.SetColor("_TintColor", (mega.SecondaryColor * MegaSecondaryBoost).ToColor());
            var link = Instantiate(ZoneLinkMaterial);
            link.SetColor("_TintColor", (mega.PrimaryColor * MegaLinkBoost).ToColor());
            _factionMaterials.Add(mega, (new RenderTexture(1024, 1024, 1, RenderTextureFormat.RHalf), primary, secondary, link));
            mega.NameGenerator = new MarkovNameGenerator(ref random, UnityHelpers.LoadAsset<TextAsset>(mega.GeonameFile).text, Settings);
            var legendElement = LegendPrototype.Instantiate<LegendElement>();
            legendElement.Primary.color = mega.PrimaryColor.ToColor();
            legendElement.Secondary.color = mega.SecondaryColor.ToColor();
            legendElement.Label.text = mega.ShortName;
        }

        Settings.NoisePosition = Random.value * 100;
        var sector = SectorGenerator.GenerateSector(Settings, sectorMegas, ref random);
        var visitedZones = new HashSet<SectorZone>();
        Debug.Log($"Found {sector.Zones.Count(z=>!z.AdjacentZones.Any())} orphaned zones!");

        var zoneInstances = new Dictionary<SectorZone, SectorZoneUI>();
        foreach (var zone in sector.Zones)
        {
            var zoneInstance = ZonePrototype.Instantiate<SectorZoneUI>();
            zoneInstances[zone] = zoneInstance;
            var zoneInstanceTransform = zoneInstance.transform;
            zoneInstanceTransform.localPosition = new Vector3(zone.Position.x, zone.Position.y);
            if(zone.Owner != null)
            {
                zoneInstance.Primary.sharedMaterial = _factionMaterials[zone.Owner].primaryMaterial;
                zoneInstance.Secondary.sharedMaterial = _factionMaterials[zone.Owner].secondaryMaterial;
            }
            //zoneInstance.Background.transform.localScale = Vector3.one * (sector.HomeZones.Values.Any(os=>os==zone) ? 4 : 2);
            var linkDirection = float2.zero;
            foreach (var link in zone.AdjacentZones)
            {
                linkDirection += normalize(link.Position - zone.Position);
                if (!visitedZones.Contains(link))
                {
                    var linkInstance = LinkPrototype.Instantiate<Transform>();
                    var pos = (zone.Position + link.Position) / 2;
                    linkInstance.localPosition = new Vector3(pos.x, pos.y);
                    if (zone.Owner != null && zone.Owner == link.Owner)
                        linkInstance.GetComponent<MeshRenderer>().sharedMaterial = _factionMaterials[zone.Owner].linkMaterial;

                    var localScale = linkInstance.localScale;
                    localScale = new Vector3(length(zone.Position - link.Position), localScale.y, localScale.z);
                    linkInstance.localScale = localScale;

                    var dir = normalize(zone.Position - link.Position);
                    linkInstance.rotation = Quaternion.Euler(0,0,atan2(dir.y,dir.x) * Mathf.Rad2Deg);
                }
            }

            linkDirection = normalize(linkDirection);
            var zoneText = zoneInstance.Label;
            zoneText.text = zone.Name;
            var zoneTextTransform = zoneText.GetComponent<RectTransform>();
            zoneTextTransform.pivot = new Vector2(sign(linkDirection.x)/2+.5f,sign(linkDirection.y)/2+.5f);
            zoneTextTransform.localPosition = new Vector3(-linkDirection.x * LabelDistance, -linkDirection.y * LabelDistance);
            visitedZones.Add(zone);

            if (zone == sector.Entrance)
            {
                var iconInstance = IconPrototype.Instantiate<MeshRenderer>();
                iconInstance.material.mainTexture = EntranceIcon;
                var iconTransform = iconInstance.transform;
                iconTransform.SetParent(zoneInstanceTransform);
                iconTransform.localScale = Vector3.one;
                iconTransform.localPosition = new Vector3(-linkDirection.x * IconDistance, -linkDirection.y * IconDistance);
            }

            if (zone == sector.Exit)
            {
                var iconInstance = IconPrototype.Instantiate<MeshRenderer>();
                iconInstance.material.mainTexture = ExitIcon;
                var iconTransform = iconInstance.transform;
                iconTransform.SetParent(zoneInstanceTransform);
                iconTransform.localScale = Vector3.one;
                iconTransform.localPosition = new Vector3(-linkDirection.x * IconDistance, -linkDirection.y * IconDistance);
            }

            var homeMega = sector.HomeZones.Keys.FirstOrDefault(m => sector.HomeZones[m] == zone);
            if (homeMega != null)
            {
                var iconInstance = IconPrototype.Instantiate<MeshRenderer>();
                iconInstance.material.mainTexture = HomeIcon;
                iconInstance.material.SetColor("_TintColor", homeMega.PrimaryColor.ToColor());
                var iconTransform = iconInstance.transform;
                iconTransform.SetParent(zoneInstanceTransform);
                iconTransform.localScale = Vector3.one;
                iconTransform.localPosition = new Vector3(-linkDirection.x * IconDistance, -linkDirection.y * IconDistance);
            }

            var bossMega = sector.BossZones.Keys.FirstOrDefault(m => sector.BossZones[m] == zone);
            if (bossMega != null)
            {
                var iconInstance = IconPrototype.Instantiate<MeshRenderer>();
                iconInstance.material.mainTexture = BossIcon;
                iconInstance.material.SetColor("_TintColor", bossMega.PrimaryColor.ToColor());
                var iconTransform = iconInstance.transform;
                iconTransform.SetParent(zoneInstanceTransform);
                iconTransform.localScale = Vector3.one;
                if(homeMega != null)
                    iconTransform.localPosition = new Vector3(linkDirection.x * IconDistance, linkDirection.y * IconDistance);
                else iconTransform.localPosition = new Vector3(-linkDirection.x * IconDistance, -linkDirection.y * IconDistance);
            }
        }
        
        // Render Influence Textures
        foreach (var mega in sectorMegas)
        {
            var influenceRenderer = InfluenceRendererPrototype.Instantiate<MeshRenderer>();
            foreach (var zone in sector.Zones)
            {
                var instance = zoneInstances[zone];
                var influence = 0f;
                if (zone.Megas.Length > 0)
                {
                    if (zone.Megas.Contains(mega))
                    {
                        influence = 10;
                        if (zone.Owner != mega)
                            influence *= .5f;
                    }
                    else influence = -10;
                }
                instance.Influence.material.SetFloat("_Depth", influence);
            }

            InfluenceCamera.targetTexture = _factionMaterials[mega].influence;
            InfluenceCamera.Render();
            
            influenceRenderer.material.mainTexture = _factionMaterials[mega].influence;
            influenceRenderer.material.SetColor("_Color1", mega.PrimaryColor.ToColor());
            influenceRenderer.material.SetColor("_Color2", mega.SecondaryColor.ToColor());
            influenceRenderer.material.SetFloat("_FillTilt", random.NextFloat(PI));
        }
    }

    // Update is called once per frame
    void Update()
    {
        SectorRenderer.material.SetFloat("CloudAmplitude", Settings.CloudAmplitude);
        SectorRenderer.material.SetFloat("CloudExponent", Settings.CloudExponent);
        SectorRenderer.material.SetFloat("NoisePosition", Settings.NoisePosition);
        SectorRenderer.material.SetFloat("Zoom", Settings.Zoom);
        SectorRenderer.material.SetFloat("NoiseAmplitude", Settings.NoiseAmplitude);
        SectorRenderer.material.SetFloat("NoiseOffset", Settings.NoiseOffset);
        SectorRenderer.material.SetFloat("NoiseGain", Settings.NoiseGain);
        SectorRenderer.material.SetFloat("NoiseLacunarity", Settings.NoiseLacunarity);
        SectorRenderer.material.SetFloat("NoiseFrequency", Settings.NoiseFrequency);
    }
}
