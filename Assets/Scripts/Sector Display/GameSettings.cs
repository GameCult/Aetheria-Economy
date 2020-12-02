using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Sector Display Properties", menuName = "Aetheria/Sector Display Properties")]
public class GameSettings : ScriptableObject
{
    public float DefaultZoneRadius;
    public float DefaultZoneMass;
    public float DefaultViewDistance;
    public float MinimapZoneGravityRange;
    public float[] MinimapZoomLevels;
    public int DefaultMinimapZoom;
    public float IconSize;
    public int AsteroidMeshCount = 5;
    public PlanetSettings PlanetSettings;
    public ZoneGenerationSettings ZoneSettings;
}

