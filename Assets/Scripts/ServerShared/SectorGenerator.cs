using System;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Random = Unity.Mathematics.Random;

public static class SectorGenerator
{
    public static Sector GenerateSector(ref Random random, SectorGenerationSettings settings, MarkovNameGenerator nameGenerator)
    {
        var inputSamples = new float2[2048];
        var sample = 0;
        var accumulator = 0f;
        while (sample < inputSamples.Length)
        {
            var v = random.NextFloat2();
            accumulator += pow(settings.CloudDensity(v), 3f) * (.25f-lengthsq(v - float2(.5f))) * 4;
            if (accumulator > .5f)
            {
                accumulator = 0;
                inputSamples[sample++] = v;
            }
        }
        // for (int i = 0; i < inputSamples.Length; i++)
        // {
        //     inputSamples[i] = random.NextFloat2();
        // }
        var outputSamples = new float2[64];
        //Array.Copy(inputSamples,outputSamples,32);
        Func<float2, float2, float, float, float> weightFunc = (p1, p2, d2, dmax) => 
            pow(1-sqrt(d2)/dmax, 1+settings.CloudDensity((p1 + p2) / 2)*8);
        WeightedSampleElimination.Eliminate(inputSamples, outputSamples, 0f, 4, 0.0f);
        
        var sector = new Sector();
        foreach (var v in outputSamples) sector.Zones.Add(new SectorZone {Position = v, Name = nameGenerator.NextName});

        return sector;
    }
}