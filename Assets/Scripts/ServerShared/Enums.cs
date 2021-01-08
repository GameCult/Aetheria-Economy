/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;

public enum ItemRotation
{
    None = 0,
    CounterClockwise = 1,
    Reversed = 2,
    Clockwise = 3
}

public enum HitType
{
    Armor,
    Hardpoint,
    Gear,
    Thermal
}

public enum LauncherCaliber
{
    Micro,
    Missile,
    Fighter
}

public enum HardpointType
{
    Hull,
    Tool,
    Thermal,
    Thruster,
    WarpThruster,
    Reactor,
    Radiator,
    Shield,
    Sensors,
    Energy,
    Ballistic,
    Launcher,
    ControlModule
}

public enum DamageType
{
    Kinetic,
    Corrosive,
    Electric,
    Thermal,
    Optical,
    Ionizing
}

[Flags]
public enum BodyType
{
    None = 0,
    Asteroid = 1 << 0,
    Planetoid = 1 << 1,
    Planet = 1 << 2,
    GasGiant = 1 << 3,
    Sun = 1 << 4
}

public enum HullType
{
    Ship,
    Station,
    Turret
}

public enum MegaPlacementType
{
    Mass = 0,
    Planets = 1,
    Resources = 2,
    Connected = 3,
    Isolated = 4
}

public enum SimpleCommodityCategory
{
    Minerals,
    Metals,
    Alloys,
    Compounds,
    Organics,
    Ammo,
    Consumer
}

public enum CompoundCommodityCategory
{
    Wearables,
    Consumables,
    Luxuries,
    Tools,
    Manufacturing,
    Assemblies
}