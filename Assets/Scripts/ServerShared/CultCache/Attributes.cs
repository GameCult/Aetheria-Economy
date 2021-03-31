using System;

[AttributeUsage(AttributeTargets.Class)]
public class ExternalEntryAttribute : Attribute { }

public class InspectableAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Field)]
public abstract class PreferredInspectorAttribute : InspectableAttribute { }

public class InspectableUnityObjectAttribute : PreferredInspectorAttribute
{
    public Type ObjectType;

    public InspectableUnityObjectAttribute(Type type)
    {
        ObjectType = type;
    }
}
public class InspectableTextAttribute : PreferredInspectorAttribute { }
public class InspectablePrefabAttribute : PreferredInspectorAttribute { }
public class InspectableTextureAttribute : PreferredInspectorAttribute { }
public class InspectableTextAssetAttribute : PreferredInspectorAttribute { }
public class InspectableTemperatureAttribute : PreferredInspectorAttribute { }
public class InspectableAnimationCurveAttribute : PreferredInspectorAttribute { }
public class InspectableColorAttribute : PreferredInspectorAttribute { }

public class InspectableDatabaseLinkAttribute : PreferredInspectorAttribute
{
    public readonly Type EntryType;

    public InspectableDatabaseLinkAttribute(Type entryType)
    {
        EntryType = entryType;
    }
}

public class RangedFloatInspectableAttribute : PreferredInspectorAttribute
{
    public readonly float Min, Max;

    public RangedFloatInspectableAttribute(float min, float max)
    {
        Min = min;
        Max = max;
    }
}

public class RangedIntInspectableAttribute : PreferredInspectorAttribute
{
    public readonly int Min, Max;

    public RangedIntInspectableAttribute(int min, int max)
    {
        Min = min;
        Max = max;
    }
}

public class InspectableTypeAttribute : PreferredInspectorAttribute
{
    public readonly Type Type;

    public InspectableTypeAttribute(Type type)
    {
        Type = type;
    }
}

[AttributeUsage(AttributeTargets.Field)]
public class RangedFloatAttribute : Attribute
{
    public readonly float Min, Max;

    public RangedFloatAttribute(float min, float max)
    {
        Min = min;
        Max = max;
    }
}


/// <summary>
///   <para>Specify a tooltip for a field in the Inspector window.</para>
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class TooltipAttribute : Attribute
{
    /// <summary>
    ///   <para>The tooltip text.</para>
    /// </summary>
    public readonly string tooltip;

    /// <summary>
    ///   <para>Specify a tooltip for a field.</para>
    /// </summary>
    /// <param name="tooltip">The tooltip text.</param>
    public TooltipAttribute(string tooltip) => this.tooltip = tooltip;
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