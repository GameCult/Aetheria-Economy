/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Sector Display Properties", menuName = "Aetheria/Sector Display Properties")]
public class GameSettings : ScriptableObject
{
    public float HeatstrokePhasingFloor = 0;
    public float HeatstrokePhasingFrequency = 5;
    public float WormholeDistanceRatio;
    public float DefaultZoneRadius;
    public float DefaultZoneMass;
    public float DefaultViewDistance;
    public float MinimapZoneGravityRange;
    public float[] MinimapZoomLevels;
    public int DefaultMinimapZoom;
    public ExponentialCurve IconSize;
    public int AsteroidMeshCount = 5;
    public float MinimapAsteroidSize = 3;
    public float PlanetRotationSpeed = 1;
    public BodySettingsCollection[] BodySettingsCollections;
    public PlanetSettings PlanetSettings;
    public ZoneGenerationSettings ZoneSettings;
    public GameplaySettings GameplaySettings;
    public Color HullHitColor;
    public Color ArmorHitColor;
    public Color HardpointHitColor;
    public Color GearHitColor;
    public Gradient ArmorGradient;
    public Gradient DurabilityGradient;
    [HideInInspector] public Sprite[] ItemIcons;
}

[Serializable]
public class BodySettingsCollection
{
    public float MinimumMass;
    public CelestialBodySettings[] BodySettings;
}