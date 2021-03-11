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
    public MeshRenderer SectorRenderer;
    public SectorGenerationSettings Settings;
    public Prototype ZonePrototype;
    public Prototype LinkPrototype;
    public Prototype EntrancePrototype;
    public Prototype ExitPrototype;
    public float ExitDistance = 1;
    public int ZoneCount = 64;
    public int MegaCount = 2;
    public float LinkDensity = .5f;
    public float LabelDistance = .4f;
    
    [Header("Name Generation")]
    public int NameGeneratorMinLength = 5;
    public int NameGeneratorMaxLength = 10;
    public int NameGeneratorOrder = 4;
    // Start is called before the first frame update
    void Start()
    {
        var filePath = new DirectoryInfo(Application.dataPath).Parent.CreateSubdirectory("GameData");
        var cache = new DatabaseCache();
        cache.Load(Path.Combine(filePath.FullName, "AetherDB.msgpack"));

        var megas = cache.GetAll<MegaCorporation>();

        var sectorMegas = megas.OrderBy(x => Random.value).Take(MegaCount).ToArray();
        foreach (var mega in sectorMegas)
        {
            var nameFile = UnityHelpers.LoadAsset<TextAsset>(mega.GeonameFile);
            var names = new HashSet<string>();
            var lines = nameFile.text.Split('\n');
            foreach (var line in lines)
            {
                foreach(var word in line.ToUpperInvariant().Split(' ', ',', '.', '"'))
                    if (word.Length >= NameGeneratorMinLength && !names.Contains(word))
                        names.Add(word);
            }

            var random = new Unity.Mathematics.Random((uint) (DateTime.Now.Ticks % uint.MaxValue));
            mega.NameGenerator = new MarkovNameGenerator(ref random, names, NameGeneratorOrder, NameGeneratorMinLength, NameGeneratorMaxLength);
        }
        
        Settings.NoisePosition = Random.value * 100;
        var sector = SectorGenerator.GenerateSector(Settings, sectorMegas, ZoneCount, LinkDensity);
        var visitedZones = new HashSet<SectorZone>();
        Debug.Log($"Found {sector.Zones.Count(z=>!z.AdjacentZones.Any())} orphaned zones!");
        foreach (var zone in sector.Zones)
        {
            var zoneInstance = ZonePrototype.Instantiate<SectorZoneUI>();
            zoneInstance.transform.localPosition = new Vector3(zone.Position.x, zone.Position.y);
            zoneInstance.Background.material.SetColor("_TintColor", zone.Occupants[0].PrimaryColor.ToColor());
            zoneInstance.Background.transform.localScale = Vector3.one * (sector.OccupantSources.Values.Any(os=>os==zone) ? 4 : 2);
            var linkDirection = float2.zero;
            foreach (var link in zone.AdjacentZones)
            {
                linkDirection += normalize(link.Position - zone.Position);
                if (!visitedZones.Contains(link))
                {
                    var linkInstance = LinkPrototype.Instantiate<Transform>();
                    var pos = (zone.Position + link.Position) / 2;
                    linkInstance.localPosition = new Vector3(pos.x, pos.y);
                    if (zone.Occupants.Length == 1 && link.Occupants.Length == 1 && zone.Occupants[0] == link.Occupants[0])
                    {
                        linkInstance.GetComponent<MeshRenderer>().material.SetColor("_TintColor", zone.Occupants[0].PrimaryColor.ToColor());
                    }
                    
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
                var entranceInstance = EntrancePrototype.Instantiate<Transform>();
                entranceInstance.localPosition = new Vector3(
                    zone.Position.x - linkDirection.x * ExitDistance,
                    zone.Position.y - linkDirection.y * ExitDistance);
            }

            if (zone == sector.Exit)
            {
                var entranceInstance = ExitPrototype.Instantiate<Transform>();
                entranceInstance.localPosition = new Vector3(
                    zone.Position.x - linkDirection.x * ExitDistance,
                    zone.Position.y - linkDirection.y * ExitDistance);
            }
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
