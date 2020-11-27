using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Sector Display Properties", menuName = "Aetheria/Sector Display Properties")]
public class SectorDisplayProperties : ScriptableObject
{
    public float DefaultViewDistance;
    public float DefaultMinimapDistance;
    public ExponentialCurve BodyDiameter;
    public ExponentialCurve GravityDepth;
    public ExponentialCurve GravityRadius;
    public ExponentialCurve WaveDepth;
    public ExponentialCurve WaveRadius;
    public ExponentialCurve WaveFrequency;
    public ExponentialCurve WaveSpeed;
    public ExponentialCurve FogTintRadius;
    public ExponentialCurve LightRadius;
    public Gradient DefaultGradient;
}
