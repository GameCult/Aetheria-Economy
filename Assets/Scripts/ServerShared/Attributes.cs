using System;

public class NameAttribute : Attribute
{
    public string Name;

    public NameAttribute(string name)
    {
        Name = name;
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class RethinkTableAttribute : Attribute
{
    public string TableName;

    public RethinkTableAttribute(string tableName)
    {
        TableName = tableName;
    }
}

public class UpdateOrderAttribute : Attribute
{
    public int Order;

    public UpdateOrderAttribute(int order)
    {
        Order = order;
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class EntityTypeRestrictionAttribute : Attribute
{
    public readonly HullType Type;

    public EntityTypeRestrictionAttribute(HullType type)
    {
        Type = type;
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class InspectableAttribute : Attribute { }

public class InspectableFieldAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Field)]
public class InspectableTextAttribute : InspectableFieldAttribute { }

[AttributeUsage(AttributeTargets.Field)]
public class InspectablePrefabAttribute : InspectableFieldAttribute { }

[AttributeUsage(AttributeTargets.Field)]
public class TemperatureInspectableAttribute : InspectableFieldAttribute { }

[AttributeUsage(AttributeTargets.Field)]
public class InspectableAnimationCurveAttribute : InspectableFieldAttribute { }

[AttributeUsage(AttributeTargets.Field)]
public class InspectableDatabaseLinkAttribute : InspectableFieldAttribute
{
    public readonly Type EntryType;

    public InspectableDatabaseLinkAttribute(Type entryType)
    {
        EntryType = entryType;
    }
}

[AttributeUsage(AttributeTargets.Field)]
public class RangedFloatInspectableAttribute : InspectableFieldAttribute
{
    public readonly float Min, Max;

    public RangedFloatInspectableAttribute(float min, float max)
    {
        Min = min;
        Max = max;
    }
}

[AttributeUsage(AttributeTargets.Field)]
public class RangedIntInspectableAttribute : InspectableFieldAttribute
{
    public readonly int Min, Max;

    public RangedIntInspectableAttribute(int min, int max)
    {
        Min = min;
        Max = max;
    }
}

public class InspectableTypeAttribute : InspectableFieldAttribute
{
    public readonly Type Type;

    public InspectableTypeAttribute(Type type)
    {
        Type = type;
    }
}