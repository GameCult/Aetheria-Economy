﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

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

public class OrderAttribute : Attribute
{
    public int Order;

    public OrderAttribute(int order)
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

public class RuntimeInspectable : Attribute { }

[AttributeUsage(AttributeTargets.Class)]
public class InspectableAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class)]
public class ExternalEntryAttribute : Attribute { }

public class InspectableFieldAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Field)]
public class InspectableTextAttribute : InspectableFieldAttribute { }

[AttributeUsage(AttributeTargets.Field)]
public class InspectableUnityObjectAttribute : InspectableFieldAttribute
{
    public Type ObjectType;

    public InspectableUnityObjectAttribute(Type type)
    {
        ObjectType = type;
    }
}

[AttributeUsage(AttributeTargets.Field)]
public class InspectablePrefabAttribute : InspectableFieldAttribute { }

[AttributeUsage(AttributeTargets.Field)]
public class InspectableTextureAttribute : InspectableFieldAttribute { }

[AttributeUsage(AttributeTargets.Field)]
public class InspectableTextAssetAttribute : InspectableFieldAttribute { }

[AttributeUsage(AttributeTargets.Field)]
public class InspectableTemperatureAttribute : InspectableFieldAttribute { }

[AttributeUsage(AttributeTargets.Field)]
public class InspectableAnimationCurveAttribute : InspectableFieldAttribute { }

[AttributeUsage(AttributeTargets.Field)]
public class InspectableColorAttribute : InspectableFieldAttribute { }

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
public class RangedFloatAttribute : Attribute
{
    public readonly float Min, Max;

    public RangedFloatAttribute(float min, float max)
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

[AttributeUsage(AttributeTargets.Field)]
public class SimplePerformanceStatAttribute : Attribute { }

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
// public class CategoryAttribute : Attribute
// {
//     public readonly Type Type;
//
//     public CategoryAttribute(Type type)
//     {
//         Type = type;
//     }
// }