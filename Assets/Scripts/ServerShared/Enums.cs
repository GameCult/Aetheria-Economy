using System;

public enum LauncherCaliber
{
    Micro,
    Missile,
    Fighter
}

public enum HardpointType
{
    Hull,
    Thruster,
    WarpThruster,
    Reactor,
    Radiator,
    Shield,
    Cooler,
    Sensors,
    Tool,
    Energy,
    Ballistic,
    Launcher
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