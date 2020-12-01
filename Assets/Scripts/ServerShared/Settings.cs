using System;
using System.Collections;
using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[Serializable, MessagePackObject(keyAsPropertyName:true), JsonObject]
public class PlanetSettings
{
    public ExponentialCurve GravityDepth;
    public ExponentialCurve GravityRadius;
    public ExponentialCurve WaveDepth;
    public ExponentialCurve WaveRadius;
    public ExponentialCurve WaveFrequency;
    public ExponentialCurve WaveSpeed;
    public ExponentialCurve FogTintRadius;
    public ExponentialCurve LightRadius;
    public ExponentialCurve BodyRadius;
    public ExponentialLerp AsteroidSize;
    public ExponentialLerp AsteroidHitpoints;
    public ExponentialLerp AsteroidRespawnTime;
    public float GravityStrength;
    public float MiningDifficulty = 500f;

    public ExponentialCurve OrbitPeriod;
}

[Serializable, MessagePackObject(keyAsPropertyName:true), JsonObject]
public class GalaxyShapeSettings
{
    public int Arms = 4;
    public float Twist = 10;
    public float TwistExponent = 2;
}

[Serializable, MessagePackObject(keyAsPropertyName:true), JsonObject]
public class ZoneGenerationSettings
{
    public GalaxyShapeSettings ShapeSettings;
    
    public ExponentialCurve PlanetSafetyRadius;
    
    public float MassFloor = 1;
    public float SunMass = 10000;
    public float GasGiantMass = 2000;
    public float PlanetMass = 100f;

    public int SatellitePasses = 4;
    public float SatelliteCreationMassFloor = 100;
    public float SatelliteCreationProbability = .25f;
    public float BinaryCreationProbability = .25f;
    public float RosetteProbability = .25f;

    public ExponentialLerp ZoneRadius;
    public ExponentialLerp ZoneMass;
    
    public float BeltProbability = .05f;
    public float BeltMassCeiling = 500f;

    public ExponentialCurve AsteroidCount;
    public ExponentialLerp AsteroidRotationSpeed;

    public float ResourceDensityMinimum = .1f;
    public float ResourceDensityMaximum = 1.5f;
}
