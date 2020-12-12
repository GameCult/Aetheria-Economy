using System;

public enum ItemRotation
{
    None = 0,
    Clockwise = 1,
    Reversed = 2,
    CounterClockwise = 3
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
    Thermal,
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
    Launcher,
    Infrastructure,
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
    Organics
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