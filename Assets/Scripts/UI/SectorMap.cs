using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using static Unity.Mathematics.noise;
using Random = UnityEngine.Random;

public class SectorMap : MonoBehaviour
{
    public MeshRenderer SectorRenderer;
    public SectorGenerationSettings Settings;
    public Prototype ZonePrototype;
    public Prototype LinkPrototype;
    public int ZoneCount = 64;
    public float LinkDensity = .5f;
    
    [Header("Name Generation")]
    public TextAsset NameFile;
    public int NameFileMinWordLength = 4;
    public int NameGeneratorMinLength = 5;
    public int NameGeneratorMaxLength = 10;
    public int NameGeneratorOrder = 4;
    // Start is called before the first frame update
    void Start()
    {
        var names = new HashSet<string>();
        var lines = NameFile.text.Split('\n');
        foreach (var line in lines)
        {
            foreach(var word in line.ToUpperInvariant().Split(' ', ',', '.', '"'))
                if (word.Length >= NameGeneratorMinLength && !names.Contains(word))
                    names.Add(word);
        }

        var random = new Unity.Mathematics.Random(1337);
        for (int i = 0; i < 4096; i++) random.NextFloat();
        var nameGenerator = new MarkovNameGenerator(ref random, names, NameGeneratorOrder, NameGeneratorMinLength, NameGeneratorMaxLength);
        Settings.NoisePosition = Random.value * 100;
        var sector = SectorGenerator.GenerateSector(Settings, nameGenerator, ZoneCount, LinkDensity);
        var visitedZones = new HashSet<SectorZone>();
        Debug.Log($"Found {sector.Zones.Count(z=>!z.AdjacentZones.Any())} orphaned zones!");
        foreach (var zone in sector.Zones)
        {
            var zoneInstance = ZonePrototype.Instantiate<Transform>();
            zoneInstance.localPosition = new Vector3(zone.Position.x, zone.Position.y);
            foreach (var link in zone.AdjacentZones)
            {
                if (!visitedZones.Contains(link))
                {
                    var linkInstance = LinkPrototype.Instantiate<Transform>();
                    var pos = (zone.Position + link.Position) / 2;
                    linkInstance.localPosition = new Vector3(pos.x, pos.y);
                    
                    var localScale = linkInstance.localScale;
                    localScale = new Vector3(length(zone.Position - link.Position), localScale.y, localScale.z);
                    linkInstance.localScale = localScale;

                    var dir = normalize(zone.Position - link.Position);
                    linkInstance.rotation = Quaternion.Euler(0,0,atan2(dir.y,dir.x) * Mathf.Rad2Deg);
                }
            }
            visitedZones.Add(zone);
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
